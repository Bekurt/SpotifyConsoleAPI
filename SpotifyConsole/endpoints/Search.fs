module SpotifyConsole.Search

open System
open Base

type FilterType =
    | Album
    | Artist
    | Playlist
    | Track

let stringToType t =
    match t with
    | "album" -> Some Album
    | "artist" -> Some Artist
    | "playlist" -> Some Playlist
    | "track" -> Some Track
    | _ -> None

let parseIntStrOption (s: string) =
    match s with
    | "" -> None
    | s -> Some(Int32.Parse s)

let private isValidQuery (query: list<string>) =
    let isFirstPresent = query.Length > 0

    let isValidFilter =
        if query.Length >= 2 then
            match stringToType (query.Item 1) with
            | Some _ -> true
            | None -> false
        else
            true

    let isValidLimit =
        if query.Length >= 3 then
            match parseIntStrOption (query.Item 2) with
            | Some n when n > 0 && n <= 50 -> true
            | _ -> false
        else
            true

    let isValidOffset =
        if query.Length >= 4 then
            match parseIntStrOption (query.Item 3) with
            | Some n when n >= 0 -> true
            | _ -> false
        else
            true

    if not isFirstPresent then
        printfn "Missing query argument"

    if not isValidFilter then
        printfn "valid filter options are album, artist, playlist or track"

    if not isValidLimit then
        printfn "limit must be an integer between 1 and 50"

    if not isValidOffset then
        printfn "offset must be a non-negative integer"

    isFirstPresent && isValidFilter && isValidLimit && isValidOffset

let searchAsync (query: list<string>) =
    let url =
        if isValidQuery query then
            sprintf
                "%s/search?q=%s&type=%s&limit=%s&offset%s"
                BASE_URL
                (Uri.EscapeDataString(query.Item 0))
                (Uri.EscapeDataString(query.Item 1))
                (query.Item 2)
                (query.Item 3)
        else
            failwith "Invalid query"

    sendGetRequest url
