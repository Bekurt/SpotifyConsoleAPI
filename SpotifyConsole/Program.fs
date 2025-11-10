module SpotifyConsole.Program

open System
open Base

let commandList = [ "auth"; "search"; "tracks"; "user"; "next"; "prev" ]
let tracksCmdList = [ "get"; "save" ]
let userCmdList = [ "top" ]

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
    | "search" :: _ -> printfn "Search item. Query structure -> search-name-type-limit-offset"
    | "tracks" :: [] -> printCmdList tracksCmdList (Some "Endpoint for saved tracks.\nAvailable commands are:")
    | "tracks" :: subPath :: _ ->
        match subPath with
        | "get" -> printfn "Get saved tracks. Query structure -> tracks-get-limit-offset"
        | "save" -> printfn "Add tracks to saved. Query structure -> tracks-save"
        | _ -> noCommandFound subPath
    | "user" :: [] -> printCmdList userCmdList (Some "Endpoint for user actions.\nAvailable commands are:")
    | "user" :: subPath :: _ ->
        match subPath with
        | "top" -> printfn "Get top items. Query structure -> user-top-type-timerange-limit-offset"
        | _ -> noCommandFound subPath
    | "next" :: _ -> printfn "Go to the next page of the last request"
    | "prev" :: _ -> printfn "Go to the previous page of the last request"
    | other :: _ -> noCommandFound other
    | _ -> printCmdList commandList None

let commandInterpreter argList =
    match argList with
    | "help" :: subCommand -> commandHelper subCommand
    | "auth" :: _ -> Auth.authorizeAsync () |> Async.AwaitTask |> Async.RunSynchronously
    | "search" :: query -> Search.searchItems query
    | "tracks" :: subPath :: query ->
        match subPath with
        | "get" -> Tracks.getUsersSavedTracks query
        | "save" -> Tracks.saveTracks ()
        | _ -> noCommandFound subPath
    | "user" :: subPath :: query ->
        match subPath with
        | "top" -> Users.getUsersTopItems query
        | _ -> noCommandFound subPath
    | "next" :: _ -> sendNextRequest ()
    | "prev" :: _ -> sendPreviousRequest ()
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
