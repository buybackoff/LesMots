namespace LesMots

open System
open System.IO
open System.IO.Compression
open System.Net
open System.Web

open ServiceStack
open ServiceStack.Text
open Ractor


type Url = string

type PageSeenEvent(pageUrl:Url,referrerUrl:Url,moment:DateTime) = 
    inherit BaseDistributedDataObject()
    member this.PageUrl with get() = pageUrl
    member this.ReferrerUrl with get() = referrerUrl
    member this.Moment with get() = moment
    
type PageVisitedEvent(pageUrl:Url,moment:DateTime) =
    inherit BaseDistributedDataObject()
    member this.PageUrl with get() = pageUrl
    member this.Moment with get() = moment


/// <summary>
/// Hashtable
/// </summary>
type PageLink =
    {/// <summary>
    /// A link to a page. Could be shortened, redirected, etc.
    /// </summary>
    Url:string;
    /// <summary>
    /// Actual resolved location of the link
    /// </summary>
    Location:string }


type Page (location:string) =
    member this.Location with get() = location
    member this.Uri with get () = System.Uri(this.Location)
    member this.Host with get() = this.Uri.Host
//    member this.MD5 with get() = 
//    new() = Page("")