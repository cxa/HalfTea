# HalfTea

Inspired by [TEA (The Elm Architecture)](https://guide.elm-lang.org/architecture/) but for Xamarin stack, in an unobstrusive way.

## UI Component Basis

To simplify, an UI component can split into those three parts:

### State — the state of component

```fsharp
type State () =
  member val Count = 0 with get, set
```

### Update — a way to update state

To tell component how to update state, we will dispatch message `Msg`. Notice you should use the builtin `State.update` to update state but not assign directly.

```fsharp
type Msg =
  | Increment
  | Decrement

let update msg (state: State) =
  match msg with
  | Increment ->
    State.update <@ state.Count @> (state.Count + 1)
  | Decrement ->
    State.update <@ state.Count @> (state.Count - 1)
```

### View — a way to view state

Bind state to UI, and setup message dispatching for UI controls. And, for convenience, you should use `State.bind` for UI binding.

```fsharp
interface IView'<State, Msg> with
  member x.BindState state =
    State.bind <@ state.Count @> <| fun count ->
      x.countLabel.Text <- sprintf "state.Count: %d" count

  member x.Subscribe state dispatch =
    x.incrButton.TouchUpInside.Add <| fun _ -> dispatch Increment
    x.decrButton.TouchUpInside.Add <| fun _ -> dispatch Decrement
```

Before we can run the component, we should make `State` reactivable by implementating `State.I` interfaces. Fortunately, we can simply by subclassing `State.Base`:

```fsharp
type State () =
    inherit State.Base ()
    member val Count = 0 with get, set
```

To eleminate `State` type annotation (since it is a class not record), you can use `Component.make''` to setup the component:

```fsharp
let make () = Component.make'' {
  init = fun () ->
    State ()

  update = fun msg state ->
    match msg with
    | Increment ->
      State.update <@ state.Count @> (state.Count + 1)
    | Decrement ->
      State.update <@ state.Count @> (state.Count - 1)
}
```

Run when view ready (on iOS `viewDidLoad` or Android `OnCreate`).

```fsharp
CounterComponent.make () |> Component.runWithView'' viewInstance
```

Check the [SimpleCounter](samples/SimpleCounter) example for detail.

## But UI is never that simple

### Command — Ask to do things on own initiative

When returning a command `Cmd<'msg>` instead of `unit`, `HalfTea` will execute the command:

```fsharp
...
| Msg.RequestRandomImage ->
  State.update <@ state.RequstMessage @> "Requesting random image..."
  let uri = Uri "https://picsum.photos/200/100/?random"
  use client = new WebClient ()
  Cmd.ofAsync
    (fun () -> client.DownloadDataTaskAsync uri |> Async.AwaitTask)
    (Choice1Of2 >> Msg.Response)
    (Choice2Of2 >> Msg.Response)
...
```

We offer some utils to make a command to perform async task or function, check out the `Cmd` command.

### Subscribe — register what you are interested in

With command you _ask_ to do something, but in the real world, something is out of our control, just like a clock, all we can do is to listen its tick, this is where `subscribe` come in.

```fsharp
subscribe = fun _state dispatch ->
  let t = new Timers.Timer 1.
  t.Elapsed.Subscribe (fun arg -> dispatch <| Msg.Tick arg.SignalTime) |> ignore
  t.Start ()
```

Check the [Counter](samples/Counter) example for detail.

## FAQ

_Q_: Why mutable class but not immutable record for state?

_A_: Because we need `INotifyPropertyChanged` to inform UI to update. And for the OO-based UI framework (Xamarin.iOS, Xamarin.Droid and Xamarin.Forms), mutable class state is much more fitted.

## Usage

Drag `src/HalfTea.fs` to your project. To track version, you may use [paket](https://fsprojects.github.io/Paket/) or git submodules. It's that simple and we have no plan to maintain a NuGet package.
