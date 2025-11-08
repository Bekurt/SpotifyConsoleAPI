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

let retrieveJson<'T> (filename: string) =
    let path = Path.Combine(Environment.CurrentDirectory, "responses", filename)

    if File.Exists(path) then
        try
            let text = File.ReadAllText path
            let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
            Some(JsonSerializer.Deserialize<'T>(text, opts))
        with _ ->
            None
    else
        printfn "File %s does not exist." filename
        None

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

type PaginatedResponse = { next: string; previous: string }

let sendNextRequest () =
    let r = retrieveJson<PaginatedResponse> "api_response.json"

    match r with
    | Some r -> r.next |> sendGetRequest
    | None -> printfn "error in json reading"


let sendPreviousRequest () =
    let r = retrieveJson<PaginatedResponse> "api_response.json"

    match r with
    | Some r -> r.previous |> sendGetRequest
    | None -> printfn "error in json reading"
