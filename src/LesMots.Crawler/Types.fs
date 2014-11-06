namespace LesMots

open System
open System.IO
open System.IO.Compression
open System.Net
open System.Web
open Ractor

// naming convention
// Types:
// ...Cache - an ephemeral state of data that could be lost at any moment and stored only in cache
// [Object][Action]Event - immutable data objects representing an event, usually stored as distributed
// [Object]Content - same as [object][get content]Event, but with focus on content
// ...State - mutable data objects

// Properties:
// ContentType
// RawContent : string/byte[] - string/bytes returned from fetching content
// RawContentBlobId - Guid of content blob stream
// [Object]ContentId - Guid of [Object]Content POCO

// Url - string address of a page, could be a shortened url, etc.
// Location - string resolved location of a url
// HostName - Uri(location).Host without leading www.
// HostLocation - Uri(location).GetLeftPart(UriPartial.Authority)


/// <summary>
/// Cache of raw robots.txt and parsed values
/// </summary>
[<RedisAttribute(Compressed = true)>]
type RobotsContentCache() = 
        /// <summary>
        /// A String that contains the host name. This is usually the DNS host name or IP address of the server.
        /// </summary>
        member val HostName : string = "" with get, set
        member val Location: string ="" with get, set
        member val LastUpdated : DateTime = Unchecked.defaultof<DateTime> with get, set
        member val RawContent : string = "" with get, set
        member val CrawlDelay : int = Settings.DefaultDelay with get, set
        member val SitemapUrls: string[] = [||] with get, set
        member val NewsSitemapUrls : string[] = [||] with get, set // high priority

type PageFetchEvent() =
    member val x = ""
    // RawContentBlobId - store content to Blob
    // LoadTime - client-side load time
    // ResponseStatus
    // ContentType
    // IsError
    // Url
    // Location, null if same as Url
    // HostName (index)

// type PageVisit

// PageVisit
(*
- ResponseTime in msecs, client side measured (StopWatch)
- ResponseStatus
- 

*)

//[<CLIMutableAttribute>]
//[<RedisAttribute(Compressed = true)>]
//type PageVisit = 
//    {
//        /// A String that contains the host name. This is usually the DNS host name or IP address of the server.
//        HostAddress : string;
//        Url: string;
//        LastUpdated : DateTime;
//        RawContent : string;
//        CrawlDelay : int;
//        SitemapUrls: string[];
//        NewsSitemapUrls : string[]; // high priority
//    }

// type PageState - previous state + all page visits = current state
    
    //member val LastVisited = DateTime.MinValue with get, set
    // last visited is not a property of a page, it is a property of a crawler w.r.t. a page
    // page itself exists unaware that some crawlers visit it
