module SpotifyConsole.Base

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open System.IO

let BASE_URL = "https://api.spotify.com/v1"

type Item = { id: string; name: string }
type ItemResponse = { items: list<Item>; total: int }
type SavedItem = { added_at: string; track: Item }
type SavedResponse = { items: list<SavedItem>; total: int }
type SaveBody = { ids: list<string> }

type SearchResponse =
    { tracks: ItemResponse
      albums: ItemResponse
      playlists: ItemResponse
      artists: ItemResponse }

let parseIntStrOption (s: string) =
    match s with
    | "" -> None
    | s -> Some(Int32.Parse s)

let retrieveJson<'T> (filename: string) =
    let path = Path.Combine(Environment.CurrentDirectory, "responses", filename)

    let text = File.ReadAllText path
    let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    JsonSerializer.Deserialize<'T>(text, opts)

let sendGetRequestArrayResponse (isRespArray: bool option) (url: string) =
    printfn "Sending GET to %s" url

    task {
        let token = Auth.getAccessToken ()
        use http = new HttpClient()
        http.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Bearer", token)

        let mutable completed = false

        while not completed do
            use! resp = http.GetAsync(url)

            if resp.StatusCode = System.Net.HttpStatusCode.TooManyRequests then
                let wait = resp.Headers.RetryAfter.Delta.Value.TotalSeconds |> int32
                printfn "Rate limit reached (429). Retrying after %d seconds..." wait
                do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds wait)
            else
                completed <- true
                let! body = resp.Content.ReadAsStringAsync()

                if not resp.IsSuccessStatusCode then
                    failwithf "Request failed: %d - %s" (int resp.StatusCode) body

                use doc =
                    match isRespArray with
                    | Some b -> JsonDocument.Parse(sprintf "{\"items\": %s}" body)
                    | None -> JsonDocument.Parse body

                let savePath =
                    Path.Combine(Environment.CurrentDirectory, "responses/api_response.json")

                let opts = JsonSerializerOptions(WriteIndented = true)
                File.WriteAllText(savePath, JsonSerializer.Serialize(doc, opts))

                printfn "GET Success"
    }

    |> Async.AwaitTask
    |> Async.RunSynchronously

let sendGetRequest (url: string) = url |> sendGetRequestArrayResponse None

let sendPostRequest<'T> (url: string) (payload: 'T) =
    printfn "Sending POST to %s" url

    task {
        let token = Auth.getAccessToken ()
        use http = new HttpClient()
        http.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Bearer", token)

        let serOpts =
            JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false)

        let contentJson = JsonSerializer.Serialize(payload, serOpts)
        use body = new StringContent(contentJson, Encoding.UTF8, "application/json")

        let mutable completed = false

        while not completed do
            use! resp = http.PostAsync(url, body)

            if resp.StatusCode = System.Net.HttpStatusCode.TooManyRequests then
                let wait = resp.Headers.RetryAfter.Delta.Value.TotalSeconds |> int32
                printfn "Rate limit reached (429). Retrying after %d seconds..." wait
                do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds wait)
            else
                completed <- true
                let! bodyResp = resp.Content.ReadAsStringAsync()

                if not resp.IsSuccessStatusCode then
                    failwithf "POST failed: %d - %s" (int resp.StatusCode) bodyResp

                let savePath =
                    Path.Combine(Environment.CurrentDirectory, "responses", "api_response.json")

                if String.IsNullOrWhiteSpace bodyResp then
                    File.WriteAllText(savePath, "")
                else
                    use doc = JsonDocument.Parse bodyResp
                    let opts = JsonSerializerOptions(WriteIndented = true)
                    File.WriteAllText(savePath, JsonSerializer.Serialize(doc, opts))

                printfn "POST Success"
    }
    |> Async.AwaitTask
    |> Async.RunSynchronously

