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

    while not (isNull next) do
        let oldResponse = retrieveJson<ParsedResponse> "parsed.json"
        sendGetRequest next
        let response = retrieveJson<PagesOf<Album>> "api.json"
        parsePagesOfAlbums response
        let newResponse = retrieveJson<ParsedResponse> "parsed.json"


        oldResponse @ newResponse
        |> List.mapi (fun r_idx item -> { item with idx = r_idx })
        |> writeJson<ParsedResponse> "parsed.json"

        next <- response.next
