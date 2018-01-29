namespace Counter.iOS

open System
open Foundation
open UIKit

open HalfTea
open Counter.Core
open Counter.Core.CounterComponent

[<Register ("ViewController")>]
type ViewController (handle:IntPtr) =
  inherit UIViewController (handle)

  [<Outlet>] member val dtLabel: UILabel = null with get, set
  [<Outlet>] member val requestMessageLabel: UILabel = null with get, set
  [<Outlet>] member val imageView: UIImageView = null with get, set
  [<Outlet>] member val countLabel: UILabel = null with get, set
  [<Outlet>] member val incrButton: UIButton = null with get, set
  [<Outlet>] member val decrButton: UIButton = null with get, set
  [<Outlet>] member val randomImgButton: UIButton = null with get, set

  override x.ViewDidLoad () =
    base.ViewDidLoad ()
    CounterComponent.run x

  interface IView'<State.T, Msg.T> with
    member x.BindState state =
      State.bind <@ state.CurrentTime @> <| fun dt ->
        x.dtLabel.Text <- dt
      State.bind <@ state.RandomImageBytes @> <| fun bytes ->
        x.imageView.Image <-
          Option.fold (fun _ bs ->
            let data = NSData.FromArray bs
            UIImage.LoadFromData(data, UIScreen.MainScreen.Scale)
          ) null bytes
      State.bind <@ state.RequstMessage @> <| fun msg ->
        x.requestMessageLabel.Text <- msg
      State.bind <@ state.Count @> <| fun c ->
        x.countLabel.Text <- sprintf "model.Count: %d" c

    member x.Subscribe state dispatch =
      x.incrButton.TouchUpInside.Add <| fun _ -> dispatch Msg.Increment
      x.decrButton.TouchUpInside.Add <| fun _ -> dispatch Msg.Decrement
      x.randomImgButton.TouchUpInside.Add <| fun _ -> dispatch Msg.RequestRandomImage
