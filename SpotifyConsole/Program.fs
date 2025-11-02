open System
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open System.IO
open System.Diagnostics
open System.Collections.Generic

type TokenInfo = {
    AccessToken: string
    RefreshToken: string option
    ExpiresAt: DateTime
}

//Token storage path
let tokenFilePath () = Path.Combine(Environment.CurrentDirectory, "spotify_tokens.json")

// Save token to file
let saveTokens (t:TokenInfo) =
    let opts = JsonSerializerOptions(WriteIndented = true)
    File.WriteAllText(tokenFilePath(), JsonSerializer.Serialize(t, opts))

// Load token from file
let loadTokens () : TokenInfo option =
    let path = tokenFilePath()
    if File.Exists(path) then
        try
            let text = File.ReadAllText(path)
            let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
            Some(JsonSerializer.Deserialize<TokenInfo>(text, opts))
        with _ -> None
    else None

// Check if token is still valid
let isValid (t:TokenInfo) = t.ExpiresAt > DateTime.UtcNow


let clientCredentialsTokenAsync (clientId:string) (clientSecret:string) =
    task {
        use http = new HttpClient()
        let auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(sprintf "%s:%s" clientId clientSecret))
        use req = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Basic", auth)
        req.Content <- new FormUrlEncodedContent([
           KeyValuePair<string,string>("grant_type","client_credentials") 
           ])
        use! resp = http.SendAsync(req)
        let! body = resp.Content.ReadAsStringAsync()
        if not resp.IsSuccessStatusCode then failwithf "Token request failed: %d - %s" (int resp.StatusCode) body
        use doc = JsonDocument.Parse(body)
        let root = doc.RootElement
        let access = root.GetProperty("access_token").GetString()
        let expires = root.GetProperty("expires_in").GetInt32()
        return access, DateTime.UtcNow.AddSeconds(float expires)
    }

let exchangeCodeForTokenAsync (clientId:string) (clientSecret:string) (code:string) (redirectUri:string) =
    task {
        use http = new HttpClient()
        let auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(sprintf "%s:%s" clientId clientSecret))
        use req = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Basic", auth)
        let form = [
            KeyValuePair<string,string>("grant_type","authorization_code")
            KeyValuePair<string,string>("code", code)
            KeyValuePair<string,string>("redirect_uri", redirectUri)
        ]
        req.Content <- new FormUrlEncodedContent(form)
        use! resp = http.SendAsync(req)
        let! body = resp.Content.ReadAsStringAsync()
        if not resp.IsSuccessStatusCode then failwithf "Exchange failed: %d - %s" (int resp.StatusCode) body
        use doc = JsonDocument.Parse(body)
        let root = doc.RootElement
        let access = root.GetProperty("access_token").GetString()
        let expires = root.GetProperty("expires_in").GetInt32()
        let refresh =
            let mutable refreshElem = Unchecked.defaultof<JsonElement>
            if root.TryGetProperty("refresh_token", &refreshElem) then
                Some(refreshElem.GetString())
            else
                None
        let ti = { AccessToken = access; RefreshToken = refresh; ExpiresAt = DateTime.UtcNow.AddSeconds(float expires) }
        return ti
    }

let refreshTokenAsync (clientId:string) (clientSecret:string) (refreshToken:string) =
    task {
        use http = new HttpClient()
        let auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(sprintf "%s:%s" clientId clientSecret))
        use req = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
        req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Basic", auth)
        let form = [
            KeyValuePair<string,string>("grant_type","refresh_token")
            KeyValuePair<string,string>("refresh_token", refreshToken)
        ]
        req.Content <- new FormUrlEncodedContent(form)
        use! resp = http.SendAsync(req)
        let! body = resp.Content.ReadAsStringAsync()
        if not resp.IsSuccessStatusCode then failwithf "Refresh failed: %d - %s" (int resp.StatusCode) body
        use doc = JsonDocument.Parse(body)
        let root = doc.RootElement
        let access = root.GetProperty("access_token").GetString()
        let expires = root.GetProperty("expires_in").GetInt32()
        let newRefresh = if root.TryGetProperty("refresh_token", &_) then Some(root.GetProperty("refresh_token").GetString()) else Some(refreshToken)
        let ti = { AccessToken = access; RefreshToken = newRefresh; ExpiresAt = DateTime.UtcNow.AddSeconds(float expires) }
        return ti
    }

let startLocalListenerAndReceiveCode (prefix:string) (path:string) =
    task {
        use listener = new HttpListener()
        listener.Prefixes.Add(prefix) // e.g. http://localhost:5000/
        listener.Start()
        try
            let! ctx = listener.GetContextAsync()
            let req = ctx.Request
            let qs = req.QueryString
            let code = qs.["code"]
            let resp = ctx.Response
            let responseString = "<html><body>You can close this window and return to the application.</body></html>"
            let buffer = Encoding.UTF8.GetBytes(responseString)
            resp.ContentLength64 <- int64 buffer.Length
            use output = resp.OutputStream
            do! output.WriteAsync(buffer, 0, buffer.Length)
            resp.Close()
            return code
        finally
            listener.Stop()
    }

