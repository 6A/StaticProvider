[<AutoOpen>]
module internal Helpers

open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.Interactive.Shell
open System.IO
open System.Text

let log s =
    File.AppendAllText("D:/log.txt", s + "\r\n")

/// Finds the function matching the given name in the given bindings.
let findFunction fname bindings = seq {
    for b in bindings do
        match b with
        | Binding(_, NormalBinding, _, _, _, _, data, pat, retInfo, body, _, _) ->
            match pat with
            | SynPat.LongIdent(id, _, _, _, _, _) ->
                let (LongIdentWithDots(id, _)) = id
                
                match id with
                | []  -> ()
                | [x] when x.idText = fname -> yield (pat, retInfo, body)
                |  _  -> () 
            | _ -> ()

        | _ -> ()
}

/// Finds the function matching the given name in the given modules.
let findFunctionInModules fname modules = seq {
    for m in modules do
        let (SynModuleOrNamespace(_, _, _, decls, _, _, _, _)) = m

        for decl in decls do
            match decl with
            | SynModuleDecl.Let(_, bindings, _) ->
                yield! findFunction fname bindings
            | _ -> ()
}

let fnToString fn = fn.ToString()

let compute func =
    use inStream = new StringReader("")
    use outStream = new StreamWriter(new MemoryStream(), Encoding.Default, 4096, false)
    use errStream = new StreamWriter(new MemoryStream(), Encoding.Default, 4096, false)

    let config = FsiEvaluationSession.GetDefaultConfiguration()
    let session = FsiEvaluationSession.Create(config, [| "fsi" |], inStream, outStream, errStream)

    let exn, errors = session.EvalInteractionNonThrowing(func)

    sprintf "Errors: %O" errors |> log

    match exn with
    | Choice1Of2 () -> 0
    | Choice2Of2 _  -> 0

