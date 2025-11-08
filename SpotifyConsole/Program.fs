module SpotifyConsole.Program

open System

let commandInterpreter argList =
    match argList with
    | "auth" :: _ -> Auth.authorizeAsync () |> Async.AwaitTask |> Async.RunSynchronously
    | "search" :: query -> Search.searchAsync query
    | "user" :: subPath :: query ->
        match subPath with
        | "top" -> Users.getUsersTopItemsAsync query
        | _ -> printfn "%s has no matches" subPath
    | "next" :: _ -> Base.sendNextRequest ()
    | "prev" :: _ -> Base.sendPreviousRequest ()
    | _ -> printfn "Command not available."

let rec interactiveLoop () =
    printf "> "

    match Console.ReadLine().Split "-" |> Array.toList with
    | "exit" :: _ -> printfn "Goodbye"
    | "quit" :: _ -> printfn "Goodbye"
    | cmd ->
        let result = commandInterpreter cmd
        interactiveLoop ()

[<EntryPoint>]
let main argv =
    try
        printf "Welcome to the interactive console. Type 'exit' or 'quit' to exit\n"
        interactiveLoop ()
        0
    with ex ->
        printfn "Error: %s\n" ex.Message
        interactiveLoop ()
        1
