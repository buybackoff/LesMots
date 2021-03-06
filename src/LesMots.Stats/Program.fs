﻿// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System
open System.Collections.Generic
open System.Linq
open System.Text.RegularExpressions
open System.IO
open System.Diagnostics

open NReadability

open ServiceStack.DataAnnotations
open ServiceStack.OrmLite



open java.io
open java.util
open edu.stanford.nlp.ling
open edu.stanford.nlp.``process``

type java.util.Iterator with
    member public this.ToSeq() = 
        match this with
        | null -> null
        | _ -> 
            let res = 
                seq {
                    while this.hasNext() do
                        yield this.next()
                }
            res.AsEnumerable()


type java.util.Iterator with
    member public this.ToEnumerable<'U>() = 
        match this with
        | null -> null
        | _ -> 
            let res = 
                seq {
                    while this.hasNext() do
                        yield this.next() //:?> 'U
                }
            res.AsEnumerable().Cast<'U>()



let mysql = new OrmLiteConnectionFactory("Server=localhost;Database=arvi;Uid=test;Pwd=test;", MySqlDialect.Provider)
let sqlite = new OrmLiteConnectionFactory("../../../../NLP.sqlite", SqliteDialect.Provider)

type NewsArticle() = 
    
    [<AliasAttribute("news_id")>][<PrimaryKeyAttribute>][<AutoIncrementAttribute>] 
    member val Id = 0 with get, set
    [<AliasAttribute("time")>] 
    member val Time = Unchecked.defaultof<DateTime> with get, set
    [<AliasAttribute("link")>] 
    member val Link = Unchecked.defaultof<string> with get, set
    [<AliasAttribute("html_source")>] 
    member val HtmlSource = Unchecked.defaultof<string> with get, set
    member val Text = Unchecked.defaultof<string> with get, set

type TokenFrequency() = 
    [<PrimaryKeyAttribute>] 
    member val Token = "" with get, set
    member val Frequency = Unchecked.defaultof<double> with get, set

type CharSingle() = 
    [<PrimaryKeyAttribute>] 
    member val Single = "" with get, set
    member val Frequency = Unchecked.defaultof<double> with get, set

type CharTuple() = 
    [<PrimaryKeyAttribute>] 
    member val Tuple = "" with get, set
    member val Frequency = Unchecked.defaultof<double> with get, set

type CharTriple() = 
    [<PrimaryKeyAttribute>] 
    member val Triple = "" with get, set
    member val Frequency = Unchecked.defaultof<double> with get, set

type CharQuadruple() = 
    [<PrimaryKeyAttribute>] 
    member val Quadruple = "" with get, set
    member val Frequency = Unchecked.defaultof<double> with get, set


// run this once
// already done this with c.7k articles, enough for start
let moveNewsFromMysqToSqLite() = 
    use mdb = mysql.OpenDbConnection()
    use sdb = sqlite.OpenDbConnection()
    sdb.CreateTable<NewsArticle>(false)
    let count = mdb.Scalar<int>("SELECT count(html_source) FROM news WHERE html_source <> '' ")
    Console.WriteLine(count)
    //news_id,
    let articles = mdb.Select<NewsArticle>("SELECT  time, link, html_source FROM news WHERE html_source <> '' ")
    let resultCount = sdb.SaveAll(articles)
    Console.WriteLine(resultCount)
    ()

let plainText (htmlSource:string) = 
    let reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
    let regHead = new Regex(@"<head[^>]*>[\s\S]*?</head>")
    let text = reg.Replace(regHead.Replace(htmlSource, ""), "");
    text

let tokenizeBySentence(text : string) : List<List<string>> =
    let options = "americanize=true,normalizeSpace=false,
    normalizeFractions=true,normalizeParentheses=false,
    normalizeOtherBrackets=false,asciiQuotes=true,unicodeQuotes=true,
    escapeForwardSlashAsterisk=false,ptb3Dashes=true,
    untokenizable=allKeep,strictTreebank3=false,tokenizeNLs=false"
    let dp = new DocumentPreprocessor(new StringReader(text))
    let ptbtf = PTBTokenizer.PTBTokenizerFactory.newWordTokenizerFactory(options)

    dp.setTokenizerFactory(ptbtf)

    let res = dp.iterator().ToEnumerable<ArrayList>()
                |> Seq.map ( fun s ->  s.iterator().ToSeq().Select(fun x -> x.ToString()).ToList())
                |> fun p -> p.ToList()
    res


