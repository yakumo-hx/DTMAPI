using DTMAPI.AuthorSdk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static void TestSynchronousPlatformCallbacksUseResolvedSymbols()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;
            using DTMAPI.Abstractions;
            public class Probe {
                public async void AsyncVoid() { await Task.Yield(); }
                public void Build(IDtmScheduler scheduler, IDtmRuntimeContext context, IDtmCommands commands, IDtmTranslations translations) {
                    scheduler.Post(async () => await Task.Yield());
                    scheduler.NextTick(action: AsyncVoid);
                    context.Subscribe(async snapshot => await Task.Yield());
                    commands.Register("bad", async command => await Task.Yield());
                    translations.SubscribeLanguageChanged(async language => await Task.Yield());
                    scheduler.Post(() => Console.WriteLine("sync"));
                    _ = Task.Run(async () => await Task.Yield());
                    new Lookalike().Post(async () => await Task.Yield());
                }
            }
            public class Lookalike { public void Post(Action action) {} }
            """;
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(DTMAPI.Abstractions.IDtmScheduler).Assembly.Location).Distinct(StringComparer.OrdinalIgnoreCase);
        var references = paths.Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("CallbackProbe", new[] { CSharpSyntaxTree.ParseText(source, path: "Program.cs") },
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0) throw new InvalidOperationException("The semantic probe must really compile: " + string.Join(";", errors.Select(d => d.ToString())));
        var findings = SynchronousCallbackInspector.Inspect(compilation).ToArray();
        if (findings.Length != 5 || findings.Any(d => d.Code != "SDK203" || !d.Path.StartsWith("Program.cs:", StringComparison.Ordinal)))
            throw new InvalidOperationException("Only the five actual platform async-void callbacks should be diagnosed, including named method-group arguments.");
    }
}
