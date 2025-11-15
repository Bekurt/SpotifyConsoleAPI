module SpotifyConsole.Artists

open Base
open Parsers

let getArtistAlbums (query: list<string>) =
    let artistResponse = retrieveJson<ParsedResponse> "parsed.json"

    let artistId = artistResponse.Head.id

    let urlMapping =
        query
        |> List.mapi (fun i s ->
            match i with
            | 0 ->
                match parseIntStrOption s with
                | Some n when n > 0 && n <= 50 -> sprintf "&limit=%s" s
                | _ -> ""
            | 1 ->
                match parseIntStrOption s with
                | Some n when n >= 0 -> sprintf "&offset=%s" s
                | _ -> ""
            | _ -> "")

    urlMapping
    |> List.fold
        (fun (out: string) (next: string) -> out + next)
        (sprintf "%s/artists/%s/albums?include_groups=album" BASE_URL artistId)
    |> sendGetRequest

    retrieveJson<PagesOf<Album>> "api.json" |> parseAlbum