let sendPutRequest<'T> (url: string) (payload: 'T) =
    printfn "Sending PUT to %s" url

    task {
        let token = Auth.getAccessToken ()
        use http = new HttpClient()
        http.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Bearer", token)

        let serOpts =
            JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false)

        let contentJson = JsonSerializer.Serialize(payload, serOpts)
        use body = new StringContent(contentJson, Encoding.UTF8, "application/json")

        let mutable completed = false

        while not completed do
            use! resp = http.PutAsync(url, body)

            if resp.StatusCode = System.Net.HttpStatusCode.TooManyRequests then
                let wait = resp.Headers.RetryAfter.Delta.Value.TotalSeconds |> int32
                printfn "Rate limit reached (429). Retrying after %d seconds..." wait
                do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds wait)
            else
                completed <- true
                let! bodyResp = resp.Content.ReadAsStringAsync()

                if not resp.IsSuccessStatusCode then
                    failwithf "PUT failed: %d - %s" (int resp.StatusCode) bodyResp

                let savePath =
                    Path.Combine(Environment.CurrentDirectory, "responses", "api_response.json")

                // Some endpoints (e.g. PUT /me/tracks) return 200 with an empty body.
                // Avoid parsing empty responses as JSON which throws an exception.
                if String.IsNullOrWhiteSpace bodyResp then
                    // write an empty file or clear previous response
                    File.WriteAllText(savePath, "")
                else
                    use doc = JsonDocument.Parse bodyResp
                    let opts = JsonSerializerOptions(WriteIndented = true)
                    File.WriteAllText(savePath, JsonSerializer.Serialize(doc, opts))

                printfn "PUT Success"
    }
    |> Async.AwaitTask
    |> Async.RunSynchronously


let sendDeleteRequest<'T> (url: string) (payload: 'T option) =
    printfn "Sending DELETE to %s" url

    task {
        let token = Auth.getAccessToken ()
        use http = new HttpClient()
        http.DefaultRequestHeaders.Authorization <- AuthenticationHeaderValue("Bearer", token)

        let serOpts =
            JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = false)

        let mutable completed = false

        while not completed do
            // create a fresh request message each attempt
            use req = new HttpRequestMessage(HttpMethod.Delete, url)

            // if a payload is provided, serialize and attach it
            match payload with
            | Some p ->
                let contentJson = JsonSerializer.Serialize(p, serOpts)
                req.Content <- new StringContent(contentJson, Encoding.UTF8, "application/json")
            | None -> ()

            use! resp = http.SendAsync(req)

            if resp.StatusCode = System.Net.HttpStatusCode.TooManyRequests then
                let wait = resp.Headers.RetryAfter.Delta.Value.TotalSeconds |> int32
                printfn "Rate limit reached (429). Retrying after %d seconds..." wait
                do! System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds wait)
            else
                completed <- true
                let! bodyResp = resp.Content.ReadAsStringAsync()

                if not resp.IsSuccessStatusCode then
                    failwithf "DELETE failed: %d - %s" (int resp.StatusCode) bodyResp

                // Some DELETE endpoints return no body; handle gracefully
                let savePath =
                    Path.Combine(Environment.CurrentDirectory, "responses", "api_response.json")

                if String.IsNullOrWhiteSpace bodyResp then
                    File.WriteAllText(savePath, "")
                else
                    use doc = JsonDocument.Parse bodyResp
                    let opts = JsonSerializerOptions(WriteIndented = true)
                    File.WriteAllText(savePath, JsonSerializer.Serialize(doc, opts))

                printfn "DELETE Success"
    }
    |> Async.AwaitTask
    |> Async.RunSynchronously

type PaginatedResponse = { next: string; previous: string }

let sendNextRequest () =
    let r = retrieveJson<PaginatedResponse> "api_response.json"
    r.next |> sendGetRequest


let sendPreviousRequest () =
    let r = retrieveJson<PaginatedResponse> "api_response.json"
    r.previous |> sendGetRequest
