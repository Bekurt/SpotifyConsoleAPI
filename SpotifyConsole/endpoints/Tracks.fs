module SpotifyConsole.Tracks

open Base

let getUsersSavedTracks (query: list<string>) =
    let urlMapping =
        query
        |> List.mapi (fun i s ->
            match i with
            | 0 ->
                match parseIntStrOption s with
                | Some n when n > 0 && n <= 50 -> sprintf "?limit=%s" s
                | _ -> ""
            | 1 ->
                match parseIntStrOption s with
                | Some n when n >= 0 -> sprintf "&offset=%s" s
                | _ -> ""
            | _ -> "")


    printfn "Sending request with parameters %A" urlMapping

    urlMapping
    |> List.fold (fun (out: string) (next: string) -> out + next) (sprintf "%s/me/tracks" BASE_URL)
    |> sendGetRequest
