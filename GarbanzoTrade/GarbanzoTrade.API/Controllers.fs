module Controllers
open System.Web.Http

type MortgageBackedSecurityController () =
  inherit ApiController ()
  member this.Get () = "hello MBS" 
