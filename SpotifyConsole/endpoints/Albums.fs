module SpotifyConsole.Albums

open Base
open Parsers

let getAlbumTracks (query: list<string>) =
    let albumResponse = retrieveJson<ParsedResponse> "parsed.json"

    let albumIdx =
        if query.Length > 0 then
            parseIntStrOption (query.Item 0) |> Option.get
        else
            0

    let albumId =
        if albumResponse.Length > albumIdx then
            (albumResponse.Item albumIdx).id
        else
            failwithf "WRONG INDEX IDIOT"
            ""

    let offset = if query.Length > 1 then query.Item 1 else "0"

    sprintf "%s/albums/%s/tracks?limit=50&offset=%s" BASE_URL albumId offset
    |> sendGetRequest

    retrieveJson<PagesOf<AlbumTrack>> "api.json"
    |> parsePagesOfAlbumTracks (albumResponse.Item albumIdx).album
