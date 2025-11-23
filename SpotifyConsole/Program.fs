module SpotifyConsole.Program

open System

let commandList =
    [ "auth"; "albums"; "artists"; "search"; "tracks"; "user"; "resp" ]

let albumsCmdList = [ "tracks" ]
let artistCmdList = [ "albums" ]
let tracksCmdList = [ "get"; "save"; "delete" ]
let userCmdList = [ "top" ]
let respCmdList = [ "join"; "joinAll"; "cut"; "clear"; "filter"; "shuffle"; "load" ]

let noCommandFound (cmd: string) = printfn "%s has no matches" cmd

let printCmdList (list: list<string>) (optionalString: string option) =
    let startString =
        match optionalString with
        | Some(s) -> s
        | None -> "Available commands are:"

    list
    |> List.fold (fun (out: string) (next: string) -> sprintf "%s\n- %s" out next) startString
    |> printfn "%s"

let commandHelper argList =
    match argList with
    | "auth" :: _ -> printfn "Autenticate to the API. No other commands needed."
    | "albums" :: [] -> printCmdList albumsCmdList (Some "Endpoint for album queries.\nAvailable commands are:")
    | "albums" :: subPath :: _ ->
        match subPath with
        | "tracks" -> printfn "Get album tracks. Query structure -> albums tracks limit offset"
        | _ -> noCommandFound subPath
    | "artists" :: [] -> printCmdList artistCmdList (Some "Endpoint for artist queries.\nAvailable commands are:")
    | "artists" :: subPath :: _ ->
        match subPath with
        | "albums" -> printfn "Get artit albums. Query structure -> artist albums limit offset"
        | _ -> noCommandFound subPath
    | "search" :: _ -> printfn "Search item. Query structure -> search name type limit offset"
    | "tracks" :: [] -> printCmdList tracksCmdList (Some "Endpoint for saved tracks.\nAvailable commands are:")
    | "tracks" :: subPath :: _ ->
        match subPath with
        | "get" -> printfn "Get saved tracks. Query structure -> tracks get limit offset OR tracks get all"
        | "save" -> printfn "Add tracks to saved. Query structure -> tracks save"
        | "delete" -> printfn "Delete tracks from saved. Query structure -> tracks delete"
        | _ -> noCommandFound subPath
    | "user" :: [] -> printCmdList userCmdList (Some "Endpoint for user actions.\nAvailable commands are:")
    | "user" :: subPath :: _ ->
        match subPath with
        | "top" -> printfn "Get top items. Query structure -> user top type timerange limit offset"
        | _ -> noCommandFound subPath
    | "resp" :: [] -> printCmdList respCmdList (Some "Response handling.\nAvailable commands are:")
    | "resp" :: subPath :: _ ->
        match subPath with
        | "joinAll" -> printfn "Add entire parsed response to fold.json"
        | "join" -> printfn "Adds selected indexes to fold.json"
        | "cut" -> printfn "Remove selected indexes from fold.json"
        | "clear" -> printfn "Clears fold.json"
        | "filter" -> printfn "Keep only selected indexes from parsed.json"
        | "load" -> printfn "Move saved.json into fold.json"
        | "shuffle" -> printfn "Shuffle saved tracks"
        | _ -> noCommandFound subPath
    | other :: _ -> noCommandFound other
    | _ -> printCmdList commandList None

let commandInterpreter argList =
    match argList with
    | "help" :: subCommand -> commandHelper subCommand
    | "auth" :: _ -> Auth.authorizeAsync () |> Async.AwaitTask |> Async.RunSynchronously
    | "albums" :: subPath :: query ->
        match subPath with
        | "tracks" -> Albums.getAlbumTracks query
        | _ -> noCommandFound subPath
    | "artists" :: subPath :: query ->
        match subPath with
        | "albums" -> Artists.getArtistAlbums query
        | _ -> noCommandFound subPath
    | "search" :: query -> Search.searchItems query
    | "tracks" :: subPath :: query ->
        match subPath with
        | "get" ->
            match query with
            | [ "all" ] -> Tracks.getAllTracks ()
            | _ -> Tracks.getTracks query
        | "save" -> Tracks.saveTracks ()
        | "delete" -> Tracks.deleteTracks ()
        | _ -> noCommandFound subPath
    | "user" :: subPath :: query ->
        match subPath with
        | "top" -> Users.getUsersTopItems query
        | _ -> noCommandFound subPath
    | "resp" :: subPath :: query ->
        match subPath with
        | "joinAll" -> Handlers.allToTheFold ()
        | "join" -> Handlers.joinTheFold query
        | "cut" -> Handlers.leaveTheFold query
        | "clear" -> Handlers.clearResponse ()
        | "filter" -> Handlers.filterResponse query
        | "load" -> Handlers.moveSavedToFold ()
        | "shuffle" -> Handlers.shuffleSavedTracks ()
        | _ -> noCommandFound subPath
    | other :: _ -> noCommandFound other
    | _ -> printfn "You need to send an instruction"

let rec interactiveLoop () =
    printf "> "

    let sep = "-"

    match Console.ReadLine().Split sep |> Array.toList with
    | "exit" :: _ -> printfn "Goodbye"
    | "quit" :: _ -> printfn "Goodbye"
    | cmd ->
        let result = commandInterpreter cmd
        interactiveLoop ()

[<EntryPoint>]
let main argv =
    try
        //printf "Welcome to the interactive console. Type 'exit' or 'quit' to exit\n"
        //interactiveLoop ()
        commandInterpreter (argv |> Array.toList)
        0
    with ex ->
        printfn "Error: %s\n" ex.Message
        //interactiveLoop ()
        1
