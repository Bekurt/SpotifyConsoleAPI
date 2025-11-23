module SpotifyConsole.Artists

open Base
open Parsers

let getArtistAlbums () =
    let artistResponse = retrieveJson<ParsedResponse> "parsed.json"

    let artistId = artistResponse.Head.id
    sendGetRequest (sprintf "%s/artists/%s/albums?include_groups=album&limit=50" BASE_URL artistId)

    let response = retrieveJson<PagesOf<Album>> "api.json"
    let mutable next = response.next

    parsePagesOfAlbums response

    retrieveJson<ParsedResponse> "parsed.json"
    |> writeJson<ParsedResponse> "albums.json"

    while not (isNull next) do
        sendGetRequest next
        let response = retrieveJson<PagesOf<Album>> "api.json"
        parsePagesOfAlbums response
        let newResponse = retrieveJson<ParsedResponse> "parsed.json"
        let oldResponse = retrieveJson<ParsedResponse> "albums.json"

        oldResponse @ newResponse |> writeJson<ParsedResponse> "albums.json"
        next <- response.next
