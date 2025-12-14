module SpotifyConsole.Tracks

open Base
open Parsers
open System

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

let getAllTracks () =
    Handlers.clearResponse ()
    getTracks [ "50"; "0" ]

    let response = retrieveJson<PagesOf<SavedTrack>> "api.json"
    parsePagesOfSavedTracks response
    Handlers.allToTheFold ()
    let mutable next = response.next

    while not (isNull next) do
        sendGetRequest next
        let response = retrieveJson<PagesOf<SavedTrack>> "api.json"
        parsePagesOfSavedTracks response
        Handlers.allToTheFold ()
        next <- response.next

let saveTrackList () =
    retrieveJson<ParsedResponse> "fold.json"
    |> writeJson<ParsedResponse> "saved.json"

let saveArtistList () =
    let allSavedTracks = retrieveJson<ParsedResponse> "fold.json"

    allSavedTracks
    |> List.map (fun item -> item.artist)
    |> List.distinct
    |> writeJson<list<string>> "artists.json"

type SaveBodyItem = { id: string; added_at: string }
type SaveBody = { timestamped_ids: list<SaveBodyItem> }
type DeleteBody = { ids: list<string> }

let saveTracks () =
    retrieveJson<ParsedResponse> "fold.json"
    |> List.chunkBySize 50
    |> List.iter (fun chunk ->
        let body =
            { timestamped_ids =
                chunk
                |> List.map (fun item ->
                    { id = item.id
                      added_at = DateTime.UtcNow.AddMinutes(float item.idx).ToString "o" }) }

        sendPutRequest<SaveBody> (sprintf "%s/me/tracks" BASE_URL) body)

let deleteTracks () =
    retrieveJson<ParsedResponse> "fold.json"
    |> List.chunkBySize 50
    |> List.iter (fun chunk ->
        let body = { ids = chunk |> List.map (fun i -> i.id) }
        sendDeleteRequest<DeleteBody> (sprintf "%s/me/tracks" BASE_URL) (Some body))
