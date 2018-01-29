namespace HalfTea

module State =
  open System.ComponentModel
  open System.Reflection
  open Microsoft.FSharp.Quotations
  open Microsoft.FSharp.Quotations.Patterns

  type I =
    inherit INotifyPropertyChanged
    abstract RaisePropertyChanged: propertyName:string -> unit

  type Base () =
    let event = Event<_, _> ()

    override x.ToString () =
      x.GetType().GetProperties()
      |> Array.map (fun (p: PropertyInfo) ->
        sprintf "%A = %A"
          p.Name
          <| if p.CanRead then p.GetValue(x, null) else "Not Readable" :> obj
        )
      |> String.concat "\n"
      |> sprintf "\n%A"

    interface I with
      [<CLIEvent>]
      member __.PropertyChanged =
        event.Publish

      member x.RaisePropertyChanged propertyName =
        event.Trigger (x, PropertyChangedEventArgs propertyName)

  let bind (getter: Expr<'a>) (binder: 'a -> unit) =
    match getter with
    | PropertyGet (Some (Value(target, _)), propInfo, []) ->
      let getPropValue () = propInfo.GetValue target :?> 'a
      (target :?> INotifyPropertyChanged).PropertyChanged
      |> Observable.filter (fun args -> args.PropertyName = propInfo.Name)
      |> Observable.add (fun _ ->
        binder <| getPropValue ()
      )
      binder <| getPropValue ()
    | _ ->
      failwith "Expression must be a property getter"

  let update (getter: Expr<'a>) (value: 'a) =
    match getter with
    | PropertyGet (Some (Value(target, _)), propInfo, []) ->
      propInfo.SetValue(target, value)
      (target :?> I).RaisePropertyChanged propInfo.Name
    | _ ->
      failwith "Expression must be a property getter"

type Dispatch<'msg> = 'msg -> unit

type IView<'props, 'state, 'msg when 'state :> State.I> =
  abstract BindState: 'props -> 'state -> unit
  abstract Subscribe: 'props -> 'state -> Dispatch<'msg> -> unit

type IView'<'state, 'msg when 'state :> State.I> =
  abstract BindState: 'state -> unit
  abstract Subscribe: 'state -> Dispatch<'msg> -> unit

// Some code is copied and modified from
// https://github.com/fable-elmish/elmish

type Sub<'msg> = Dispatch<'msg> -> unit

type Cmd<'msg> = Sub<'msg> list

