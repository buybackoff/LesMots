namespace LesMots

open System
open System.IO
open System.IO.Compression
open System.Net
open System.Web
open Ractor

open RobotsTxt

type Settings = {
    RedisConnectionString: string;
    UserAgent : string;
    Timeout: int;
    RobotsTtl: int;
    DefaultDelay: int;
    HostsKey: string;
    RobotsKey: string;
    }

[<CLIMutableAttribute>]
type RobotsContent = 
    {
        /// A String that contains the host name. This is usually the DNS host name or IP address of the server.
        Host : string;
        Url: string;
        LastUpdated : DateTime;
        RawContent : string;
        CrawlDelay : int;
        SitemapUrls: string[];
        NewsSitemapUrls : string[]; // high priority
    }



// type PageVisit
// type PageState - previous state + all page visits = current state
    
    //member val LastVisited = DateTime.MinValue with get, set
    // last visited is not a property of a page, it is a property of a crawler w.r.t. a page
    // page itself exists unaware that some crawlers visit it

module Crawler =
    
    let settings = 
        { 
            RedisConnectionString = "localhost";
            UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.125 Safari/537.36 (yo.buybackoff.com/bot.html)";
            Timeout = 5000;
            DefaultDelay = 30;
            RobotsTtl = 21600; //60 * 60 * 6; // 6 hours
            HostsKey = "hosts";
            RobotsKey = "hosts:robots";
        }

    let redis = new Redis(settings.RedisConnectionString, "crawler")


    /// <summary>
    /// Return content and actual location of the url. Ungzips urls ending with .gz
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
                    wr.UserAgent <- settings.UserAgent
                | :? FileWebRequest as fr -> ()
                | _ -> failwith "not implemented"
                let! respChild = Async.StartChild(req.GetResponseAsync() |> Async.AwaitTask, settings.Timeout)
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
    let tryFetchRobots (url:string) : Async<(RobotsContent)> =
        let uriOk, uri = Uri.TryCreate(url, UriKind.Absolute)
        if not uriOk then failwith "bad host url"
        let hostName = uri.GetLeftPart(UriPartial.Authority)
        async {
            let! fetched, location, content = tryFetchUrl (hostName + "/robots.txt")
            match fetched with
            | false -> 
                let robotsContent = 
                    {   Host = hostName; 
                        Url = location.ToString();
                        LastUpdated = DateTime.UtcNow;
                        RawContent = content;
                        CrawlDelay = settings.DefaultDelay;
                        SitemapUrls = [||];
                        NewsSitemapUrls = [||]
                        }
                return robotsContent
            | true -> 
                let robots = Robots.Load(content)
                let delay = int (robots.CrawlDelay(settings.UserAgent))
                let siteMaps = robots.Sitemaps |> Seq.map (fun x -> x.Value) |> Seq.toArray
                let newsSitemaps = siteMaps |> Seq.filter (fun x -> x.Contains("news")) |> Seq.toArray
                let robotsContent = 
                    {   Host = hostName; 
                        Url = location.ToString();
                        LastUpdated = DateTime.UtcNow;
                        RawContent = content;
                        CrawlDelay = if delay > 0 then delay else settings.DefaultDelay;
                        SitemapUrls = siteMaps;
                        NewsSitemapUrls = newsSitemaps
                        }
                return robotsContent
        }
        
        
    let tryGetRobots (host:string) : Async<RobotsContent> =            
        let uriOk, uri = Uri.TryCreate(host, UriKind.Absolute)
        if not uriOk then failwith "bad host url"
        let hostName = uri.GetLeftPart(UriPartial.Authority)
        async {
            let! robots = redis.HGetAsync<RobotsContent>(settings.HostsKey, hostName) 
                            |> Async.AwaitTask
            match box robots with
            // first time
            | null -> // x when x = Unchecked.defaultof<RobotsContent> -> 
                let! robotsContent = tryFetchRobots hostName
                do! redis.HSetAsync<RobotsContent>(settings.HostsKey, hostName, robotsContent) 
                    |> Async.AwaitTask |> Async.Ignore
                return robotsContent
            // get cached robots
            | :? RobotsContent as cachedRobots ->
                let ageInSeconds = int (DateTime.UtcNow - cachedRobots.LastUpdated).TotalSeconds
                if ageInSeconds > settings.RobotsTtl then
                    let! robotsContent = tryFetchRobots hostName
                    do! redis.HSetAsync<RobotsContent>(settings.HostsKey, hostName, robotsContent) 
                        |> Async.AwaitTask |> Async.Ignore
                    return robotsContent
                else return cachedRobots
        }
