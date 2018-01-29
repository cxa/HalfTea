namespace SimpleCounter.Droid

open System

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget

open HalfTea
open SimpleCounter.Core
open CounterComponent

type Resources = SimpleCounter.Droid.Resource

[<Activity (Label = "SimpleCounter", MainLauncher = true, Icon = "@mipmap/icon")>]
type MainActivity () =
  inherit Activity ()

  override x.OnCreate (bundle) =
    base.OnCreate (bundle)
    x.SetContentView (Resources.Layout.Main)
    CounterComponent.make () |> Component.runWithView'' x

  interface IView'<State, Msg> with
    member x.BindState state =
      State.bind <@ state.Count @> <| fun count ->
        let countTexView = x.FindViewById<TextView>(Resources.Id.countTextView)
        countTexView.Text <- sprintf "state.Count: %d" count

    member x.Subscribe state dispatch=
      let incrButton = x.FindViewById<Button>(Resources.Id.incrButton)
      let decrButton = x.FindViewById<Button>(Resources.Id.decrButton)
      incrButton.Click.Add <| fun _ -> dispatch Msg.Increment
      decrButton.Click.Add <| fun _ -> dispatch Msg.Decrement
