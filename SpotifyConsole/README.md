# SpotifyConsole (F#)

Small F# console app demonstrating how to authenticate to the Spotify Web API using the Client Credentials flow and execute a simple search request.


Prerequisites
- .NET SDK 8.0 or newer installed (dotnet)
- A Spotify application client id and client secret: https://developer.spotify.com/dashboard/

Setup
1. Create an app in the Spotify Developer Dashboard and copy the Client ID and Client Secret.
2. In your app settings, add the redirect URI: http://localhost:5000/callback
	- The app must have that redirect URI registered for the Authorization Code flow to work.
3. Set environment variables in your shell:

```bash
export SPOTIFY_CLIENT_ID="your-client-id"
export SPOTIFY_CLIENT_SECRET="your-client-secret"
```

On Windows (PowerShell):

```powershell
$env:SPOTIFY_CLIENT_ID = "your-client-id"
$env:SPOTIFY_CLIENT_SECRET = "your-client-secret"
```

Build & run

From the workspace root:

```bash
dotnet build ./SpotifyConsole/SpotifyConsole.fsproj
dotnet run --project ./SpotifyConsole/SpotifyConsole.fsproj -- auth
dotnet run --project ./SpotifyConsole/SpotifyConsole.fsproj -- search "Radiohead Creep"
```

Commands
- auth — launches your browser to authenticate the app and stores tokens in `spotify_tokens.json` in the current directory. Make sure `http://localhost:5000/callback` is registered in the Spotify dashboard.
- search <query> — searches for tracks (uses saved user token if present and valid; otherwise attempts refresh; if none, falls back to Client Credentials token).

Notes & caveats
- This example demonstrates the Authorization Code flow with a simple local HTTP listener. On Windows you may need appropriate permissions to listen on a port. If binding fails, run the app as administrator or choose a registered URL ACL.
- Tokens are stored unencrypted in `spotify_tokens.json` for demo purposes. For production, secure storage is required.

License: MIT
