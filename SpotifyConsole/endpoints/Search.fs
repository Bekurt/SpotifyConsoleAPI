module SpotifyConsole.Search

open Base
open System
open System.Text.Json
open System.IO

type SearchItem = { id: string; name: string }
type SearchItemList = { items: list<SearchItem> }

type SearchResponse =
    { tracks: SearchItemList
      albums: SearchItemList
      playlists: SearchItemList
      artists: SearchItemList }

let parseSearchItems (itemType: string) =
    let itemList = retrieveJson<SearchResponse> "api_response.json"

    let parsedList =
        match itemType with
        | "&type=track" -> itemList.tracks.items |> List.map (fun i -> { id = i.id; name = i.name })
        | "&type=artist" -> itemList.artists.items |> List.map (fun i -> { id = i.id; name = i.name })
        | "&type=album" -> itemList.albums.items |> List.map (fun i -> { id = i.id; name = i.name })
        | "&type=playlist" -> itemList.playlists.items |> List.map (fun i -> { id = i.id; name = i.name })
        | _ -> itemList.tracks.items |> List.map (fun i -> { id = i.id; name = i.name })

    let savePath =
        Path.Combine(Environment.CurrentDirectory, "responses/parsed_response.json")

    let opts = JsonSerializerOptions(WriteIndented = true)
    File.WriteAllText(savePath, JsonSerializer.Serialize(parsedList, opts))

let searchItems (query: list<string>) =
    let urlMapping =
        query
        |> List.mapi (fun i s ->
            match i with
            | 0 -> sprintf "?q=%s" s
            | 1 ->
                match s with
                | "album" -> sprintf "&type=%s" s
                | "artist" -> sprintf "&type=%s" s
                | "playlist" -> sprintf "&type=%s" s
                | "track" -> sprintf "&type=%s" s
                | _ -> "&type=track"
            | 2 ->
                match parseIntStrOption s with
                | Some n when n > 0 && n <= 50 -> sprintf "&limit=%s" s
                | _ -> ""
            | 3 ->
                match parseIntStrOption s with
                | Some n when n >= 0 -> sprintf "&offset=%s" s
                | _ -> ""
            | _ -> "")

    if query.Length > 0 then
        urlMapping
        |> List.fold (fun (out: string) (next: string) -> out + next) (sprintf "%s/search" BASE_URL)
        |> sendGetRequest

        parseSearchItems (urlMapping.Item 0)
    else
        failwith "Query is missing required parameters"
