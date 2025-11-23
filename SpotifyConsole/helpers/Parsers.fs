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

type AlbumTrack =
    { artists: list<Artist>
      disc_number: int
      duration_ms: int
      id: string
      name: string
      track_number: int }

type SavedTrack = { added_at: string; track: Track }

type TrackSearch = { tracks: PagesOf<Track> }
type AlbumSearch = { albums: PagesOf<Album> }
type ArtistSearch = { artists: PagesOf<Artist> }

type ParsedItem =
    { idx: int
      id: string
      track: string
      album: string
      artist: string }

type ParsedResponse = list<ParsedItem>

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
      album = track.album |> albumParser
      disc_number = track.disc_number
      duration_ms = track.duration_ms
      id = track.id
      name = track.name
      track_number = track.track_number }

let albumTrackParser (track: AlbumTrack) =
    { artists = track.artists |> List.map artistParser
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
          album = track.album |> albumParser
          disc_number = track.disc_number
          duration_ms = track.duration_ms
          id = track.id
          name = track.name
          track_number = track.track_number } }


let parsePagesOfArtists (pages: PagesOf<Artist>) =
    printfn "Found %d results" pages.total

    writeJson
        "api.json"
        { limit = pages.limit
          next = pages.next
          offset = pages.offset
          previous = pages.previous
          total = pages.total
          items = pages.items |> List.map artistParser }

    pages.items
    |> List.mapi (fun idx item ->
        { idx = idx
          id = item.id
          track = ""
          album = ""
          artist = item.name })
    |> writeJson "parsed.json"

let parsePagesOfAlbums (pages: PagesOf<Album>) =
    printfn "Found %d results" pages.total

    writeJson
        "api.json"
        { limit = pages.limit
          next = pages.next
          offset = pages.offset
          previous = pages.previous
          total = pages.total
          items = pages.items |> List.map albumParser }

    pages.items
    |> List.mapi (fun idx item ->
        { idx = idx
          id = item.id
          track = ""
          album = item.name
          artist = item.artists.Head.name })
    |> writeJson "parsed.json"

let parsePagesOfTracks (pages: PagesOf<Track>) =
    printfn "Found %d results" pages.total

    writeJson
        "api.json"
        { limit = pages.limit
          next = pages.next
          offset = pages.offset
          previous = pages.previous
          total = pages.total
          items = pages.items |> List.map trackParser }

    pages.items
    |> List.mapi (fun idx item ->
        { idx = idx
          id = item.id
          track = item.name
          album = item.album.name
          artist = item.artists.Head.name })
    |> writeJson "parsed.json"

let parsePagesOfAlbumTracks (album: string) (pages: PagesOf<AlbumTrack>) =
    printfn "Found %d results" pages.total

    writeJson
        "api.json"
        { limit = pages.limit
          next = pages.next
          offset = pages.offset
          previous = pages.previous
          total = pages.total
          items = pages.items |> List.map albumTrackParser }

    pages.items
    |> List.mapi (fun idx item ->
        { idx = idx
          id = item.id
          track = item.name
          album = album
          artist = item.artists.Head.name })
    |> writeJson "parsed.json"

let parsePagesOfSavedTracks (pages: PagesOf<SavedTrack>) =
    printfn "Found %d results" pages.total

    writeJson
        "api.json"
        { limit = pages.limit
          next = pages.next
          offset = pages.offset
          previous = pages.previous
          total = pages.total
          items = pages.items |> List.map savedTrackParser }

    pages.items
    |> List.mapi (fun idx item ->
        { idx = idx
          id = item.track.id
          track = item.track.name
          album = item.track.album.name
          artist = item.track.artists.Head.name })
    |> writeJson "parsed.json"
