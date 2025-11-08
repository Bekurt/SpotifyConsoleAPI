module SpotifyConsole.Program

open System
open System.IO
open System.Text.Json

let commandInterpreter argList =
    match argList with
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

    match Console.ReadLine().Split "-" |> Array.toList with
    | "exit" :: _ -> 0
    | "quit" :: _ -> 0
    | cmd ->
        let result = commandInterpreter cmd
        interactiveLoop () |> ignore
        result

let retrieveJson<'T> (filename: string) =
    let path = Path.Combine(Environment.CurrentDirectory, "responses", filename)

    if File.Exists(path) then
        try
            let text = File.ReadAllText path
            let opts = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
            Some(JsonSerializer.Deserialize<'T>(text, opts))
        with _ ->
            None
    else
        printfn "File %s does not exist." filename
        None

[<EntryPoint>]
let main argv =
    try
        printf "Welcome to the interactive console. Type 'exit' or 'quit' to exit\n"
        interactiveLoop ()
    with ex ->
        printfn "Error: %s" ex.Message
        1
