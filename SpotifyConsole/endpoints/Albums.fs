module SpotifyConsole.Albums

open Base
open Parsers

let getAlbumTracks () =
    Handlers.clearResponse ()
    let albumResponse = retrieveJson<ParsedResponse> "parsed.json"

    albumResponse
    |> List.iter (fun album ->
        sprintf "%s/albums/%s/tracks?limit=50" BASE_URL album.id |> sendGetRequest
        let response = retrieveJson<PagesOf<AlbumTrack>> "api.json"

        response |> parsePagesOfAlbumTracks album.album
        Handlers.allToTheFold ()

        let mutable next = response.next

        while not (isNull next) do
            sendGetRequest next
            let response = retrieveJson<PagesOf<AlbumTrack>> "api.json"

            response |> parsePagesOfAlbumTracks album.album
            Handlers.allToTheFold ()

            next <- response.next)
