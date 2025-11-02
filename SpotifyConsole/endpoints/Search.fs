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

type SearchQuery =
    { Query: string
      Type: FilterType option
      Limit: int option
      Offset: int option }

let typeToString (t: FilterType) =
    match t with
    | Album -> "album"
    | Artist -> "artist"
    | Playlist -> "playlist"
    | Track -> "track"

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
    match query.Length with
    | 0 ->
        printfn "Missing query argument"
        false
    | 2 ->
        if stringToType (query.Item(1)) = None then
            printfn "valid filter options are album, artist, playlist or track"
            false
        else
            true
    | _ -> true

let buildQuery (query: list<string>) =
    { Query = String.Join(" ", query.Item(0))
      Type =
        try
            query.Item(1) |> stringToType
        with _ ->
            None
      Limit =
        try
            query.Item(2) |> parseIntStrOption
        with _ ->
            None
      Offset =
        try
            query.Item(3) |> parseIntStrOption
        with _ ->
            None }

let searchAsync (token: string) (query: SearchQuery) =
    task {
        use http = new HttpClient()
        http.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Bearer", token)

        let typeStr =
            match query.Type with
            | Some t -> typeToString t
            | None -> "track"

        let limit =
            match query.Limit with
            | Some l -> l
            | None -> 50

        let offset =
            match query.Offset with
            | Some o -> o
            | None -> 0

        let url =
            sprintf
                "https://api.spotify.com/v1/search?q=%s&type=%s&limit=%d&offset%d"
                (Uri.EscapeDataString(query.Query))
                (Uri.EscapeDataString(typeStr))
                limit
                offset

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
