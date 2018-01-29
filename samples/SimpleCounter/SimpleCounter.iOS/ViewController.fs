namespace SimpleCounter.iOS

open System

open Foundation
open UIKit

open HalfTea
open SimpleCounter.Core
open CounterComponent

[<Register ("ViewController")>]
type ViewController (handle:IntPtr) =
  inherit UIViewController (handle)

  [<Outlet>] member val countLabel: UILabel = null with get, set
  [<Outlet>] member val incrButton: UIButton = null with get, set
  [<Outlet>] member val decrButton: UIButton = null with get, set

  override x.ViewDidLoad () =
    base.ViewDidLoad ()
    CounterComponent.make () |> Component.runWithView'' x

  interface IView'<State, Msg> with
    member x.BindState state =
      State.bind <@ state.Count @> <| fun count ->
        x.countLabel.Text <- sprintf "state.Count: %d" count

    member x.Subscribe state dispatch =
      x.incrButton.TouchUpInside.Add <| fun _ -> dispatch Increment
      x.decrButton.TouchUpInside.Add <| fun _ -> dispatch Decrement
