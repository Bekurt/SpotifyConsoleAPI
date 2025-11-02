module SpotifyConsole.Search

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Text.Json

let searchTracksAsync (token: string) (query: string) =
    task {
        use http = new HttpClient()
        http.DefaultRequestHeaders.Authorization <- Headers.AuthenticationHeaderValue("Bearer", token)

        let url =
            sprintf "https://api.spotify.com/v1/search?q=%s&type=track&limit=5" (Uri.EscapeDataString(query))

        use! resp = http.GetAsync(url)
        let! body = resp.Content.ReadAsStringAsync()

        if not resp.IsSuccessStatusCode then
            failwithf "Search failed: %d - %s" (int resp.StatusCode) body

        use doc = JsonDocument.Parse(body)
        let items = doc.RootElement.GetProperty("tracks").GetProperty("items")

        let results =
            items.EnumerateArray()
            |> Seq.map (fun t ->
                let name = t.GetProperty("name").GetString()

                let artists =
                    t.GetProperty("artists").EnumerateArray()
                    |> Seq.map (fun a -> a.GetProperty("name").GetString())
                    |> String.concat ", "

                let url = t.GetProperty("external_urls").GetProperty("spotify").GetString()
                (name, artists, url))
            |> Seq.toArray

        return results
    }
