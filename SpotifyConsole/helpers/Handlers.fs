module SpotifyConsole.Handlers

open Base
open Parsers

let selectFromResponse (cmd: list<string>) =
    let intIdxList =
        cmd
        |> List.map (fun s ->
            match parseIntStrOption s with
            | Some i -> i
            | None -> 0)

    retrieveJson<ParsedResponse> "parsed.json"
    |> List.filter (fun i -> intIdxList |> List.contains i.idx)

let clearResponse () = [] |> writeJson "fold.json"

let allToTheFold () =
    let foldResponse = retrieveJson<ParsedResponse> "fold.json"
    let parsedResponse = retrieveJson<ParsedResponse> "parsed.json"

    foldResponse @ parsedResponse
    |> List.mapi (fun r_idx item -> { item with idx = r_idx })
    |> writeJson "fold.json"

let joinTheFold (cmd: list<string>) =
    let foldResponse = retrieveJson<ParsedResponse> "fold.json"

    foldResponse @ selectFromResponse cmd
    |> List.mapi (fun r_idx item -> { item with idx = r_idx })
    |> writeJson "fold.json"

let leaveTheFold (cmd: list<string>) =
    let foldResponse = retrieveJson<ParsedResponse> "fold.json"
    let selectedItemsIdx = cmd |> selectFromResponse |> List.map (fun item -> item.idx)

    foldResponse
    |> List.filter (fun item -> selectedItemsIdx |> List.contains item.idx |> not)
    |> List.mapi (fun r_idx item -> { item with idx = r_idx })
    |> writeJson "fold.json"

let shuffleSavedTracks () =
    let rnd = System.Random()

    retrieveJson<ParsedResponse> "saved.json"
    |> List.sortBy (fun _ -> rnd.Next())
    |> List.mapi (fun r_idx item -> { item with idx = r_idx })
    |> writeJson<ParsedResponse> "saved.json"

let moveSavedToFold () =
    retrieveJson<ParsedResponse> "saved.json"
    |> writeJson<ParsedResponse> "fold.json"
