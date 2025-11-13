module SpotifyConsole.Albums

open Base
open System
open System.Text.Json
open System.IO

let parseAlbum () =
    let itemList = retrieveJson<ItemResponse> "api_response.json"

    printfn "Found %d results" itemList.total

    let parsedList = itemList.items |> List.map (fun i -> { id = i.id; name = i.name })

    let savePath =
        Path.Combine(Environment.CurrentDirectory, "responses/parsed_response.json")

    let opts = JsonSerializerOptions(WriteIndented = true)
    File.WriteAllText(savePath, JsonSerializer.Serialize(parsedList, opts))


let getAlbumTracks (query: list<string>) =
    let albumResponse = retrieveJson<SearchResponse> "parsed_response.json"

    let albumId = albumResponse.albums.items.Head.id

    let urlMapping =
        query
        |> List.mapi (fun i s ->
            match i with
            | 0 ->
                match parseIntStrOption s with
                | Some n when n > 0 && n <= 50 -> sprintf "?limit=%s" s
                | _ -> ""
            | 1 ->
                match parseIntStrOption s with
                | Some n when n >= 0 -> sprintf "&offset=%s" s
                | _ -> ""
            | _ -> "")

    urlMapping
    |> List.fold (fun (out: string) (next: string) -> out + next) (sprintf "%s/albums/%s/tracks" BASE_URL albumId)
    |> sendGetRequest

    parseAlbum ()