module Cmd =
  let none
    : Cmd<'msg> =
    []

  let ofMsg (msg: 'msg)
    : Cmd<'msg> =
    [fun dispatch -> dispatch msg]

  let ofSub (sub: Sub<'msg>)
    : Cmd<'msg> =
    [sub]

  let map (f: 'a -> 'msg) (cmd: Cmd<'a>)
    : Cmd<'msg> =
    cmd |> List.map (fun g -> (fun d -> f >> d) >> g)

  let batch cmds
    : Cmd<'msg> =
    List.concat cmds

  let private dispatchAsync (task: unit -> Async<_>)
                            (ofSuccess: _ -> 'msg)
                            (ofError: _ -> 'msg)
                             dispatch =
    async {
      let! r = task () |> Async.Catch
      let msg =
        match r with
        | Choice1Of2 x -> ofSuccess x
        | Choice2Of2 x -> ofError x
      dispatch msg
    }

  let ofAsync task ofSuccess ofError
    : Cmd<'msg> =
    let bind = dispatchAsync task ofSuccess ofError
    [bind >> Async.StartImmediate]

  let ofCancellableAsync task ofSuccess ofError cancellationToken
    : Cmd<'msg> =
    let bind = dispatchAsync task ofSuccess ofError
    [ fun dispatch ->
      Async.StartImmediate (bind dispatch, cancellationToken)
    ]

  let private dispatchAsync' (task: unit -> Async<_>)
                             (ofSuccess: _ -> 'msg)
                              cancellationToken
                              dispatch =
    async {
      let r =
        match cancellationToken with
        | Some token -> Async.RunSynchronously (task (), cancellationToken=token)
        | None -> Async.RunSynchronously <| task ()
      dispatch <| ofSuccess r
    }

  let ofAsync' task ofSuccess
    : Cmd<'msg> =
    let bind = dispatchAsync' task ofSuccess None
    [bind >> Async.StartImmediate]

  let ofCancellableAsync' task ofSuccess cancellationToken
    : Cmd<'msg> =
    let bind = dispatchAsync' task ofSuccess <| Some cancellationToken
    [ fun dispatch ->
      Async.StartImmediate (bind dispatch, cancellationToken)
    ]

  let ofFunc (func: unit -> _)
             (ofSuccess: _ -> 'msg)
             (ofError: _ -> 'msg)
              dispatch
    : Cmd<'msg> =
    let bind =
      try ofSuccess >> dispatch <| func ()
      with e -> ofError >> dispatch <| e
    [bind]

  let ofFunc' (func: unit -> _)
                  (ofSuccess: _ -> 'msg)
                  dispatch
    : Cmd<'msg> =
    [ofSuccess >> dispatch <| func ()]

module Component =
  type T<'props, 'state, 'msg> =
    { init: 'props -> 'state * Cmd<'msg>
    ; update: 'msg -> 'props -> 'state -> Cmd<'msg>
    ; subscribe: 'props -> 'state -> Dispatch<'msg> -> unit
    }

  type T'<'state, 'msg> =
    { init: unit -> 'state * Cmd<'msg>
    ; update: 'msg -> 'state -> Cmd<'msg>
    ; subscribe: 'state -> Dispatch<'msg> -> unit
    }

  type T''<'state, 'msg> =
    { init: unit -> 'state
    ; update: 'msg -> 'state -> unit
    }

  let make (t: T<'props, 'state, 'msg>) =
    t

  let make' (t': T'<'state, 'msg>) =
    let { T'.init = init
        ; update = update
        ; subscribe = subscribe
        } = t'
    { T.init = init
    ; update = fun msg () state -> update msg state
    ; subscribe = fun () state dispatch -> subscribe state dispatch
    }

  let make'' (t'': T''<'state, 'msg>) =
    let { T''.init = init
        ; update = update
        } = t''
    { T.init = fun () -> init (), Cmd.none
    ; update = fun msg () state -> update msg state; Cmd.none
    ; subscribe = fun () _ _ -> ()
    }

  let withConsoleTrace ``component`` =
    let { T.init=init
        ; update=update
        } = ``component``
    let traceInit props =
      let state, cmd = init props
      printfn "initial state: %A" state
      state, cmd
    let traceUpdate msg props state =
      let cmd = update msg props state
      printfn "msg: %A, updated state: %A" msg state
      cmd
    { ``component`` with init = traceInit; update = traceUpdate }

  let private run props (``component``: T<'props, 'state, 'msg>) setupView =
    let { T.init=init
        ; update=update
        ; subscribe=subscribe
        } = ``component``
    let cur = System.Threading.SynchronizationContext.Current
    let state, initialCmd = init props
    let mailbox = MailboxProcessor.Start <| fun inbox ->
      let rec loop () = async {
        let! msg = inbox.Receive ()
        do
          cur.Post ((fun _ ->
            let cmd = update msg props state
            cmd |> List.iter (fun sub -> sub inbox.Post)
          ), null)
        return! loop ()
      }
      loop ()
    setupView props state mailbox.Post
    subscribe props state mailbox.Post
    initialCmd
    |> List.iter (fun sub -> sub mailbox.Post)

  let runWithView props (view: IView<'props, 'state, 'msg>)
                        (``component``: T<'props, 'state, 'msg>) =
    run props ``component`` <| fun props state dispatch ->
      view.BindState props state
      view.Subscribe props state dispatch

  let runWithView' props (view: IView'<'state, 'msg>)
                         (``component``: T<'props, 'state, 'msg>) =
    run props ``component`` <| fun _ state dispatch ->
      view.BindState state
      view.Subscribe state dispatch

  let runWithView'' (view: IView'<'state, 'msg>)
                    (``component``: T<unit, 'state, 'msg>) =
    run () ``component`` <| fun _ state dispatch ->
      view.BindState state
      view.Subscribe state dispatch
