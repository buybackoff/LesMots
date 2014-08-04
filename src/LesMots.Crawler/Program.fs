// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open LesMots
open System
open System.Collections.Generic
open System.Linq
open System.IO
open System.Xml.Linq
open System.Net
open System.Web
open Ractor


[<EntryPoint>]
let main argv = 
    

    let robots = Crawler.tryGetRobots("http://wsj.com") |> Async.RunSynchronously

    let a = 1

//    let ok, location, content = 
//        Crawler.tryFetchUrl("file:///" + __SOURCE_DIRECTORY__ + "/data/sitemap_news.xml")
//        |> Async.RunSynchronously 
//
//    let xn s = XName.Get(s)
//    let xml = XDocument.Parse(content)
//
//    let inline (+) (ns:XNamespace) (localName:string) = ns.GetName(localName)
//
//    let ns = XNamespace.Get("http://www.google.com/schemas/sitemap-news/0.9")
//    let newsName = xn ("news") //ns +
//
//    let news = xml.Descendants().Where(fun e -> e.Name.LocalName = "news")
//
//    news.Count()
    0 // return an integer exit code
