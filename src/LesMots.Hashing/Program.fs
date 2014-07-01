// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open LesMots

[<EntryPoint>]
let main argv = 
    let hashList = Hashing.hash "Antidisestablishmentarianism"
    printfn "%A" argv
    0 // return an integer exit code
