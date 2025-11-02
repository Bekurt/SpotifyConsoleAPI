module SpotifyConsole.Auth

open System
open System.Collections.Generic
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.IO
open System.Diagnostics

type TokenInfo =
    { AccessToken: string
      RefreshToken: string option
      ExpiresAt: DateTime }

let private tokenFilePath () =
    Path.Combine(Environment.CurrentDirectory, "spotify_tokens.json")

let private saveTokens (t: TokenInfo) =
    let opts = JsonSerializerOptions(WriteIndented = true)
    File.WriteAllText(tokenFilePath (), JsonSerializer.Serialize(t, opts))

let loadTokens () : TokenInfo option =
    let p = tokenFilePath ()

    if File.Exists(p) then
        try
            let text = File.ReadAllText(p)
            let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
            Some(JsonSerializer.Deserialize<TokenInfo>(text, opts))
        with _ ->
            None
    else
        None

let private isValid (t: TokenInfo) =
    t.ExpiresAt > DateTime.UtcNow.AddMinutes(1.0)

let private clientCredentialsTokenAsync (clientId: string) (clientSecret: string) =
    task {
        use http = new HttpClient()

        let auth =
            Convert.ToBase64String(Encoding.UTF8.GetBytes(sprintf "%s:%s" clientId clientSecret))

        use req =
            new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")

        req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Basic", auth)
        req.Content <- new FormUrlEncodedContent([ KeyValuePair<string, string>("grant_type", "client_credentials") ])
        use! resp = http.SendAsync(req)
        let! body = resp.Content.ReadAsStringAsync()

        if not resp.IsSuccessStatusCode then
            failwithf "Token request failed: %d - %s" (int resp.StatusCode) body

        use doc = JsonDocument.Parse(body)
        let root = doc.RootElement
        let access = root.GetProperty("access_token").GetString()
        let expires = root.GetProperty("expires_in").GetInt32()
        return access, DateTime.UtcNow.AddSeconds(float expires)
    }

let private exchangeCodeForTokenAsync (clientId: string) (clientSecret: string) (code: string) (redirectUri: string) =
    task {
        use http = new HttpClient()

        let auth =
            Convert.ToBase64String(Encoding.UTF8.GetBytes(sprintf "%s:%s" clientId clientSecret))

        use req =
            new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")

        req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Basic", auth)

        let form =
            [ KeyValuePair<string, string>("grant_type", "authorization_code")
              KeyValuePair<string, string>("code", code)
              KeyValuePair<string, string>("redirect_uri", redirectUri) ]

        req.Content <- new FormUrlEncodedContent(form)
        use! resp = http.SendAsync(req)
        let! body = resp.Content.ReadAsStringAsync()

        if not resp.IsSuccessStatusCode then
            failwithf "Exchange failed: %d - %s" (int resp.StatusCode) body

        use doc = JsonDocument.Parse(body)
        let root = doc.RootElement
        let access = root.GetProperty("access_token").GetString()
        let expires = root.GetProperty("expires_in").GetInt32()
        let mutable tmp = Unchecked.defaultof<JsonElement>

        let refresh =
            if root.TryGetProperty("refresh_token", &tmp) then
                Some(tmp.GetString())
            else
                None

        let ti =
            { AccessToken = access
              RefreshToken = refresh
              ExpiresAt = DateTime.UtcNow.AddSeconds(float expires) }

        return ti
    }

let private refreshTokenAsync (clientId: string) (clientSecret: string) (refreshToken: string) =
    task {
        use http = new HttpClient()

        let auth =
            Convert.ToBase64String(Encoding.UTF8.GetBytes(sprintf "%s:%s" clientId clientSecret))

        use req =
            new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")

        req.Headers.Authorization <- Headers.AuthenticationHeaderValue("Basic", auth)

        let form =
            [ KeyValuePair<string, string>("grant_type", "refresh_token")
              KeyValuePair<string, string>("refresh_token", refreshToken) ]

        req.Content <- new FormUrlEncodedContent(form)
        use! resp = http.SendAsync(req)
        let! body = resp.Content.ReadAsStringAsync()

        if not resp.IsSuccessStatusCode then
            failwithf "Refresh failed: %d - %s" (int resp.StatusCode) body

        use doc = JsonDocument.Parse(body)
        let root = doc.RootElement
        let access = root.GetProperty("access_token").GetString()
        let expires = root.GetProperty("expires_in").GetInt32()
        let mutable tmp = Unchecked.defaultof<JsonElement>

        let newRefresh =
            if root.TryGetProperty("refresh_token", &tmp) then
                Some(tmp.GetString())
            else
                Some(refreshToken)

        let ti =
            { AccessToken = access
              RefreshToken = newRefresh
              ExpiresAt = DateTime.UtcNow.AddSeconds(float expires) }

        return ti
    }

let private startLocalListenerAndReceiveCode (prefix: string) (path: string) =
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

            let responseString =
                "<html><body>You can close this window and return to the application.</body></html>"

            let buffer = Encoding.UTF8.GetBytes(responseString)
            resp.ContentLength64 <- int64 buffer.Length
            use output = resp.OutputStream
            do! output.WriteAsync(buffer, 0, buffer.Length)
            resp.Close()
            return code
        finally
            listener.Stop()
    }

let private openBrowser url =
    try
        let psi = ProcessStartInfo(FileName = url, UseShellExecute = true)
        Process.Start(psi) |> ignore
    with _ ->
        // fallback: print url
        printfn "Open this URL in your browser: %s" url

let authorizeAsync (clientId: string) (clientSecret: string) (redirectUri: string) (scopes: string) =
    task {
        // Build authorization URL
        let authUrl =
            sprintf
                "https://accounts.spotify.com/authorize?client_id=%s&response_type=code&redirect_uri=%s&scope=%s&show_dialog=true"
                (Uri.EscapeDataString(clientId))
                (Uri.EscapeDataString(redirectUri))
                (Uri.EscapeDataString(scopes))

        openBrowser authUrl
        // Expect redirect to e.g. http://localhost:5000/callback?code=...
        let uri = Uri(redirectUri)
        let prefix = sprintf "%s://%s:%d/" uri.Scheme uri.Host uri.Port

        let code =
            startLocalListenerAndReceiveCode prefix uri.AbsolutePath
            |> Async.AwaitTask
            |> Async.RunSynchronously

        if String.IsNullOrEmpty(code) then
            failwith "No code received"

        let tokens =
            exchangeCodeForTokenAsync clientId clientSecret code redirectUri
            |> Async.AwaitTask
            |> Async.RunSynchronously

        saveTokens tokens
        printfn "Authorization complete. Tokens saved to %s" (tokenFilePath ())
    }

let getAccessToken (clientId: string) (clientSecret: string) (redirectUri: string) =
    // Try saved tokens -> refresh if needed -> fallback to client credentials
    match loadTokens () with
    | Some t when isValid t -> t.AccessToken
    | Some t when t.RefreshToken.IsSome ->
        let refreshed =
            refreshTokenAsync clientId clientSecret t.RefreshToken.Value
            |> Async.AwaitTask
            |> Async.RunSynchronously

        saveTokens refreshed
        refreshed.AccessToken
    | _ ->
        // fallback to client credentials
        let access, _exp =
            clientCredentialsTokenAsync clientId clientSecret
            |> Async.AwaitTask
            |> Async.RunSynchronously

        access
