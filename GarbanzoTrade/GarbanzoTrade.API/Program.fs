open Owin
open System
open System.Web.Http
open Microsoft.Owin.Hosting

type StartUp () = 
  member this.Configuration (app : IAppBuilder) = 
    let config = new HttpConfiguration ()
    let route  = config.Routes.MapHttpRoute ("Default"
                                            ,"api/{controller}/{id}")
    route.Defaults.Add("id", RouteParameter.Optional)
    app.UseWebApi (config) |> ignore


[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    let baseAddress = "http://localhost:8000"
    let server      = WebApp.Start<StartUp> (baseAddress)
    // Wrap in windows service..
    Console.WriteLine ("Server is now running.")
    Console.ReadLine () |> ignore
    server.Dispose ()
    0 // return an integer exit code