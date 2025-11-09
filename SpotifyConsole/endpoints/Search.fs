module SpotifyConsole.Search

open Base

let searchItems (query: list<string>) =
    let urlMapping =
        query
        |> List.mapi (fun i s ->
            match i with
            | 0 -> sprintf "?q=%s" s
            | 1 ->
                match s with
                | "album" -> sprintf "&type=%s" s
                | "artist" -> sprintf "&type=%s" s
                | "playlist" -> sprintf "&type=%s" s
                | "track" -> sprintf "&type=%s" s
                | _ -> "&type=track"
            | 2 ->
                match parseIntStrOption s with
                | Some n when n > 0 && n <= 50 -> sprintf "&limit=%s" s
                | _ -> ""
            | 3 ->
                match parseIntStrOption s with
                | Some n when n >= 0 -> sprintf "&offset=%s" s
                | _ -> ""
            | _ -> "")

    if query.Length > 0 then
        urlMapping
        |> List.fold (fun (out: string) (next: string) -> out + next) (sprintf "%s/search" BASE_URL)
        |> sendGetRequest
    else
        failwith "Query is missing required parameters"
