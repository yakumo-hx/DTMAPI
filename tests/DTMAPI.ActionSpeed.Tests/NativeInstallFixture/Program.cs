using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using DolocTown;
using HarmonyLib;
using Yuuka.DTMAPI.ActionSpeed;

internal static class Program
{
    private const string Owner = "Yuuka.DTMAPI.ActionSpeed.Native";
    private const string OtherOwner = "Tests.ActionSpeed.Unrelated";
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private static int Main(string[] args)
    {
        NativeActions? native = null;
        var other = new Harmony(OtherOwner);
        try
        {
            var author = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(args[0]));
            var intent = (Dictionary<string, object>)author["nativeReferences"];
            Require((string)intent["harmonyOwner"] == Owner && NativeActions.HarmonyOwner == Owner, "exact current owner");
            var targets = new List<MethodInfo>();
            var members = ((System.Collections.IEnumerable)intent["requiredMembers"]).Cast<Dictionary<string, object>>().ToArray();
            Require(members.Length == 43, "current explicit native signature inventory");
            foreach (Dictionary<string, object> member in members)
            {
                Require((string)member["assemblyIdentity"] == "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "declared host identity");
                Type type = typeof(Program).Assembly.GetType((string)member["declaringType"], true)!;
                string name = (string)member["name"];
                if ((string)member["kind"] == "field")
                {
                    var field = type.GetField(name, Declared)!;
                    Require(field != null && Signature(field.FieldType) == (string)member["returnType"] && field.IsStatic == (bool)member["isStatic"], "native field " + name);
                    continue;
                }
                string[] parameters = ((System.Collections.IEnumerable)member["parameterTypes"]).Cast<string>().ToArray();
                var matches = type.GetMethods(Declared).Where(method => method.Name == name &&
                    method.GetParameters().Select(parameter => Signature(parameter.ParameterType)).SequenceEqual(parameters)).ToArray();
                Require(matches.Length == 1, "exact declared target " + type.FullName + "." + name);
                var target = matches[0];
                Require(Signature(target.ReturnType) == (string)member["returnType"] && target.IsStatic == (bool)member["isStatic"], "native signature " + target);
                if (name != "get_CouldHarvest") targets.Add(target);
            }
            var otherTarget = typeof(SimpleWell).GetMethod("OnInteract")!;
            other.Patch(otherTarget, prefix: new HarmonyMethod(typeof(Program), nameof(Unrelated)));
            native = new NativeActions(() => new ActionConfig());
            native.Install();
            var actualTargets = Harmony.GetAllPatchedMethods().Where(method => Harmony.GetPatchInfo(method).Owners.Contains(Owner)).ToArray();
            Require(actualTargets.Length == targets.Count && targets.All(actualTargets.Contains), "actual installation equals declared 41 targets");
            foreach (var target in targets)
            {
                string? prefix = "BeginOperation", postfix = null, finalizer = "EndOperation";
                switch (target.Name)
                {
                    case "OnEnter": prefix = null; postfix = "Entered"; finalizer = null; break;
                    case "OnExit": prefix = "Leaving"; finalizer = null; break;
                    case "PlayAnimation": prefix = "Playing"; finalizer = null; break;
                    case "UseItemContinues": prefix = "Drinking"; finalizer = null; break;
                    case "OnUpdateInNormalState": prefix = null; postfix = "NormalUpdate"; finalizer = null; break;
                    case "OnPause": prefix = "Pausing"; finalizer = null; break;
                }
                var patches = Harmony.GetPatchInfo(target);
                Check(patches.Prefixes, prefix, target); Check(patches.Postfixes, postfix, target);
                Check(patches.Finalizers, finalizer, target); Check(patches.Transpilers, null, target);
            }
            var scope = (ActionScope)typeof(NativeActions).GetField("scope", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(native)!;
            NativeHost.DuringCall = () => { Require(scope.Current == ActionKind.Fill, "prefix establishes operation"); throw new ApplicationException("native fixture failure"); };
            try { new SimpleWell().OnInteract(); throw new Exception("native exception was swallowed"); }
            catch (ApplicationException ex) { Require(ex.Message == "native fixture failure", "original failure"); }
            Require(scope.Current == ActionKind.None, "finalizer restores operation on native failure");
            NativeHost.DuringCall = () => { };
            native.Dispose(); native.Dispose();
            Require(!Harmony.GetAllPatchedMethods().Any(method => Harmony.GetPatchInfo(method).Owners.Contains(Owner)), "exact-owner unpatch");
            Require(Harmony.GetPatchInfo(otherTarget).Owners.Contains(OtherOwner), "unrelated owner survives");
            using (var next = new NativeActions(() => new ActionConfig())) { next.Install(); }
            Require(!Harmony.GetAllPatchedMethods().Any(method => Harmony.GetPatchInfo(method).Owners.Contains(Owner)), "reinstall and dispose");
            Console.WriteLine("ActionSpeed NativeInstallFixture: PASS (real installer, 41 targets / 43 signatures, prefix/postfix/finalizer, exception restoration, exact-owner cleanup). Native host bodies are fixtures; no game acceptance.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { NativeHost.DuringCall = () => { }; native?.Dispose(); other.UnpatchSelf(); }
    }
    private static string Signature(Type type) => (type.Assembly == typeof(object).Assembly ? "[bcl]" : "[Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]") + type.FullName;
    private static void Check(IEnumerable<Patch> patches, string? name, MethodInfo target)
    {
        var own = patches.Where(patch => patch.owner == Owner).ToArray();
        Require(own.Length == (name == null ? 0 : 1) && (name == null || own[0].PatchMethod == typeof(NativeActions).GetMethod(name, Declared)), "callback " + target + " / " + name);
    }
    private static void Unrelated() { }
    private static void Require(bool condition, string label) { if (!condition) throw new Exception(label); }
}
