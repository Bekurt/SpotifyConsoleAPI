module SpotifyConsole.Tracks

open Base
open Parsers

let getTracks (query: list<string>) =
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
    |> List.fold (fun (out: string) (next: string) -> out + next) (sprintf "%s/me/tracks" BASE_URL)
    |> sendGetRequest

    retrieveJson<PagesOf<SavedTrack>> "api.json" |> parsePagesOfSavedTracks


type SaveBody = { ids: list<string> }

let saveTracks () =
    let tracks = retrieveJson<ParsedResponse> "parsed.json"

    let body = { ids = tracks |> List.map (fun i -> i.id) }

    sendPutRequest<SaveBody> (sprintf "%s/me/tracks" BASE_URL) body

let deleteTracks () =
    let tracks = retrieveJson<ParsedResponse> "parsed.json"

    let body: SaveBody = { ids = tracks |> List.map (fun i -> i.id) }

    sendDeleteRequest<SaveBody> (sprintf "%s/me/tracks" BASE_URL) (Some body)

let checkTracks () =
    let tracks = retrieveJson<list<ParsedItem>> "parsed.json"

    tracks
    |> List.fold (fun s i -> s + i.id + ",") ""
    |> sprintf "%s/me/tracks/contains?ids=%s" BASE_URL
    |> sendGetRequestArrayResponse (Some true)

    parseCheckTracks ()