let concordance(text : string) : SortedList<string, List<int> * List<int>> =
  let dict = SortedList<string, List<int> * List<int>>()
  let sentences = tokenizeBySentence text
  sentences
  |> Seq.iteri (fun sentenseIdx sentence ->
      sentence
      |> Seq.iter (fun token ->
          let token = token.ToLowerInvariant()
          if dict.ContainsKey(token) then
            let freq, occur =  dict.[token]
            freq.[0] <- freq.[0] + 1
            occur.Add(sentenseIdx + 1)
          else
            let freq = List<int>()
            freq.Add(1)
            let occur = List<int>()
            occur.Add(sentenseIdx + 1)
            dict.[token] <- (freq, occur)
        )
    )
  dict

let tokensWithCount = Dictionary<string, int>()
//let charSingles = Dictionary<string, int>()
let charTuples = Dictionary<string, int>()
let charTriples = Dictionary<string, int>()
let charQuadruples = Dictionary<string, int>()

let countTokens (dic:Dictionary<string, int>) (scale:int) (tokens:string seq)  =
        tokens 
        |> Seq.iter (fun t -> 
                            lock dic (fun () ->
                                if dic.ContainsKey(t) then 
                                    dic.[t] <- dic.[t] + scale
                                else 
                                    dic.[t] <- scale
                            )
                         )

let extractContentFromHtmlSource() = 
    use db = sqlite.OpenDbConnection()
    let count = db.Count<NewsArticle>()
    Console.WriteLine(count)

    let transcoder = NReadabilityTranscoder()

    let articles = db.Select<NewsArticle>("news_id < 500").ToArray()
    articles
    |> Array.Parallel.iter
            (fun article -> 
                try
                    let input = TranscodingInput(article.HtmlSource)
                    input.Url <- article.Link
                    let result = transcoder.Transcode(input)
                    if result.ContentExtracted then
                        let text = plainText(result.ExtractedContent)
                        let sentenseTokens = tokenizeBySentence(text)
                        sentenseTokens |> Seq.iter (countTokens tokensWithCount 1)
                        Console.WriteLine(article.Id)
                    ()
                with _ -> ()
            )


    let sorted = tokensWithCount.Where(fun kvp -> kvp.Key.Length > 1).OrderByDescending(fun kvp -> kvp.Value)
    let totalTokens = ref (sorted.Sum(fun t -> t.Value))
    let sortedTokens = 
        tokensWithCount.OrderByDescending(fun kvp -> kvp.Value).Take(5000)
                    .Select(fun kvp -> 
                                let ct = TokenFrequency()
                                ct.Token <- kvp.Key
                                ct.Frequency <- (double kvp.Value) / (double !totalTokens)
                                ct)
    use db = sqlite.OpenDbConnection()
    db.CreateTable<TokenFrequency>(true)
    db.SaveAll(sortedTokens) |> ignore
    sorted


let countCharTuplesAndTriples (tokens:IEnumerable<KeyValuePair<string, int>>) =
    let processToken (token:string) (scale:int) =
        let charArray = token.ToArray()
//        if charArray.Length >= 1 then
//            charArray 
//            |> Seq.windowed 1
//            |> Seq.map (fun carr -> String(carr))
//            |> countTokens charSingles scale
        if charArray.Length >= 2 then
            charArray 
            |> Seq.windowed 2
            |> Seq.map (fun carr -> String(carr))
            |> countTokens charTuples scale
        if charArray.Length >= 3 then
            charArray 
            |> Seq.windowed 3
            |> Seq.map (fun carr -> String(carr))
            |> countTokens charTriples scale
        if charArray.Length >= 4 then
            charArray 
            |> Seq.windowed 4
            |> Seq.map (fun carr -> String(carr))
            |> countTokens charQuadruples scale

    tokens.ToArray()
    |> Array.Parallel.iter ( fun token -> processToken token.Key token.Value)

