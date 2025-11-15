module SpotifyConsole.Albums

open Base
open Parsers

let getAlbumTracks (query: list<string>) =
    let albumResponse = retrieveJson<ParsedResponse> "parsed.json"

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

    retrieveJson<PagesOf<Track>> "api.json" |> parseTrack
