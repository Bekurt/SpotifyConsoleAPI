module SpotifyConsole.Handlers

open Base
open Parsers
open System
open System.Text
open System.Text.Json
open System.IO

let cumulateResponse () =
    let newResponse = retrieveJson<ParsedResponse> "parsed.json"
    let currentResponse = retrieveJson<ParsedResponse> "folded.json"

    currentResponse @ newResponse
    |> List.mapi (fun r_idx item -> { item with idx = r_idx })
    |> writeJson "fold.json"

let clearResponse () = [] |> writeJson "fold.json"

let selectFromResponse (cmd: list<string>) =
    let intIdxList =
        cmd
        |> List.map (fun s ->
            match parseIntStrOption s with
            | Some i -> i
            | None -> 0)

    retrieveJson<ParsedResponse> "parsed.json"
    |> List.filter (fun i -> intIdxList |> List.contains i.idx)
