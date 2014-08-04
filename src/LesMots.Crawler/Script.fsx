#r "System.Xml.Linq"
#r "../../packages/FsUnit.1.3.0.1/Lib/Net40/FsUnit.NUnit.dll"
#r "../../packages/Ractor.0.2.0/Lib/Net40/Ractor.dll"

#load "Crawler.fs"
open LesMots
open System
open System.Linq
open System.IO
open System.Xml.Linq
open System.Net
open System.Web
open FsUnit

let ok, content, location = 
    Crawler.tryFetch("file:///" + __SOURCE_DIRECTORY__ + "/data/sitemap_news.xml")
    |> Async.RunSynchronously 

let xn s = XName.Get(s)
let xml = XDocument.Parse(content)

let inline (+) (ns:XNamespace) (localName:string) = ns.GetName(localName)

let ns = XNamespace.Get("http://www.google.com/schemas/sitemap-news/0.9")
let newsName = ns + "news"

let news = xml.Elements(newsName)

news.Count()

type Content =
| Robots of string // contents of robots.txt
| SitemapIndex of XDocument
| Sitemap of XDocument
| Page of string // html of a page
| Image of byte[]


// Host is IDistributedDataObject



// fetch should return content from url - a single task
// parse(content)


// parse robots.txt: 

xml.Root.Name.LocalName.ToLowerInvariant() = "sitemapindex"



let parseSitemapIndex (smi:XDocument) =
    
    failwith "not implemented"