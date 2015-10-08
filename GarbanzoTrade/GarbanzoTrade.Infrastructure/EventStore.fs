module GarbanzoTrade.Infrastructure.EventStore
open System

type EventRecord =
  { EventID  : Guid
    Score    : Int64
    Stamp    : DateTime
    Headers  : Headers
    Payload  : Object }
and Headers = (String * Object) list
and EventStream =
  { StreamID : Guid
    Events   : EventRecord list }

type private EventStore (store) = 
    member __.Store : Map<Guid,EventRecord> = store

    member E.ReadStream streamID =  
      E.Store |> Map.filter (fun sID _ -> sID = streamID)
              |> Map.toList
              |> List.map snd
              |> List.sortBy (fun e -> e.Score)
    
    member E.ProbeStream streamID =
      E.Store |> Map.filter (fun sID _ -> sID = streamID)
              |> Map.toList
              |> List.map snd
              |> List.maxBy (fun e -> e.Score)

    member E.AppendStream streamID record =
      new EventStore (E.Store |> Map.add streamID record)

    member __.PrepRecord score headers payload =
      { EventID = Guid.NewGuid()
        Score   = score
        Stamp   = DateTime.UtcNow
        Headers = headers
        Payload = payload }

type private StoreAction = 
  | Append of streamID:Guid * score:Int64 * headers:Headers * payload:Object
  | Read   of streamID:Guid * AsyncReplyChannel<EventStream>
  | Probe  of streamID:Guid * AsyncReplyChannel<Int64>

let private eventStoreAgent = 
  MailboxProcessor<StoreAction>.Start (fun inbox ->
    let rec loop (store:EventStore) =
      async { let! message = inbox.Receive ()
              match message with
              | Append (sID,score,headers,payload) -> let record = store.PrepRecord score headers payload
                                                      let store  = store.AppendStream sID record
                                                      return! loop store
              | Read   (sID,replyStream)           -> { StreamID = sID
                                                        Events   = store.ReadStream sID } |> replyStream.Reply
                                                      return! loop store
              | Probe  (sID,replyScore)            -> let probe = store.ProbeStream sID
                                                      replyScore.Reply (probe.Score)
                                                      return! loop store }
    loop (new EventStore (Map.empty)))

// Event Store API ------> 
let readStream streamID = 
  eventStoreAgent.PostAndReply (fun replyChannel -> Read (streamID,replyChannel))

let appendStream streamID score headers payload = 
  eventStoreAgent.Post (Append (streamID, score, headers, payload))

let probeStream streamID =
  eventStoreAgent.PostAndReply (fun replyChannel -> Probe (streamID,replyChannel))     