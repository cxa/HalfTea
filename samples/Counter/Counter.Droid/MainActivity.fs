namespace Counter.Droid

open System

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Graphics

open HalfTea
open Counter.Core
open Counter.Core.CounterComponent

type Resources = Counter.Droid.Resource

[<Activity (Label = "Counter", MainLauncher = true, Icon = "@mipmap/icon")>]
type MainActivity () =
  inherit Activity ()

  override x.OnCreate (bundle) =
    base.OnCreate (bundle)
    x.SetContentView (Resources.Layout.Main)
    CounterComponent.run x

  interface IView'<State.T, Msg.T> with
    member x.BindState state =
      State.bind <@ state.CurrentTime @> <| fun dtStr ->
        let textView = x.FindViewById<TextView>(Resources.Id.dtTextView)
        textView.Text <- dtStr
      State.bind <@ state.RandomImageBytes @> <| fun bytesOpt ->
        let imageView = x.FindViewById<ImageView>(Resources.Id.imageView)
        let bitmap =
          Option.fold (fun _ bytes ->
            BitmapFactory.DecodeByteArray (bytes, 0, Array.length bytes)
          ) null bytesOpt
        imageView.SetImageBitmap bitmap
      State.bind <@ state.RequstMessage @> <| fun msg ->
        let textView = x.FindViewById<TextView>(Resources.Id.requestMessageLabel)
        textView.Text <- msg
      State.bind <@ state.Count @> <| fun c ->
        let countTexView = x.FindViewById<TextView>(Resources.Id.countTextView)
        countTexView.Text <- sprintf "model.Count: %d" c

    member x.Subscribe state dispatch=
      let incrButton = x.FindViewById<Button>(Resources.Id.incrButton)
      let decrButton = x.FindViewById<Button>(Resources.Id.decrButton)
      let reqRmdButton = x.FindViewById<Button>(Resources.Id.requestRandomImgButton)
      incrButton.Click.Add <| fun _ -> dispatch Msg.Increment
      decrButton.Click.Add <| fun _ -> dispatch Msg.Decrement
      reqRmdButton.Click.Add <| fun _ -> dispatch Msg.RequestRandomImage