let openBrowser url =
    try
        let psi = ProcessStartInfo(FileName = url, UseShellExecute = true)
        Process.Start(psi) |> ignore
    with _ ->
        // fallback: print url
        printfn "Open this URL in your browser: %s" url

let authorizeAsync (clientId:string) (clientSecret:string) (redirectUri:string) (scopes:string) =
    task {
        // Build authorization URL
        let authUrl = sprintf "https://accounts.spotify.com/authorize?client_id=%s&response_type=code&redirect_uri=%s&scope=%s&show_dialog=true" (Uri.EscapeDataString(clientId)) (Uri.EscapeDataString(redirectUri)) (Uri.EscapeDataString(scopes))
        openBrowser authUrl
        // Expect redirect to e.g. http://localhost:5000/callback?code=...
        let uri = Uri(redirectUri)
        let prefix = sprintf "%s://%s:%d/" uri.Scheme uri.Host uri.Port
        let code = startLocalListenerAndReceiveCode prefix uri.AbsolutePath |> Async.AwaitTask |> Async.RunSynchronously
        if String.IsNullOrEmpty(code) then failwith "No code received"
        let tokens = exchangeCodeForTokenAsync clientId clientSecret code redirectUri |> Async.AwaitTask |> Async.RunSynchronously
        saveTokens tokens
        printfn "Authorization complete. Tokens saved to %s" (tokenFilePath())
    }

let getAccessToken (clientId:string) (clientSecret:string) (redirectUri:string) =
    // Try saved tokens -> refresh if needed -> fallback to client credentials
    match loadTokens() with
    | Some t when isValid t -> t.AccessToken
    | Some t when t.RefreshToken.IsSome ->
        let refreshed = refreshTokenAsync clientId clientSecret t.RefreshToken.Value |> Async.AwaitTask |> Async.RunSynchronously
        saveTokens refreshed
        refreshed.AccessToken
    | _ ->
        // fallback to client credentials
        let access, _exp = clientCredentialsTokenAsync clientId clientSecret |> Async.AwaitTask |> Async.RunSynchronously
        access

let searchTracksAsync (token:string) (query:string) =
    task {
        use http = new HttpClient()
        http.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Bearer", token)
        let url = sprintf "https://api.spotify.com/v1/search?q=%s&type=track&limit=5" (Uri.EscapeDataString(query))
        use! resp = http.GetAsync(url)
        let! body = resp.Content.ReadAsStringAsync()
        if not resp.IsSuccessStatusCode then failwithf "Search failed: %d - %s" (int resp.StatusCode) body
        use doc = JsonDocument.Parse(body)
        let items = doc.RootElement.GetProperty("tracks").GetProperty("items")
        let results =
            items.EnumerateArray()
            |> Seq.map(fun t ->
                let name = t.GetProperty("name").GetString()
                let artists =
                    t.GetProperty("artists").EnumerateArray()
                    |> Seq.map(fun a -> a.GetProperty("name").GetString())
                    |> String.concat ", "
                let url = t.GetProperty("external_urls").GetProperty("spotify").GetString()
                (name, artists, url)
            )
            |> Seq.toArray
        return results
    }

[<EntryPoint>]
let main argv =
    try
        let clientId = "e1db6991cfe64c6b85b848fed5a90419"
        let clientSecret = "48015b50089e4a748c6fea70bdcacd90"
        if String.IsNullOrWhiteSpace(clientId) || String.IsNullOrWhiteSpace(clientSecret) then
            printfn "Please set SPOTIFY_CLIENT_ID and SPOTIFY_CLIENT_SECRET environment variables."
            printfn "See README.md for instructions."
            1
        else
            match argv |> Array.toList with
            | "auth" :: _ ->
                // Default redirect URI -- must be registered in your Spotify app
                let redirectUri = "http://localhost:5000/callback"
                let scopes = "user-read-private user-read-email" // example scopes
                authorizeAsync clientId clientSecret redirectUri scopes |> Async.AwaitTask |> Async.RunSynchronously
                0
            | "search" :: rest when rest.Length > 0 ->
                let query = String.Join(" ", rest)
                let token = getAccessToken clientId clientSecret "http://localhost:5000/callback"
                let tracks = searchTracksAsync token query |> Async.AwaitTask |> Async.RunSynchronously
                if tracks.Length = 0 then
                    printfn "No tracks found for '%s'" query
                else
                    printfn "Top %d results for '%s':" tracks.Length query
                    tracks |> Array.iteri(fun i (name, artists, url) -> printfn "%d) %s — %s\n   %s" (i+1) name artists url)
                0
            | _ ->
                printfn "Usage: dotnet run -- auth"
                printfn "       dotnet run -- search <query>"
                printfn "Notes: For `auth` ensure your app's Redirect URI includes http://localhost:5000/callback"
                1
    with ex ->
        printfn "Error: %s" (ex.Message)
        1
