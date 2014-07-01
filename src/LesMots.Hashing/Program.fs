// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open LesMots
open System
open System.Collections.Generic
open System.Linq
open System.Text.RegularExpressions
open System.IO
open System.Diagnostics

open ServiceStack.DataAnnotations
open ServiceStack.OrmLite

type TokenFrequency() = 
    [<PrimaryKeyAttribute>] 
    member val Token = "" with get, set
    member val Frequency = Unchecked.defaultof<double> with get, set


let sqlite = new OrmLiteConnectionFactory("../../../../NLP.sqlite", SqliteDialect.Provider)

let textCompression7() =
    use db = sqlite.OpenDbConnection()
    let tokens = db.Select<TokenFrequency>()
    let mutable success = 0
    for token in tokens do
        try
            let hashList = Hashing.hash7 token.Token
            if hashList.Count < 8 then success <- success + 1
        with _ -> ()
    let successRatio = (double success)/(double tokens.Count)
    Console.WriteLine("7 bits success ratio: " + successRatio.ToString())

let textCompression8() =
    use db = sqlite.OpenDbConnection()
    let tokens = db.Select<TokenFrequency>()
    let mutable success = 0
    for token in tokens do
        try
            let hashList = Hashing.hash8 token.Token
            if hashList.Count < 7 then success <- success + 1
        with _ -> ()
    let successRation = (double success)/(double tokens.Count)
    Console.WriteLine("8 bits success ratio: " + successRation.ToString())

[<EntryPoint>]
let main argv = 
    //let hashList = Hashing.hash8 "Antidisestablishmentarianism" announcement
    let hashList = Hashing.hash8 "announcement"
    textCompression7()
    textCompression8()
    Console.WriteLine("Press enter to exit...")
    Console.ReadLine() |> ignore
    printfn "%A" argv
    0 // return an integer exit code
