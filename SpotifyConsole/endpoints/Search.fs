module SpotifyConsole.Search

open System
open System.Net.Http
open System.Text.Json
open System.IO

type FilterType =
    | Album
    | Artist
    | Playlist
    | Track

let stringToType t =
    match t with
    | "album" -> Some Album
    | "artist" -> Some Artist
    | "playlist" -> Some Playlist
    | "track" -> Some Track
    | _ -> None

let parseIntStrOption (s: string) =
    match s with
    | "" -> None
    | s -> Some(Int32.Parse s)

let isValidQuery (query: list<string>) =
    let isFirstPresent = query.Length > 0

    let isValidFilter =
        if query.Length >= 2 then
            match stringToType (query.Item 1) with
            | Some _ -> true
            | None -> false
        else
            true

    let isValidLimit =
        if query.Length >= 3 then
            match parseIntStrOption (query.Item 2) with
            | Some n when n > 0 && n <= 50 -> true
            | _ -> false
        else
            true

    let isValidOffset =
        if query.Length >= 4 then
            match parseIntStrOption (query.Item 3) with
            | Some n when n >= 0 -> true
            | _ -> false
        else
            true

    if not isFirstPresent then
        printfn "Missing query argument"

    if not isValidFilter then
        printfn "valid filter options are album, artist, playlist or track"

    if not isValidLimit then
        printfn "limit must be an integer between 1 and 50"

    if not isValidOffset then
        printfn "offset must be a non-negative integer"

    isFirstPresent && isValidFilter && isValidLimit && isValidOffset

let buildUrl (queryList: list<string>) =
    if isValidQuery queryList then
        sprintf
            "https://api.spotify.com/v1/search?q=%s&type=%s&limit=%s&offset%s"
            (Uri.EscapeDataString(queryList.Item 0))
            (Uri.EscapeDataString(queryList.Item 1))
            (queryList.Item 2)
            (queryList.Item 3)
    else
        failwith "Invalid query"

let searchAsync (query: list<string>) =
    let url = buildUrl query

    task {
        let token = Auth.getAccessToken ()
        use http = new HttpClient()
        http.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Bearer", token)

        use! resp = http.GetAsync(url)
        let! body = resp.Content.ReadAsStringAsync()

        if not resp.IsSuccessStatusCode then
            failwithf "Search failed: %d - %s" (int resp.StatusCode) body

        use doc = JsonDocument.Parse(body)

        let savePath =
            Path.Combine(Environment.CurrentDirectory, "responses/api_response.json")

        let opts = JsonSerializerOptions(WriteIndented = true)
        File.WriteAllText(savePath, JsonSerializer.Serialize(doc, opts))

    // let items = doc.RootElement.GetProperty("tracks").GetProperty("items")

    // let results =
    //     items.EnumerateArray()
    //     |> Seq.map (fun t ->
    //         let name = t.GetProperty("name").GetString()

    //         let artists =
    //             t.GetProperty("artists").EnumerateArray()
    //             |> Seq.map (fun a -> a.GetProperty("name").GetString())
    //             |> String.concat ", "

    //         let url = t.GetProperty("external_urls").GetProperty("spotify").GetString()
    //         (name, artists, url))
    //     |> Seq.toArray

    // return results
    }
