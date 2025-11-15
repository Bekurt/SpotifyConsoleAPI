module SpotifyConsole.Albums

open Base
open System
open System.Text.Json
open System.IO

let parseAlbum () =
    let itemList = retrieveJson<ItemResponse> "api_response.json"

    printfn "Found %d results" itemList.total

    let parsedList = itemList.items |> List.map (fun i -> { id = i.id; name = i.name })

    writeJson "parsed_response.json" parsedList


let getAlbumTracks (query: list<string>) =
    let albumResponse = retrieveJson<ParsedResponse> "parsed_response.json"

    let albumId = albumResponse.Head.id

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
