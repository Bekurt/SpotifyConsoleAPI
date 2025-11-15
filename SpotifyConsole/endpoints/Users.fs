module SpotifyConsole.Users

open Base
open Parsers

let getUsersTopItems (query: list<string>) =
    let urlMapping =
        query
        |> List.mapi (fun i s ->
            match i with
            | 0 ->
                match s with
                | "artists" -> sprintf "%s?" s
                | "tracks" -> sprintf "%s?" s
                | _ -> "tracks?"
            | 1 ->
                match s with
                | "long" -> sprintf "time_range=%s_term" s
                | "medium" -> sprintf "time_range=%s_term" s
                | "short" -> sprintf "time_range=%s_term" s
                | _ -> "time_range=medium_term"
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
        |> List.fold (fun (out: string) (next: string) -> out + next) (sprintf "%s/me/top/" BASE_URL)
        |> sendGetRequest

        match urlMapping.Item 0 with
        | "tracks?" -> retrieveJson<PagesOf<Track>> "api.json" |> parsePagesOfTracks
        | "artists?" -> retrieveJson<PagesOf<Artist>> "api.json" |> parsePagesOfArtists
        | _ -> retrieveJson<PagesOf<Track>> "api.json" |> parsePagesOfTracks
    else
        failwith "Query is missing required parameters"
