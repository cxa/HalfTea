namespace Counter.Core

module CounterComponent =
  open System
  open System.Net
  open HalfTea

  module State =
    type T () =
      inherit State.Base ()

      member val CurrentTime = "" with get, set
      member val Count = 0 with get, set
      member val RequstMessage = "" with get, set
      member val RandomImageBytes: byte[] option = None with get, set

  module Msg =
    type T =
    | Tick of DateTime
    | Increment
    | Decrement
    | RequestRandomImage
    | Response of Choice<byte[], exn>

  let make () = Component.make' {
    init = fun () ->
      State.T (), Cmd.none

  ; update = fun msg state ->
      match msg with
      | Msg.Tick dt ->
        let dtStr = dt.ToShortDateString () + " " + dt.ToLongTimeString ()
        State.update <@ state.CurrentTime @> dtStr
        Cmd.none
      | Msg.Increment ->
        State.update <@ state.Count @> (state.Count + 1)
        Cmd.none
      | Msg.Decrement ->
        State.update <@ state.Count @> (state.Count - 1)
        Cmd.none
      | Msg.RequestRandomImage ->
        State.update <@ state.RequstMessage @> "Requesting random image..."
        let uri = Uri "https://picsum.photos/200/100/?random"
        use client = new WebClient ()
        Cmd.ofAsync
          (fun () -> client.DownloadDataTaskAsync uri |> Async.AwaitTask)
          (Choice1Of2 >> Msg.Response)
          (Choice2Of2 >> Msg.Response)
      | Msg.Response result ->
        match result with
        | Choice1Of2 bytes ->
          State.update <@ state.RequstMessage @> "Image downloaded successfully"
          State.update <@ state.RandomImageBytes @> <| Some bytes
        | Choice2Of2 exn ->
          State.update <@ state.RequstMessage @> <| sprintf "Failed to download image: %A" exn.Message
        Cmd.none

  ; subscribe = fun _state dispatch ->
      let t = new Timers.Timer 1.
      t.Elapsed.Subscribe (fun arg -> dispatch <| Msg.Tick arg.SignalTime) |> ignore
      t.Start ()
  }

  let run view =
    make ()
    |> Component.runWithView'' view
