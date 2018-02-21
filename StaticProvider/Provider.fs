namespace ProviderImplementation

open ProviderImplementation.ProvidedTypes
open FSharp.Core.CompilerServices

open System
open System.IO
open System.Reflection

open Microsoft.FSharp.Compiler.Ast
open Microsoft.FSharp.Compiler.SourceCodeServices

/// <summary>
///   Defines a type provider that executes top-level functions marked with
///   <see cref="StaticAttribute" />.
/// </summary>
[<TypeProvider>]
type StaticProvider (config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config, assemblyReplacementMap=[("StaticProvider.DesignTime", "StaticProvider")])

    let ns = "StaticProvider.Provided"
    let assembly = Assembly.GetExecutingAssembly()

    do
        // Load FSharp.Compiler.Services using the provided references
        let isFcs (x: string) = x.Contains("FSharp.Compiler.Service")
        let fcs = Array.find isFcs config.ReferencedAssemblies

        Assembly.LoadFrom fcs |> ignore

    let instantiate (typeName: string) (args: obj[]) =
        let name = args.[0] :?> string
        let file = args.[1] :?> string
        let code = File.ReadAllText(file)

        let checker = FSharpChecker.Create()
        let options = { FSharpParsingOptions.Default with SourceFiles = [| file |] }

        let parsed = checker.ParseFile(file, code, options) |> Async.RunSynchronously

        assert(not parsed.ParseHadErrors) // Since we were invoked, it shouldn't have any error

        let tree = parsed.ParseTree.Value
        
        match tree with
        | ParsedInput.ImplFile(implFile) ->
            let (ParsedImplFileInput(_, _, _, _, _, modules, _)) = implFile

            let (_, _, body) = Seq.exactlyOne (findFunctionInModules name modules)

            let expr = fnToString body
            let res = compute expr

            ProvidedTypeDefinition(assembly, ns, typeName, Some typeof<obj>)

        | _ -> failwith ""


    let createTypes () =

        let staticType =
            let ty = ProvidedTypeDefinition(assembly, ns, "Static", Some typeof<obj>)
            let parameters = [ ProvidedStaticParameter("TypeName", typeof<string>) 
                             ; ProvidedStaticParameter("FileName", typeof<string>) ]

            ty.AddXmlDoc("Executes the methods in the given type.")
            ty.DefineStaticParameters(parameters, instantiate)
           
            ty

        let attrType =
            let ty = ProvidedTypeDefinition(assembly, "", "StaticAttribute", Some typeof<Attribute>)
            let ctor = ProvidedConstructor([], invokeCode = fun _ -> <@@ () @@>)

            ty.AddMember(ctor)
            ty.AddXmlDoc("Indicates that this method will be invoked during runtime.")
            ty.AddCustomAttribute(
                let ctor = typeof<AttributeUsageAttribute>.GetConstructors().[0]
                let arg = CustomAttributeTypedArgument(AttributeTargets.Method)

                { new CustomAttributeData() with
                    member __.Constructor          = ctor
                    member __.ConstructorArguments = [| arg |] :> _
                    member __.NamedArguments       = [|     |] :> _ }
            )

            ty

        [staticType]

    do
        this.AddNamespace(ns, createTypes())


[<assembly: TypeProviderAssembly>]
do ()
