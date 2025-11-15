module SpotifyConsole.Handlers

open Base
open System
open System.Text
open System.Text.Json
open System.IO

let cumulateResponse () =
    let newResponse = retrieveJson<ParsedResponse> "parsed_response.json"
    let currentResponse = retrieveJson<ParsedResponse> "cumulative_response.json"

    writeJson "cumulative_response.json" (currentResponse @ newResponse)

let clearResponse () =
    [] |> writeJson "cumulative_response.json"
