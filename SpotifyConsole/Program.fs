module SpotifyConsole.Program

open System

[<EntryPoint>]
let main argv =
    try
        let clientId = "e1db6991cfe64c6b85b848fed5a90419"
        let clientSecret = "48015b50089e4a748c6fea70bdcacd90"

        if String.IsNullOrWhiteSpace(clientId) || String.IsNullOrWhiteSpace(clientSecret) then
            printfn "Please set SPOTIFY_CLIENT_ID and SPOTIFY_CLIENT_SECRET environment variables."
            printfn "See README.md for instructions."
            1
        else
            match argv |> Array.toList with
            | "auth" :: _ ->
                // Default redirect URI -- must be registered in your Spotify app
                let redirectUri = "http://localhost:5000/callback"
                let scopes = "user-read-private user-read-email" // example scopes

                Auth.authorizeAsync clientId clientSecret redirectUri scopes
                |> Async.AwaitTask
                |> Async.RunSynchronously

                0
            | "search" :: rest when rest.Length > 0 ->
                let query = String.Join(" ", rest)

                let token =
                    Auth.getAccessToken clientId clientSecret "http://localhost:5000/callback"

                let tracks =
                    Search.searchTracksAsync token query
                    |> Async.AwaitTask
                    |> Async.RunSynchronously

                if tracks.Length = 0 then
                    printfn "No tracks found for '%s'" query
                else
                    printfn "Top %d results for '%s':" tracks.Length query

                    tracks
                    |> Array.iteri (fun i (name, artists, url) -> printfn "%d) %s — %s\n   %s" (i + 1) name artists url)

                0
            | _ ->
                printfn "Usage: dotnet run -- auth"
                printfn "       dotnet run -- search <query>"
                printfn "Notes: For `auth` ensure your app's Redirect URI includes http://localhost:5000/callback"
                1
    with ex ->
        printfn "Error: %s" (ex.Message)
        1
