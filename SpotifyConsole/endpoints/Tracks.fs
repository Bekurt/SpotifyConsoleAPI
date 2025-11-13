module SpotifyConsole.Tracks

open Base
open System
open System.Text.Json
open System.IO

let parseTracks () =
    let itemList = retrieveJson<SavedResponse> "api_response.json"

    printfn "Found %d results" itemList.total

    let parsedList =
        itemList.items |> List.map (fun i -> { id = i.track.id; name = i.track.name })

    let savePath =
        Path.Combine(Environment.CurrentDirectory, "responses/parsed_response.json")

    let opts = JsonSerializerOptions(WriteIndented = true)
    File.WriteAllText(savePath, JsonSerializer.Serialize(parsedList, opts))

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

    parseTracks ()

let saveTracks () =
    let tracks = retrieveJson<ParsedResponse> "parsed_response.json"

    let body: SaveBody = { ids = tracks |> List.map (fun i -> i.id) }

    sendPutRequest<SaveBody> (sprintf "%s/me/tracks" BASE_URL) body

let deleteTracks () =
    let tracks = retrieveJson<ParsedResponse> "parsed_response.json"

    let body: SaveBody = { ids = tracks |> List.map (fun i -> i.id) }

    sendDeleteRequest<SaveBody> (sprintf "%s/me/tracks" BASE_URL) (Some body)

type CheckResponse = { items: list<bool> }
type ReadableResponse = { isSaved: bool; name: string }

let makeReadable () =
    let itemList = retrieveJson<CheckResponse> "api_response.json"
    let inputList = retrieveJson<ParsedResponse> "parsed_response.json"

    let parsedList =
        itemList.items
        |> List.mapi (fun idx item ->
            { isSaved = item
              name = (inputList.Item idx).name })

    let savePath =
        Path.Combine(Environment.CurrentDirectory, "responses/api_response.json")

    let opts = JsonSerializerOptions(WriteIndented = true)
    File.WriteAllText(savePath, JsonSerializer.Serialize(parsedList, opts))

let checkTracks () =
    let tracks = retrieveJson<list<Item>> "parsed_response.json"

    tracks
    |> List.fold (fun s i -> s + i.id + ",") ""
    |> sprintf "%s/me/tracks/contains?ids=%s" BASE_URL
    |> sendGetRequestArrayResponse (Some true)

    makeReadable ()
