using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using DTMAPI.Authoring.Contracts;

namespace DTMAPI.AuthorSdk;

// Deliberately bounded: recognizable direct async-void lambdas/method groups, not dataflow through variables.
internal static class SynchronousCallbackInspector
{
    internal static IEnumerable<AuthorDiagnostic> Inspect(CSharpCompilation compilation)
    {
        var contracts = new[] { "IDtmScheduler", "IDtmRuntimeContext", "IDtmCommands", "IDtmTranslations" }
            .Select(name => compilation.GetTypeByMetadataName("DTMAPI.Abstractions." + name))
            .Where(type => type?.ContainingAssembly.Name == "DTMAPI.Abstractions").ToArray();
        if (contracts.Length == 0) yield break;
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (InvocationExpressionSyntax invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetOperation(invocation) is not IInvocationOperation call ||
                    !contracts.Any(type => SymbolEqualityComparer.Default.Equals(type, call.TargetMethod.ContainingType))) continue;
                foreach (IArgumentOperation argument in call.Arguments)
                {
                    if (argument.Parameter?.Type is not INamedTypeSymbol { DelegateInvokeMethod.ReturnsVoid: true }) continue;
                    IOperation value = argument.Value;
                    while (value is IConversionOperation conversion) value = conversion.Operand;
                    if (value is IDelegateCreationOperation creation) value = creation.Target;
                    IMethodSymbol? method = value is IAnonymousFunctionOperation lambda ? lambda.Symbol :
                        value is IMethodReferenceOperation reference ? reference.Method : null;
                    if (method is not { IsAsync: true, ReturnsVoid: true }) continue;
                    FileLinePositionSpan line = argument.Syntax.GetLocation().GetLineSpan();
                    yield return new AuthorDiagnostic
                    {
                        Code = "SDK203", Severity = DTMAPI.Authoring.Contracts.DiagnosticSeverity.Error,
                        Path = line.Path + ":" + (line.StartLinePosition.Line + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Message = "This platform callback is synchronous. An async-void delegate escapes scheduler completion, exception and scope tracking. Complete background work separately, then Post a synchronous result callback."
                    };
                }
            }
        }
    }
}
