// Learn more about F# at http://fsharp.org

open System
open BackgroundLogger.Core
open System.Threading
open System.Reactive.Linq
open System.Reactive.Linq

let slowAppender = {new IAppender with
                        member this.Log(messages: Collections.Generic.IList<string>): unit = 
                            Thread.Sleep(1000)
                            for m in messages do Console.WriteLine(m)
                            }

[<EntryPoint>]
let main argv =
    
    let logger = createBackgroundLogger [slowAppender] (TimeSpan.FromSeconds(1.0)) 5 30

    for i = 1 to 100 do
        logger.Log(sprintf "Test %i" i)

    Console.ReadKey()

    printfn "Hello World from F#!"
    0 // return an integer exit code
