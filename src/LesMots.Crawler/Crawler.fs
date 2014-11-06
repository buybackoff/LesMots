namespace LesMots

open System
open System.IO
open System.IO.Compression
open System.Net
open System.Web
open Ractor
open RobotsTxt


module Crawler =
    
    /// <summary>
    /// Get host name without leading 'www.', e.g. example.com
    /// </summary>
    /// <param name="location">Resolved url location</param>
    let hostName (location:Uri) : string = 
        let host = location.Host
        if host.StartsWith("www.") then host.Substring(4) else host

    /// <summary>
    /// Scheme and the actual host of a page, e.g. http://www.example.com
    /// </summary>
    /// <param name="location">Resolved url location</param>
    let hostLocation (location:Uri) : string = location.GetLeftPart(UriPartial.Authority)


    let redis = new Redis(Settings.RedisConnectionString, Settings.RedisPrefix)


    /// <summary>
    /// Return content and actual location of the url. Ungzips urls ending with .gz.
    /// Used for unconditional fetching of web content.
    /// </summary>
    /// <param name="url">A link to fetch</param>
    let tryFetchUrl (url:string) : Async<(bool * Uri * string)> = 
        async {
            let gzipped = url.EndsWith(".gz")
            try
                let req = WebRequest.Create(url) //:?> HttpWebRequest
                match req with 
                | :? HttpWebRequest  -> 
                    let wr = req :?> HttpWebRequest
                    wr.UserAgent <- Settings.UserAgent
                | :? FileWebRequest as fr -> ()
                | _ -> failwith "not implemented"
                req.Method <- "GET"
                let! respChild = Async.StartChild(req.GetResponseAsync() |> Async.AwaitTask, Settings.FetchTimeout)
                use! resp = respChild
                let location = resp.ResponseUri
                use stream = 
                    if gzipped then
                        new GZipStream(resp.GetResponseStream(), CompressionMode.Decompress) :> Stream
                    else resp.GetResponseStream()
                use reader = new StreamReader(stream)
                let content = reader.ReadToEnd()
                stream.Close()
                return (true, location, content)
            with
            | _ -> return (false, null, null)
        }
        
    
    /// <summary>
    /// Fetch and parse robot.txt from remote host
    /// </summary>
    /// <param name="url">Any url for a host</param>
    let tryFetchRobots (url:string) : Async<(RobotsContentCache)> =
        let uriOk, uri = Uri.TryCreate(url, UriKind.Absolute)
        if not uriOk then failwith "bad host url"
        async {
            let! fetched, location, content = tryFetchUrl (uri.ToString())
            match fetched with
            | false -> 
                let robotsContent = 
                    RobotsContentCache(
                        HostName = hostName uri, 
                        Location = url,
                        LastUpdated = DateTime.UtcNow,
                        RawContent = "",
                        CrawlDelay = Settings.DefaultDelay,
                        SitemapUrls = [||],
                        NewsSitemapUrls = [||]
                    )
                return robotsContent
            | true -> 
                let robots = Robots.Load(content)
                let delay = int (robots.CrawlDelay(Settings.UserAgent))
                let siteMaps = robots.Sitemaps |> Seq.map (fun x -> x.Value) |> Seq.toArray
                let newsSitemaps = siteMaps |> Seq.filter (fun x -> x.Contains("news")) |> Seq.toArray
                let robotsContent = 
                    RobotsContentCache(
                        HostName = hostName location, 
                        Location = location.ToString(),
                        LastUpdated = DateTime.UtcNow,
                        RawContent = content,
                        CrawlDelay = (if delay > 0 then delay else Settings.DefaultDelay),
                        SitemapUrls = siteMaps,
                        NewsSitemapUrls = newsSitemaps
                    )
                return robotsContent
        }
        
    /// <summary>
    /// Get fresh cached robots.txt content or fetch it
    /// </summary>
    /// <param name="url">Remote url</param>
    let tryGetRobots (url:string) : Async<RobotsContentCache> =            
        let uriOk, uri = Uri.TryCreate(url, UriKind.Absolute)
        if not uriOk then failwith "bad host url"
        let hLocation = hostLocation uri
        let hName =  hostName uri
        async {
            let! robots = redis.HGetAsync<RobotsContentCache>(Settings.HostsKey, hName) 
                            |> Async.AwaitTask
            match box robots with
            // first time
            | null -> // x when x = Unchecked.defaultof<RobotsContent> -> 
                let! robotsContent = tryFetchRobots (hLocation + "/robots.txt")
                do! redis.HSetAsync<RobotsContentCache>(Settings.HostsKey, hName, robotsContent) 
                    |> Async.AwaitTask |> Async.Ignore
                return robotsContent
            // get cached robots
            | :? RobotsContentCache as cachedRobots ->
                let ageInSeconds = int (DateTime.UtcNow - cachedRobots.LastUpdated).TotalSeconds
                if ageInSeconds > Settings.RobotsTtl then
                    // TODO fetch stored location, e.g. already redirected to www.
                    let! robotsContent = tryFetchRobots (hLocation + "/robots.txt")
                    do! redis.HSetAsync<RobotsContentCache>(Settings.HostsKey, hName, robotsContent) 
                        |> Async.AwaitTask |> Async.Ignore
                    return robotsContent
                else return cachedRobots
            | _ -> return failwith "never"
        }

    /// <summary>
    /// Check if crawler is allowed to go to a url
    /// </summary>
    let isPathAllowed (url:string) : Async<bool> =
        async {
            let! robots = tryGetRobots url
            let r = Robots.Load(robots.RawContent)
            return r.IsPathAllowed(Settings.UserAgent, url)
        }

    /// <summary>
    /// Unique Url GUID based on MD5 hash
    /// </summary>
    let getUrlGuid (url:string) : Guid = 
        url.MD5Guid()

