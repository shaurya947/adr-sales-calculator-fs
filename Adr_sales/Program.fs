open HttpClient
open FSharp.Data
open FSharp.Data.JsonExtensions
open System
open System.Text.RegularExpressions
open System.Collections.Generic

let isStringFilename string = 
    Regex.IsMatch(string, "^85830718_[0-9]{4}_[A-Z]{2}[.]txt")

let isLineTotalAmount string = 
    Regex.IsMatch(string, "^Total_Amount")

//DEBUGGING FUNCTIONS
//
//let displayFilenamesOnly list =
//    list |> List.iter (fun x -> printfn "%s" x)
//
//let displayFiles list prefix =
//    list |> List.iter (fun x ->
//        printfn "\n\n%s\n\n" x
//        let result = createRequest Get (prefix + x) |> getResponseBody
//        printfn "%s" result)
//
//let displayDictDeep (dict: Dictionary<string, Dictionary<string, Dictionary<string, decimal>>>) = 
//    printfn "{"
//    for eyy in dict do
//        printfn "\t%s:" eyy.Key
//        printfn "\t{"
//
//        for emm in eyy.Value do
//            printfn "\t\t%s:" emm.Key
//            printfn "\t\t{"
//
//            for ecountry in emm.Value do
//                printfn "\t\t\t%s:\t%A" ecountry.Key ecountry.Value
//            
//            printfn "\t\t}"
//        
//        printfn "\t}"
//
//    printfn "}"
//
//DEBUGGING FUNCTIONS END

let getLastDayOfMonthAsString mm yyyy = 
    match mm with
    | mm when mm < 1 || mm > 12 -> "0"
    | 2 -> if yyyy % 4 = 0 then "29" else "28"
    | 1 | 3 | 5 | 7 | 8 | 10 | 12 -> "31"
    | _ -> "30"

let getCurrencyCode country = 
    match country with
    | "AU" | "CA" | "HK" | "NZ" | "SG" | "US" -> country + "D"
    | "EU" | "IN" | "ID" | "ZA" -> country + "R"
    | "DK" | "NO" | "SE" -> country + "K"
    | "CN" | "JP" | "TR" -> country + "Y"
    | "CH" -> "CHF"
    | "GB" -> "GBP"
    | "IL" -> "ILS"
    | "RU" -> "RUB"
    | "MX" -> "MXN"
    | "WW" -> "USD"
    | _ -> "unrealized"

let generateMainDictionary prefix (filesArray: string[]) ext (exceptionList: HashSet<string>)= 
    let dict = new Dictionary<string, Dictionary<string, Dictionary<string, decimal>>>()

    printfn "Reading files..."
    filesArray |> Array.iter (fun f ->
        let result = createRequest Get (prefix + f + ext) |> getResponseBody
        printfn "%s" (prefix + f + ext)

        //can process result more here if needed
        //but for now, just get "Total_Amount"

        let tAmount = Decimal.Parse ((result.Split('\n') |> Array.find isLineTotalAmount).Split('\t').[1])

        //insert entry into dictionary
        let splitArr = f.Split '_'
        let mm = splitArr.[1].[0..1]
        let yy = splitArr.[1].[2..3]
        let country = splitArr.[2]

        //check for year key
        if not <| dict.ContainsKey yy
            then dict.Add(yy, new Dictionary<string, Dictionary<string, decimal>>())
        else ()
        
        //check for month key
        if not <| dict.[yy].ContainsKey mm
            then dict.[yy].Add(mm, new Dictionary<string, decimal>())
        else ()

        //add country key -- this should be unique for every month every year

        //convert tAmount to USD using historic currency rates
        let currCode = getCurrencyCode country

        match currCode with
        | "unrealized" -> ignore <| exceptionList.Add (f + ext)
        | "USD" -> dict.[yy].[mm].Add(country, tAmount)
        | _ -> (
                let exRateQuery = "http://api.fixer.io/20" + yy + "-" + mm + "-" + (getLastDayOfMonthAsString (Int32.Parse(mm)) (2000 + Int32.Parse(yy))) + "/?base=" + currCode + "&symbols=USD"
                let exRate = JsonValue.Parse (createRequest Get exRateQuery |> getResponseBody)
                dict.[yy].[mm].Add(country, tAmount * exRate?rates?USD.AsDecimal())
               )

        )

    printfn "Done\n\n"
    dict


let displayReport (dict: Dictionary<string, Dictionary<string, Dictionary<string, decimal>>>) (exceptionList: HashSet<string>)= 
    printfn "Here is the report of total earnings over all products by year, by month\n"
    for eyy in dict do
        printfn "Year: %s" ("20" + eyy.Key)

        let mutable yearSum = 0M

        for emm in eyy.Value do
            let mutable monthSum = 0M

            printf "\tMonth: %s" emm.Key

            for ecountry in emm.Value do
                monthSum <- (monthSum + ecountry.Value)

            printf ", Earnings: %.2M\n" monthSum
            yearSum <- (yearSum + monthSum)

        printfn "\tTotal Year Earnings: %.2M" yearSum

    printfn "\n\nHere is a list of the files containing unrealized income:\n";
    for exFile in exceptionList do
        printfn "%s" exFile

[<EntryPoint>]
let main argv = 

    let result = createRequest Get "https://github.com/amirrajan/amirrajan.github.com/tree/master/adr-sales" 
                    |> getResponseBody

    let filesArray = result.Split('\n', '"') |> Array.filter isStringFilename |> Array.map (fun f -> f.Split('.').[0])
    let urlPrefix = "https://raw.githubusercontent.com/amirrajan/amirrajan.github.com/master/adr-sales/"

    let exceptionList = new HashSet<string>()
    let dict = generateMainDictionary urlPrefix filesArray ".txt" exceptionList

    displayReport dict exceptionList
    0 // return an integer exit code

