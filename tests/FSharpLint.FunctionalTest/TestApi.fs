﻿namespace FSharpLint.FunctionalTest

module TestApi =

    open System.IO
    open System.Diagnostics
    open NUnit.Framework
    open FSharpLint.Application.Lint
    open FSharp.Compiler.SourceCodeServices
    open FSharp.Compiler.Text

    let (</>) x y = Path.Combine(x, y)

    let basePath = TestContext.CurrentContext.TestDirectory </> ".." </> ".." </> ".." </> ".." </> ".."

    let sourceFile = basePath </> "tests" </> "TypeChecker.fs"

    [<TestFixture(Category = "Acceptance Tests")>]
    type TestApi() =
        let generateAst source =
            let sourceText = SourceText.ofString source
            let checker = FSharpChecker.Create()

            let (options, _diagnostics) =
                checker.GetProjectOptionsFromScript(sourceFile, sourceText)
                |> Async.RunSynchronously

            let parseResults =
                checker.ParseFile(sourceFile, sourceText, options |> checker.GetParsingOptionsFromProjectOptions |> fst)
                |> Async.RunSynchronously

            match parseResults.ParseTree with
            | Some(parseTree) -> parseTree
            | None -> failwith "Failed to parse file."

        [<Category("Performance")>]
        [<Test>]
        member __.``Performance of linting an existing file``() =
            let text = File.ReadAllText sourceFile
            let tree = text |> generateAst
            let fileInfo = { Ast = tree; Source = text; TypeCheckResults = None }

            let stopwatch = Stopwatch.StartNew()
            let times = ResizeArray()

            let iterations = 100

            for _ in 0..iterations do
                stopwatch.Restart()

                let result = lintParsedFile OptionalLintParameters.Default fileInfo sourceFile

                stopwatch.Stop()

                times.Add stopwatch.ElapsedMilliseconds

            let result = times |> Seq.sum |> (fun totalMilliseconds -> totalMilliseconds / int64 iterations)

            Assert.Less(result, 250)
            System.Console.WriteLine(sprintf "Average runtime of linter on parsed file: %d (milliseconds)."  result)

        [<Test>]
        member __.``Lint project``() =
            let projectPath = basePath </> "tests" </> "FSharpLint.FunctionalTest.TestedProject"
            let projectFile = projectPath </> "FSharpLint.FunctionalTest.TestedProject.fsproj"

            let result = lintProject OptionalLintParameters.Default projectFile

            match result with
            | LintResult.Success warnings ->
                Assert.AreEqual(9, warnings.Length)
            | LintResult.Failure _ ->
                Assert.True(false)
                
        [<Test>]
        member __.``Lint solution``() =
            let projectPath = basePath </> "tests" </> "FSharpLint.FunctionalTest.TestedProject"
            let solutionFile = projectPath </> "FSharpLint.FunctionalTest.TestedProject.sln"

            let result = lintSolution OptionalLintParameters.Default solutionFile

            match result with
            | LintResult.Success warnings ->
                Assert.AreEqual(9, warnings.Length)
            | LintResult.Failure _ ->
                Assert.True(false)               