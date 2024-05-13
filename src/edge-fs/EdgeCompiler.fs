namespace global
open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Text.RegularExpressions
open System.Threading.Tasks
open System.Diagnostics

type EdgeCompiler() =
    
    let referencesRegex = new Regex(@"\/\/\#r\s+""[^""]+""\s*", RegexOptions.Multiline)
    let referenceRegex = new Regex(@"\/\/\#r\s+""([^""]+)""\s*")
    let debuggingEnabled = not <| String.IsNullOrEmpty(Environment.GetEnvironmentVariable("EDGE_FS_DEBUG"))
    let tools = Environment.GetEnvironmentVariable("EDGE_FS_TOOLS")
   
    let writeSourceToDisk source =
        let path = Path.GetTempPath()
        let mutable fileName = Path.GetRandomFileName() 
        let fileNameEx = Path.ChangeExtension(fileName, ".fs")
        let fullName = Path.Combine(path, fileNameEx)
        File.WriteAllText(fullName, source)
        fullName
    
    let tryCompileCmd(source, references) =
        let fileName = writeSourceToDisk source
       
        let outputAssemblyName = Path.ChangeExtension(fileName, "dll")
        
        let processInfo = ProcessStartInfo($"{tools}\\fsc.exe", $"-a {fileName} -o {outputAssemblyName} --target:library")
        processInfo.WorkingDirectory <- tools
        processInfo.CreateNoWindow <- true;
        processInfo.UseShellExecute <- false;
        // *** Redirect output ***
        processInfo.RedirectStandardError <- true;
        processInfo.RedirectStandardOutput <- true;
        let proc = Process.Start(processInfo)
        proc.WaitForExit()
        let error = proc.StandardError.ReadToEnd();
        let exitCode = proc.ExitCode
        
        let assembly = 
            if exitCode <> 0 then None else
                try Some(Assembly.UnsafeLoadFrom(outputAssemblyName))
                with
                | _ -> (None)

        try
            File.Delete fileName
            File.Delete outputAssemblyName
        with
        | _ -> ()
            
        exitCode, error, assembly
       
    let getReferences (parameters:IDictionary<string, Object>) source=
    // add assembly references provided explicitly through parameters
        let passed = match parameters.TryGetValue("references")  with
                     | true, v -> seq {for item in unbox v do yield unbox item}
                     | _ -> Seq.empty

        // add assembly references provided in code as //#r "assemblyname" comments
        let enc = seq{ for m in referencesRegex.Matches(source) do
                           let referenceMatch = referenceRegex.Match(m.Value)
                           if referenceMatch.Success then
                               yield referenceMatch.Groups.[1].Value }
        seq{ yield! passed
             yield! enc}

    let getLineDirective (parameters:IDictionary<string, Object>) fileName= 
        if (debuggingEnabled) then
            let file = match parameters.TryGetValue("jsFileName") with
                       | true, (:? String as jsFileName) -> jsFileName
                       | _ -> fileName
                       
            let lineNumber = match parameters.TryGetValue("jsLineNumber") with
                             | true, (:? int as number) -> number
                             | _ -> 0
            if String.IsNullOrEmpty(file) then String.Empty
            else String.Format("#line {0} \"{1}\"\n", lineNumber, fileName)
        else String.Empty
            
    let tryGetAssembly lineDirective source references islambda =
        // try to compile source code as a library
        if islambda then     
            let lsource = "namespace global\n"
                          + "open System\n"
                          + "open System.Runtime\n"
                          + "type Startup() =\n"
                          + "    member x.Invoke(input: obj) =\n"
                          + lineDirective
                          + "        async {let! result = input |> (" + source + ")\n"
                          + "               return result :> obj } |> Async.StartAsTask"
    
            
            let result, errors, assembly = tryCompileCmd(lsource, references)

            if result = 0 then assembly else
                invalidOp <| "Unable to compile F# code.\n----> Errors when compiling as a CLR async lambda expression:\n" + errors
                         
        else let result, errors, assembly = tryCompileCmd(lineDirective + source, references)
             if result = 0 then assembly 
                else invalidOp <| "Unable to compile F# code.\n----> Errors when compiling as a CLR library:\n" + errors
          
    member x.CompileFunc( parameters: IDictionary<string, Object>) =
        let source, isLambda, fileName = 
            match parameters.["source"] :?> String with
            | input when input.EndsWith(".fs", StringComparison.InvariantCultureIgnoreCase) 
                         || input.EndsWith(".fsx", StringComparison.InvariantCultureIgnoreCase)
                -> File.ReadAllText(input), false, input
            | input -> input, true, String.Empty
        
        let references = getReferences parameters source
        let lineDirective = getLineDirective parameters fileName
        
        let typeName = match parameters.TryGetValue("typeName") with
                       | true, (:? String as typeName) -> typeName
                       | _ -> "Startup"
                       
        let methodName = match parameters.TryGetValue("methodName") with
                         | true, (:? String as methodName) -> methodName
                         | _ -> "Invoke"
            
        // extract the entry point to a class method
        match tryGetAssembly lineDirective source references isLambda with
        | Some assembly -> let startupType = assembly.GetType( typeName, true, true)
                           let instance = Activator.CreateInstance(startupType, false)
                           
                           match startupType.GetMethod(methodName, BindingFlags.Instance ||| BindingFlags.Public) with
                           | null -> invalidOp "Unable to access CLR method to wrap via reflection. Make sure it is a public instance method."
                           | invokeMethod -> // create a Func<object,Task<object>> around the method invocation using reflection
                                             Func<_,_> (fun (input:obj) -> (invokeMethod.Invoke(instance, [| input |])) :?> Task<obj> )
        | None -> invalidOp "Unable to build Dynamic Assembly, no assembly output."