module SpotifyConsole.Base

open System
open System.Net.Http
open System.Text.Json
open System.IO

let BASE_URL = "https://api.spotify.com/v1"

let parseIntStrOption (s: string) =
    match s with
    | "" -> None
    | s -> Some(Int32.Parse s)

let sendGetRequest (url: string) =
    printfn "Sending request to %s" url

    task {
        let token = Auth.getAccessToken ()
        use http = new HttpClient()
        http.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Bearer", token)

        use! resp = http.GetAsync(url)
        let! body = resp.Content.ReadAsStringAsync()

        if not resp.IsSuccessStatusCode then
            failwithf "Request failed: %d - %s" (int resp.StatusCode) body

        use doc = JsonDocument.Parse(body)

        let savePath =
            Path.Combine(Environment.CurrentDirectory, "responses/api_response.json")

        let opts = JsonSerializerOptions(WriteIndented = true)
        File.WriteAllText(savePath, JsonSerializer.Serialize(doc, opts))

        printfn "Success"
    }

    |> Async.AwaitTask
    |> Async.RunSynchronously
