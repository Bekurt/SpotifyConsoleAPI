module SpotifyConsole.Program

open System
open System.IO
open System.Text.Json

let commandInterpreter argList =
    match argList with
    | "auth" :: _ -> Auth.authorizeAsync () |> Async.AwaitTask |> Async.RunSynchronously
    | "search" :: query -> Search.searchAsync query
    | "user" :: subPath :: query ->
        match subPath with
        | "top" -> Users.getUsersTopItemsAsync query
        | _ -> printfn "%s has no matches" subPath
    | _ -> printfn "Command not available."

let rec interactiveLoop () =
    printf "> "

    match Console.ReadLine().Split "-" |> Array.toList with
    | "exit" :: _ -> printfn "Goodbye"
    | "quit" :: _ -> printfn "Goodbye"
    | cmd ->
        let result = commandInterpreter cmd
        interactiveLoop ()

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
        0
    with ex ->
        printfn "Error: %s\n" ex.Message
        interactiveLoop ()
        1
