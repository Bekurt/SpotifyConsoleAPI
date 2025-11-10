module SpotifyConsole.Tracks

open Base
open System
open System.Text.Json
open System.IO

type Track = { id: string; name: string }
type SavedItem = { added_at: string; track: Track }
type SavedResponse = { items: list<SavedItem> }

let parseSavedTracks () =
    let itemList = retrieveJson<SavedResponse> "api_response.json"

    let parsedList =
        itemList.items |> List.map (fun i -> { id = i.track.id; name = i.track.name })

    let savePath =
        Path.Combine(Environment.CurrentDirectory, "responses/parsed_response.json")

    let opts = JsonSerializerOptions(WriteIndented = true)
    File.WriteAllText(savePath, JsonSerializer.Serialize(parsedList, opts))

let getUsersSavedTracks (query: list<string>) =
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

    parseSavedTracks ()

type SaveBody = { ids: list<string> }

let saveTracks () =
    let tracks = retrieveJson<list<ParsedResponse>> "parsed_response.json"

    let body: SaveBody = { ids = tracks |> List.map (fun i -> i.id) }

    sendPutRequest<SaveBody> (sprintf "%s/me/tracks" BASE_URL) body
