#!/usr/bin/env -S dotnet fsi

// SquatchStyle F# — FSI build script (no FAKE dependency).
// Usage: dotnet fsi build.fsx [target]
// Targets: clean | restore | build | test | format | check-format | pack | push | all

open System
open System.Diagnostics

let sln = "SquatchStyle.slnx"
let analyzerProj = "src/SquatchStyle.Analyzers/SquatchStyle.Analyzers.fsproj"
let testProj = "tests/SquatchStyle.Analyzers.Tests/SquatchStyle.Analyzers.Tests.fsproj"
let outputDir = "artifacts"

let configuration =
    Environment.GetEnvironmentVariable("CONFIGURATION") |> Option.ofObj |> Option.defaultValue "Release"

let exec (cmd: string) (args: string) =
    printfn "$ %s %s" cmd args
    use p = new Process()
    p.StartInfo.FileName <- cmd
    p.StartInfo.Arguments <- args
    p.StartInfo.UseShellExecute <- false
    p.Start() |> ignore
    p.WaitForExit()
    if p.ExitCode <> 0 then
        failwithf "'%s %s' exited with code %d" cmd args p.ExitCode

let dotnet args = exec "dotnet" args

let clean () =
    for d in [ outputDir; "src/SquatchStyle.Analyzers/bin"; "src/SquatchStyle.Analyzers/obj"
               "tests/SquatchStyle.Analyzers.Tests/bin"; "tests/SquatchStyle.Analyzers.Tests/obj" ] do
        if IO.Directory.Exists d then
            IO.Directory.Delete(d, true)
            printfn "Deleted %s" d

let restore () = dotnet $"restore {sln}"

let build () = dotnet $"build {sln} -c {configuration} --no-restore"

let test () =
    IO.Directory.CreateDirectory $"{outputDir}/test-results" |> ignore
    dotnet $"test {testProj} -c {configuration} --results-directory {outputDir}/test-results"

let format () = dotnet "fantomas src tests --recurse"

let checkFormat () = dotnet "fantomas src tests --recurse --check"

let pack () =
    IO.Directory.CreateDirectory $"{outputDir}/packages" |> ignore
    dotnet $"pack {analyzerProj} -c {configuration} --no-build -o {outputDir}/packages"

let push () =
    let nupkgs = IO.Directory.GetFiles($"{outputDir}/packages", "*.nupkg")
    if nupkgs.Length = 0 then
        failwith "No .nupkg files found in artifacts/packages. Run 'pack' first."
    let nupkg = nupkgs |> Array.sortDescending |> Array.head
    printf "NuGet API key: "
    let apiKey = Console.ReadLine()
    if String.IsNullOrWhiteSpace apiKey then
        failwith "API key cannot be empty."
    dotnet $"nuget push {nupkg} -k {apiKey} -s https://api.nuget.org/v3/index.json"

let all () =
    restore ()
    build ()
    test ()
    pack ()

let target =
    match fsi.CommandLineArgs |> Array.tryLast with
    | Some t when not (t.EndsWith ".fsx") -> t.ToLowerInvariant()
    | _ -> "all"

printfn "=== SquatchStyle build — target: %s ===" target

match target with
| "clean" -> clean ()
| "restore" -> restore ()
| "build" -> build ()
| "test" -> test ()
| "format" -> format ()
| "check-format" -> checkFormat ()
| "pack" -> pack ()
| "push" -> push ()
| "all" -> all ()
| other -> failwithf "Unknown target '%s'. Valid: clean | restore | build | test | format | check-format | pack | push | all" other

printfn "=== Done ==="
