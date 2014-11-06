namespace LesMots

open System
open System.Configuration

/// <summary>
/// Loads settings from app/web.config, fallbacks to defaults for defined properties
/// </summary>
type Settings() = 
    static member RedisConnectionString 
        with get () : string = Settings.GetOrDefault("CrawlerRedisConnectionString","localhost,resolveDns=true")
    static member RedisPrefix
        with get () : string = Settings.GetOrDefault("CrawlerRedisPrefix","WC")
    static member DBMainConnectionString
        with get () : string = Settings.GetOrDefault("DBMainConnectionString","")
    static member DBShardsConnectionString
        with get () : string = Settings.GetOrDefault("DBShardsConnectionString", "")
    static member UserAgent 
        with get() : string = Settings.GetOrDefault("CrawlerUserAgent","Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/36.0.1985.125 Safari/537.36 (yo.buybackoff.com/bot.html)")
    static member FetchTimeout 
        with get() : int = Int32.Parse(Settings.GetOrDefault("CrawlerFetchTimeout","30000"))
    static member RobotsTtl 
        with get() : int = Int32.Parse(Settings.GetOrDefault("CrawlerRobotsTtl","21600"))
    static member DefaultDelay 
        with get() : int = Int32.Parse(Settings.GetOrDefault("CrawlerDefaultDelay","15"))
    static member HostsKey 
        with get() : string = Settings.GetOrDefault("CrawlerHostsKey","hosts")
    static member RobotsKey 
        with get() : string = Settings.GetOrDefault("CrawlerRobotsKey","hosts:robots")
    
    static member GetOrDefault(key:string, defaultValue:string):string =
        Ractor.Config.GetSettingOrDefault(key, defaultValue)
        


