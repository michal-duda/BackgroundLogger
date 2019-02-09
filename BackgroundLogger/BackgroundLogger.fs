namespace BackgroundLogger

open System

module Core = 
    open System.Reactive.Subjects
    open System.Reactive.Linq
    open System.Collections.Generic
    open System.Reactive.Concurrency
    open System.Runtime.CompilerServices

    type ILogger =
        inherit IDisposable
        abstract member Log : string -> Unit
    
    type IAppender =
        abstract member Log : IList<string> -> Unit
    
    type WrappedEvent<'T> =  ('T -> Unit) -> Unit

    [<Extension>]
    type Extensions() =
        
        [<Extension>]
        static member QueueLimit<'T> (observable: IObservable<IList<'T>>, limit: int) : IObservable<WrappedEvent<IList<'T>>> = 
            let mutable counter = 0

            let decreaseAndApply processingFunc (value : IList<'T>) = 
                processingFunc(value)
                counter <- counter - value.Count

            observable.SelectMany(fun t ->
                if counter >= limit then
                    Observable.Empty<WrappedEvent<IList<'T>>>()
                else
                    counter <- counter + t.Count
                    let ret = fun processingFunc -> decreaseAndApply processingFunc t
                    Observable.Return(ret)
            )


    let createBackgroundLogger (appenders : list<IAppender>) (bufferTimeout : TimeSpan) (bufferSize : int) (maxQueueLength : int) = 
        let subject = new Subject<string>()
        let eventLoopScheduler = new EventLoopScheduler()

        let initList m =
            let l = new List<string>()
            l.Add(m)
            l

        let subscription = subject.AsObservable()
                            .Buffer(bufferTimeout, bufferSize)
                            .Where(fun b -> b.Count > 0)
                            .QueueLimit(maxQueueLength)
                            .ObserveOn(eventLoopScheduler)
                            .Subscribe(fun message -> message (fun m -> appenders |> List.iter (fun a -> a.Log(m))))
        
        { 
            new ILogger with
                member this.Log(message: string): unit = subject.OnNext(message)
                member this.Dispose() : unit = subscription.Dispose()
        }

    