//    let totalSingles = ref (charSingles.Sum(fun t -> t.Value))
//    let sortedSingles = 
//        charSingles.OrderByDescending(fun kvp -> kvp.Value)
//            .Take(256).Select(fun kvp -> 
//                                let ct = CharSingle()
//                                ct.Single <- kvp.Key
//                                ct.Frequency <- (double kvp.Value) / (double !totalSingles)
//                                ct)

    let totalTuples = ref (charTuples.Sum(fun t -> t.Value))
    let sortedTuples = 
        charTuples.OrderByDescending(fun kvp -> kvp.Value)
            .Take(512).Select(fun kvp -> 
                                let ct = CharTuple()
                                ct.Tuple <- kvp.Key
                                ct.Frequency <- (double kvp.Value) / (double !totalTuples)
                                ct)
    
    let totalTriples = ref (charTriples.Sum(fun t -> t.Value))
    let sortedTriples = 
        charTriples.OrderByDescending(fun kvp -> kvp.Value)
            .Take(512).Select(fun kvp -> 
                                let ct = CharTriple()
                                ct.Triple <- kvp.Key
                                ct.Frequency <- (double kvp.Value) / (double !totalTuples)
                                ct)
    let totalQuadruples = ref (charQuadruples.Sum(fun t -> t.Value))
    let sortedQuadruples = 
        charQuadruples.OrderByDescending(fun kvp -> kvp.Value)
            .Take(512).Select(fun kvp -> 
                                let ct = CharQuadruple()
                                ct.Quadruple <- kvp.Key
                                ct.Frequency <- (double kvp.Value) / (double !totalTuples)
                                ct)
    use db = sqlite.OpenDbConnection()
    //db.CreateTable<CharSingle>(true)
    db.CreateTable<CharTuple>(true)
    db.CreateTable<CharTriple>(true)
    db.CreateTable<CharQuadruple>(true)
    //db.SaveAll(sortedSingles) |> ignore
    db.SaveAll(sortedTuples) |> ignore
    db.SaveAll(sortedTriples) |> ignore
    db.SaveAll(sortedQuadruples) |> ignore

    ()

// generate .fs file with dics
let generateHardCodedDicts() =
    use db = sqlite.OpenDbConnection()
    //let singles = db.Select<CharSingle>() |> Seq.mapi (fun i t -> KeyValuePair<int, string>(i,t.Single))
    let tuples = db.Select<CharTuple>() |> Seq.mapi (fun i t -> KeyValuePair<int, string>(i,t.Tuple))
    let triples = db.Select<CharTriple>() |> Seq.mapi (fun i t -> KeyValuePair<int, string>(i,t.Triple))
    let quads = db.Select<CharQuadruple>() |> Seq.mapi (fun i t -> KeyValuePair<int, string>(i,t.Quadruple))

    let path = "./dictionaries.fs"
    use sw = File.AppendText(path)
    sw.WriteLine("[<AutoOpen>] module Chars =")
    sw.WriteLine("open System")
    sw.WriteLine("open System.Collections.Generic")
    
    let visibleASCII = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\]^_`abcdefghijklmnopqrstuvwxyz{|}~"
    sw.WriteLine("let singles = Dictionary<string, int>()")
    visibleASCII.ToArray()
    |> Seq.iteri
        (fun i a -> 
            sw.WriteLine("singles.Add(@\"" + String(a, 1) + "\", " + i.ToString() + ")")
            )
//    for kvp in singles do
//        sw.WriteLine("singles.Add(\"" + kvp.Value + "\", " + kvp.Key.ToString() + ")")
    
    sw.WriteLine("let tuples = Dictionary<string, int>()")
    for kvp in tuples.Take(256) do
        sw.WriteLine("tuples.Add(@\"" + kvp.Value + "\", " + kvp.Key.ToString() + ")")
    
    sw.WriteLine("let triples = Dictionary<string, int>()")
    for kvp in triples.Take(256) do
        sw.WriteLine("triples.Add(@\"" + kvp.Value + "\", " + kvp.Key.ToString() + ")")
    
    sw.WriteLine("let quads = Dictionary<string, int>()")
    for kvp in quads.Take(256) do
        sw.WriteLine("quads.Add(@\"" + kvp.Value + "\", " + kvp.Key.ToString() + ")")
    ()



[<EntryPoint>]
let main argv = 
    let sw = new Stopwatch()
    sw.Start()

    // http://www.bloomberg.com/news/articles/2016-02-05/the-rich-are-already-using-robo-advisers-and-that-scares-banks
    let sampleText = "Millennials and small investors aren’t the only ones using robo-advisers, a group that includes pioneers Wealthfront Inc. and Betterment LLC and services provided by mutual-fund giants, said Kendra Thompson, an Accenture Plc managing director. At Charles Schwab Corp., about 15 percent of those in automated portfolios have at least $1 million at the company."
    
    let conc = concordance(sampleText)

    for item in conc do
      if item.Key.Any(fun ch -> Char.IsLetter(ch)) then
        printfn "%s - {%d:%s}" item.Key (fst item.Value).[0] (String.Join (",", (snd item.Value)))
    // one-off operation

    // moveNewsFromMysqToSqLite()

//    extractContentFromHtmlSource() 
//    |> countCharTuplesAndTriples
//    
//    generateHardCodedDicts() 

    sw.Stop()
    Console.WriteLine("Elapsed ms: " + sw.ElapsedMilliseconds.ToString())
    Console.WriteLine("Press Enter to exit...")
    Console.ReadLine() |> ignore
    0 // return an integer exit code
