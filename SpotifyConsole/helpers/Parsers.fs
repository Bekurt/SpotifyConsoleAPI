module SpotifyConsole.Parsers

open Base

type PagesOf<'Item> =
    { limit: int
      next: string
      offset: int
      previous: string
      total: int
      items: list<'Item> }

type Artist = { id: string; name: string }

type Album =
    { album_type: string
      total_tracks: int
      id: string
      name: string
      release_date: string
      artists: list<Artist> }

type Track =
    { artists: list<Artist>
      album: Album
      disc_number: int
      duration_ms: int
      id: string
      name: string
      track_number: int }

type SavedTrack = { added_at: string; track: Track }

type SearchResponse =
    { tracks: PagesOf<Track>
      albums: PagesOf<Album>
      artists: PagesOf<Artist> }

type ParsedItem = { idx: int; id: string; name: string }
type ParsedResponse = list<ParsedItem>

let parsePagesOf<'T> (parserOfT: 'T -> 'T) (pages: PagesOf<'T>) =
    printfn "Found %d results" pages.total

    writeJson
        "api.json"
        { limit = pages.limit
          next = pages.next
          offset = pages.offset
          previous = pages.previous
          total = pages.total
          items = pages.items |> List.map parserOfT }

let albumParser (album: Album) =
    { album_type = album.album_type
      artists = album.artists |> List.map (fun art -> { id = art.id; name = art.name })
      id = album.id
      name = album.name
      total_tracks = album.total_tracks
      release_date = album.release_date }

let artistParser (artist: Artist) = { id = artist.id; name = artist.name }

let trackParser (track: Track) =
    { artists = track.artists |> List.map artistParser
      album = track.album
      disc_number = track.disc_number
      duration_ms = track.duration_ms
      id = track.id
      name = track.name
      track_number = track.track_number }

let savedTrackParser (savedTrack: SavedTrack) =
    let track = savedTrack.track

    { added_at = savedTrack.added_at
      track =
        { artists = track.artists |> List.map artistParser
          album = track.album
          disc_number = track.disc_number
          duration_ms = track.duration_ms
          id = track.id
          name = track.name
          track_number = track.track_number } }

let parseAlbum = parsePagesOf<Album> albumParser
let parseArtist = parsePagesOf<Artist> artistParser
let parseTrack = parsePagesOf<Track> trackParser
let parseSavedTrack = parsePagesOf<SavedTrack> savedTrackParser


type CheckResponse = { items: list<bool> }
type ReadableResponse = { isSaved: bool; name: string }

let parseCheckTracks () =
    let itemList = retrieveJson<CheckResponse> "api.json"
    let inputList = retrieveJson<ParsedResponse> "parsed.json"


    itemList.items
    |> List.mapi (fun idx item ->
        { isSaved = item
          name = (inputList.Item idx).name })
    |> writeJson "api.json"
