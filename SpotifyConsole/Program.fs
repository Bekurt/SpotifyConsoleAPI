module SpotifyConsole.Program

open System

let commandInterpreter argv =
    match argv |> Array.toList with
    | "auth" :: _ ->
        Auth.authorizeAsync () |> Async.AwaitTask |> Async.RunSynchronously
        0
    | "search" :: query when query.Length > 0 ->
        Search.searchAsync query |> Async.AwaitTask |> Async.RunSynchronously
        0
    | _ ->
        printfn "Command not available."
        1

let rec interactiveLoop () =
    printf "> "

    match Console.ReadLine().Split(" - ") |> Array.toList with
    | "exit" :: _ -> 0
    | "quit" :: _ -> 0
    | cmd ->
        let result = commandInterpreter (cmd |> Array.ofList)
        interactiveLoop |> ignore
        result


[<EntryPoint>]
let main argv =
    try
        match argv |> Array.toList with
        | "interactive" :: _ ->
            printfn "Interactive mode. Type 'exit' to quit."
            interactiveLoop ()
        | _ -> commandInterpreter argv
    with ex ->
        printfn "Error: %s" ex.Message
        1
