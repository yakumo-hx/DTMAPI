using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace DTMAPI.Author.Analyzers;

// Deliberately bounded to direct async-void lambdas and method groups.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SynchronousCallbackAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        "SDK203", "Platform callbacks must complete synchronously",
        "This platform callback is synchronous. An async-void delegate escapes completion, exception and scope tracking. Complete background work separately, then Post a synchronous result callback.",
        "DTMAPI", DiagnosticSeverity.Error, isEnabledByDefault: true,
        customTags: new[] { WellKnownDiagnosticTags.NotConfigurable });

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(start =>
        {
            var contracts = new[] { "IDtmScheduler", "IDtmRuntimeContext", "IDtmCommands", "IDtmTranslations" }
                .Select(name => start.Compilation.GetTypeByMetadataName("DTMAPI.Abstractions." + name))
                .Where(type => type?.ContainingAssembly.Name == "DTMAPI.Abstractions").ToArray();
            start.RegisterOperationAction(operation =>
            {
                var call = (IInvocationOperation)operation.Operation;
                if (!contracts.Any(type => SymbolEqualityComparer.Default.Equals(type, call.TargetMethod.ContainingType))) return;
                foreach (var argument in call.Arguments)
                {
                    if (argument.Parameter?.Type is not INamedTypeSymbol { DelegateInvokeMethod.ReturnsVoid: true }) continue;
                    IOperation value = argument.Value;
                    while (value is IConversionOperation conversion) value = conversion.Operand;
                    if (value is IDelegateCreationOperation creation) value = creation.Target;
                    var method = value is IAnonymousFunctionOperation lambda ? lambda.Symbol :
                        value is IMethodReferenceOperation reference ? reference.Method : null;
                    if (method is { IsAsync: true, ReturnsVoid: true })
                        operation.ReportDiagnostic(Diagnostic.Create(Rule, argument.Syntax.GetLocation()));
                }
            }, OperationKind.Invocation);
        });
    }
}
