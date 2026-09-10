using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static int Main(string[] args)
        {
            string suite = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "DtmApiTestSuite").Value!;
            try
            {
                string? focus = Environment.GetEnvironmentVariable("DTMAPI_UNIT_TEST_FOCUS");
                bool list = false;
                for (int index = 0; index < args.Length; index++)
                {
                    if (args[index] == "--list") list = true;
                    else if (args[index] == "--focus" && index + 1 < args.Length)
                    {
                        focus = args[++index];
                        if (string.IsNullOrWhiteSpace(focus)) throw UnknownFocus(focus);
                    }
                    else throw new ArgumentException("Unknown DTMAPI Unit test argument: " + args[index]);
                }

                DirectoryInfo? repository = new DirectoryInfo(AppContext.BaseDirectory);
                while (repository != null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md")))
                    repository = repository.Parent;
                string mapPath = Path.Combine(repository?.FullName
                    ?? throw new InvalidOperationException("Could not locate the Unit suite map repository."),
                    "tests", "DTMAPI.UnitTests", "suites.json");
                using JsonDocument map = JsonDocument.Parse(File.ReadAllText(mapPath));
                JsonElement registration = map.RootElement.GetProperty("suites").EnumerateArray()
                    .Single(row => row.GetProperty("id").GetString() == suite);
                JsonElement defaults = registration.GetProperty("default");
                JsonElement focuses = registration.GetProperty("focuses");
                string[] calls;
                if (string.IsNullOrEmpty(focus) || string.Equals(focus, suite, StringComparison.OrdinalIgnoreCase))
                    calls = defaults.EnumerateArray().Select(value => value.GetString()!).ToArray();
                else
                {
                    JsonProperty[] match = focuses.EnumerateObject()
                        .Where(property => string.Equals(property.Name, focus, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (match.Length != 1) throw UnknownFocus(focus);
                    calls = match[0].Value.EnumerateArray().Select(value => value.GetString()!).ToArray();
                }
                if (list)
                {
                    Console.WriteLine(suite + ": " + string.Join(", ", calls));
                    return 0;
                }
                if (calls.Length == 0)
                    throw new ArgumentException("Suite " + suite + " has no default tests; select its explicit focus.");

                // Resolve the entire route before starting tests. A missing/renamed test cannot
                // silently shrink a suite or fall through to another project's default.
                var methods = new List<MethodInfo>();
                foreach (string call in calls)
                {
                    int separator = call.LastIndexOf('.');
                    string typeName = separator < 0 ? "Program" : call.Substring(0, separator);
                    string methodName = separator < 0 ? call : call.Substring(separator + 1);
                    Type type = typeof(Program).Assembly.GetType("DTMAPI.UnitTests." + typeName, throwOnError: true)!;
                    MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null, types: Type.EmptyTypes, modifiers: null)
                        ?? throw new MissingMethodException(type.FullName, methodName);
                    if (method.ReturnType != typeof(void))
                        throw new InvalidOperationException("Registered Unit test must return void: " + call);
                    methods.Add(method);
                }

                using DtmApiTestSession session = DtmApiTestSession.Start("Unit-" + suite);
                try
                {
                    Console.WriteLine("Unit suite " + suite + ": " + methods.Count + " registered entrypoints");
                    foreach (MethodInfo method in methods)
                    {
                        try { method.Invoke(null, null); }
                        catch (TargetInvocationException exception) when (exception.InnerException != null)
                        {
                            Console.Error.WriteLine("Failed entrypoint: " + method.DeclaringType!.Name + "." + method.Name);
                            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                        }
                    }
                    session.MarkSucceeded();
                    Console.WriteLine("DTMAPI.UnitTests: OK (" + (string.IsNullOrEmpty(focus) ? suite : focus) + ")");
                    return 0;
                }
                catch (Exception exception)
                {
                    session.MarkFailed(exception);
                    throw;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("DTMAPI.UnitTests: FAILED (" + suite + ")");
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static ArgumentException UnknownFocus(string? focus) => new ArgumentException(
            "Unknown DTMAPI Unit test focus: " + focus + ". Default tests were not run.");
    }
}
