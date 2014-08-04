namespace LesMots

open System
open System.Text
open System.Collections.Generic
open System.Linq

// TODO instead of trying to find "the best" by iterating over all possible combinations,
// try the first

// TODO remove string srom int * int * string, that was for debugging only

[<AutoOpenAttribute>]
module Hashing = 
    let hash7 (word : string) : List<int * int * string> =        
        let rec hash' (word : string) (subLength : int) : List<int * int * string> = 
            if word.Length = 0 then List<int * int * string>() // empty list
            else if word.Length >= subLength then 
                if subLength > 1 then 
                    let subStarts = List<int * int * string>() // position and Sub #
                    let encodedVersions = List<List<int * int * string>>()
                    // determine all possible starting positions
                    for i in 0..word.Length - subLength do // moving beginning of substring
                        let sub = word.Substring(i, subLength)
                        let ok, subCode = 
                            match subLength with
                            | 4 -> Chars7.Quads.TryGetValue(sub)
                            | 3 -> Chars7.Triples.TryGetValue(sub)
                            | 2 -> Chars7.Tuples.TryGetValue(sub)
                            | _ -> failwith "wrong substring length"
                        if ok then subStarts.Add((i, subCode, sub))
                    let hasSubs = subStarts.Count > 0
                    if hasSubs then 
                        // try each and compare the resulting length
                        for pos, subCode, sub in subStarts do
                            let encodingVersion = 
                                let l = List<int * int * string>()
                                if pos > 0 then l.AddRange(hash' (word.Substring(0, pos)) subLength)
                                l.Add((subLength - 1, subCode, sub))
                                if pos + subLength < word.Length  then
                                    l.AddRange(hash' (word.Substring(pos + subLength, word.Length - pos - subLength)) subLength)
                                l
                            encodedVersions.Add(encodingVersion)
                        let shortestEncoding = encodedVersions |> Seq.minBy (fun l -> l.Count)
                        shortestEncoding
                    else hash' word (subLength - 1)
                else if subLength = 0 then failwith "never reach this"
                else // subLength = 1
                    let l = List<int * int * string>()
                    let charArray = word.ToCharArray()
                    for c in charArray do
                        let sub = String(c, 1)
                        let ok, subCode = Chars7.Singles.TryGetValue(sub)
                        if not ok then 
#if DEBUG
                            Console.WriteLine("Non-ascii: " + sub)
#endif
                            raise (ArgumentOutOfRangeException("Non-Ascii char in input"))
                        l.Add((0, subCode, sub))
                    l
            else hash' word (subLength - 1)
        // bits
        hash' word 4


    let hash8 (word : string) : List<int * int * string> =        
        let rec hash' (word : string) (subLength : int) : List<int * int * string> = 
            if word.Length = 0 then List<int * int * string>() // empty list
            else if word.Length >= subLength then 
                if subLength > 1 then 
                    let subStarts = List<int * int * string>() // position and Sub #
                    let encodedVersions = List<List<int * int * string>>()
                    // determine all possible starting positions
                    for i in 0..word.Length - subLength do // moving beginning of substring
                        let sub = word.Substring(i, subLength)
                        let ok, subCode = 
                            match subLength with
                            | 4 -> Chars8.Quads.TryGetValue(sub)
                            | 3 -> Chars8.Triples.TryGetValue(sub)
                            | 2 -> Chars8.Tuples.TryGetValue(sub)
                            | _ -> failwith "wrong substring length"
                        if ok then subStarts.Add((i, subCode, sub))
                    let hasSubs = subStarts.Count > 0
                    if hasSubs then 
                        // try each and compare the resulting length
                        for pos, subCode, sub in subStarts do
                            let encodingVersion = 
                                let l = List<int * int * string>()
                                if pos > 0 then l.AddRange(hash' (word.Substring(0, pos)) subLength)
                                l.Add((subLength - 1, subCode, sub))
                                if pos + subLength < word.Length  then
                                    l.AddRange(hash' (word.Substring(pos + subLength, word.Length - pos - subLength)) subLength)
                                l
                            encodedVersions.Add(encodingVersion)
                        let shortestEncoding = encodedVersions |> Seq.minBy (fun l -> l.Count)
                        shortestEncoding
                    else hash' word (subLength - 1)
                else if subLength = 0 then failwith "never reach this"
                else // subLength = 1
                    let l = List<int * int * string>()
                    let charArray = word.ToCharArray()
                    for c in charArray do
                        let sub = String(c, 1)
                        let ok, subCode = Chars8.Singles.TryGetValue(sub)
                        if not ok then
#if DEBUG
                            Console.WriteLine("Non-ascii: " + sub)
#endif
                            raise (ArgumentOutOfRangeException("Non-Ascii char in input"))
                        l.Add((0, subCode, sub))
                    l
            else hash' word (subLength - 1)
        // bits
        hash' word 4