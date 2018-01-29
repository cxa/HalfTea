namespace SimpleCounter.Core

module CounterComponent =
  open HalfTea

  type State () =
    inherit State.Base ()
    member val Count = 0 with get, set

  type Msg =
    | Increment
    | Decrement

  let make () = Component.make'' {
    init = fun () ->
      State()

    update = fun msg state ->
      match msg with
      | Increment ->
        State.update <@ state.Count @> (state.Count + 1)
      | Decrement ->
        State.update <@ state.Count @> (state.Count - 1)
  }
