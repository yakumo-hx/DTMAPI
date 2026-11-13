using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using HarmonyLib;

namespace DTMAPI.ChestLocatorEnhancer
{
    // Only the three selected native entrypoints own these short-lived sessions.
    // No inventory is retained here, and no delegate is installed into native UI.
    internal sealed class ChestLocatorUiRefresh
    {
        internal const double RefreshIntervalSeconds = 0.25;
        private readonly IMonitor monitor;
        private ConditionalWeakTable<Array, Source> sources = new ConditionalWeakTable<Array, Source>();
        private ConditionalWeakTable<object, Dialog> dialogData = new ConditionalWeakTable<object, Dialog>();
        private readonly Dictionary<object, Session> sessions = new Dictionary<object, Session>();
        private readonly Dictionary<object, Dialog> dialogs = new Dictionary<object, Dialog>();
        private Session? quantityCreation;
        private int refreshing;
        internal int QueryCount { get; private set; }
        internal int PeriodicQueries { get; private set; }
        internal int RejectedConfirmations { get; private set; }
        internal int SessionCount => sessions.Count + dialogs.Count;
        internal double QueryMilliseconds { get; private set; }
        internal double RefreshMilliseconds { get; private set; }

        internal ChestLocatorUiRefresh(IMonitor monitor) { this.monitor = monitor; }

        internal void RememberEquipmentQuery(object equipment, Array result)
        {
            if (refreshing != 0) return;
            Type type = equipment.GetType();
            if (type != Native.Workbench && type != Native.EquipmentWorkbench) return;
            sources.Remove(result);
            sources.Add(result, new Source(type == Native.Workbench ? Kind.Recipe : Kind.Equipment, equipment));
        }

        internal void RecipeStarted(object state, object[] args, bool succeeded)
        {
            Forget(state);
            // The native ordinary Workbench supplies default crafting and max
            // delegates. Custom recipes, stores, filters and buffers stay native.
            if (!succeeded || !IsOrdinaryRecipe(args) || !(args[4] is Array inventories)) return;
            Bind(state, inventories, Kind.Recipe);
        }

        internal static bool IsOrdinaryRecipe(object[] args) =>
            args.Length == 20 && args[1] == null && args[3] == null &&
            args[7] == null && args[8] == null && args[15] == null && args[19] == null &&
            !(bool)args[12] && !(bool)args[13];

        internal void EquipmentStarted(object state, Array? inventories, bool succeeded)
        {
            Forget(state);
            if (succeeded && inventories != null) Bind(state, inventories, Kind.Equipment);
        }

        private void Bind(object state, Array inventories, Kind kind)
        {
            if (sources.TryGetValue(inventories, out Source source) && source.Kind == kind)
                sessions[state] = new Session(state, source);
        }

        internal void BuildingStarted(object state)
        {
            Forget(state);
            sessions[state] = new Session(state, new Source(Kind.Building, null));
        }

        internal object? BeginQuantityAction(object state)
        {
            Session? previous = quantityCreation;
            quantityCreation = sessions.TryGetValue(state, out Session session) ? session : null;
            return previous;
        }
        internal void EndQuantityAction(object? previous) { quantityCreation = previous as Session; }

        internal void QuantityCreated(object data, object recipe, Array inventories, object? group)
        {
            Session? session = quantityCreation;
            if (session == null || !session.Active || session.State.Target is not object parent) return;
            // Native Equipment makes an array copy; equality here is identity of
            // every member, not a guess based on the currently visible UI.
            Array source = (Array)Native.Get(parent, "inventoriesAround")!;
            if (source.Length != inventories.Length) return;
            for (int i = 0; i < source.Length; i++)
                if (!ReferenceEquals(source.GetValue(i), inventories.GetValue(i))) return;
            dialogData.Remove(data);
            dialogData.Add(data, new Dialog(session, recipe, group));
        }

        internal void QuantityStarted(object state, object data, bool succeeded)
        {
            dialogs.Remove(state);
            if (succeeded && dialogData.TryGetValue(data, out Dialog dialog)) dialogs[state] = dialog;
        }

        internal bool Tick(object state)
        {
            if (dialogs.TryGetValue(state, out Dialog dialog))
            {
                if (!dialog.Parent.Clock.IsDue(Now)) return true;
                try { return RefreshDialog(state, dialog, periodic: true) >= 1; }
                catch (Exception ex) { Failed(dialog.Parent, ex); return true; }
            }
            if (!sessions.TryGetValue(state, out Session session) || !session.Clock.IsDue(Now)) return true;
            try { Refresh(session, periodic: true); }
            catch (Exception ex) { Failed(session, ex); }
            return true;
        }

        internal bool BeforeAction(object state)
        {
            if (!sessions.TryGetValue(state, out Session session)) return true;
            try { Refresh(session, periodic: false); return true; }
            catch (Exception ex) { Failed(session, ex); return false; }
        }

        internal bool BeforeConsume(object state, object? recipe, int count)
        {
            if (!sessions.TryGetValue(state, out Session session)) return true;
            try
            {
                // The native successful consume redraws the parent. Refresh
                // membership here after quantity Pop/Resume, without doing that
                // same render twice. Recipe max is a live native getter.
                Refresh(session, periodic: false, redraw: !HasCurrentSelection(session));
                object selectedRecipe = recipe ?? Native.Get(state, "currentRecipe")!;
                bool allowed = ChestLocatorUiRefreshPolicy.CanConsume(count, MaxCount(session, selectedRecipe), Native.ShouldCost);
                if (!allowed)
                {
                    RejectedConfirmations++;
                    Redraw(session);
                }
                return allowed;
            }
            catch (Exception ex) { RecoverConsumeDisplay(session); Failed(session, ex); return false; }
        }

        internal void ConsumeFailed(object state)
        {
            if (sessions.TryGetValue(state, out Session session)) RecoverConsumeDisplay(session);
        }

        private void RecoverConsumeDisplay(Session session)
        {
            // Native cost/output notifications may throw before its final
            // redraw. Show current state without retrying or undoing gameplay.
            try { Refresh(session, periodic: false); }
            catch (Exception ex) { Failed(session, ex); }
        }

        internal bool BeforeQuantityConfirm(object state, int count)
        {
            if (!dialogs.TryGetValue(state, out Dialog dialog)) return true;
            try
            {
                int max = RefreshDialog(state, dialog, periodic: false);
                bool allowed = max > 0 && count > 0 && count <= max;
                if (!allowed) RejectedConfirmations++;
                return allowed;
            }
            catch (Exception ex) { Failed(dialog.Parent, ex); return false; }
        }

        private void Refresh(Session session, bool periodic, bool redraw = true)
        {
            if (!session.Active) throw new InvalidOperationException("The originating crafting UI has closed.");
            object state = session.State.Target ?? throw new InvalidOperationException("The originating UI no longer exists.");
            long started = Stopwatch.GetTimestamp();
            Array inventories;
            refreshing++;
            try { inventories = session.Source.Query(); }
            finally { refreshing--; }
            QueryCount++;
            if (periodic) PeriodicQueries++;
            QueryMilliseconds += (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
            Native.Set(state, "inventoriesAround", inventories);
            session.Clock.Mark(Now);
            session.ErrorLogged = false;
            if (redraw) Redraw(session);
            RefreshMilliseconds += (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
        }

        private static void Redraw(Session session)
        {
            object state = session.State.Target ?? throw new InvalidOperationException("The originating UI no longer exists.");
            object panel = Native.Get(state, "panel")!;
            if (session.Source.Kind == Kind.Recipe) panel = Native.Get(panel, "recipePanel")!;
            Native.Call(panel, "RefreshView");
        }

        private static bool HasCurrentSelection(Session session)
        {
            object state = session.State.Target!;
            object panel = Native.Get(state, "panel")!;
            Kind kind = session.Source.Kind;
            if (kind == Kind.Recipe) panel = Native.Get(panel, "recipePanel")!;
            int index = (int)Native.Get(panel, "selectedIndex")!;
            int slot = (int)Native.Get(panel, "currentSlotIndex")!;
            int capacity = (int)Native.Get(panel, kind == Kind.Equipment ? "TotalCapacity" : "totalCapacity")!;
            Array? data = Native.Get(panel, "currentDatas") as Array;
            IList choices = (IList)Native.Get(state, kind == Kind.Recipe ? "unlockedRecipes" : kind == Kind.Equipment ? "currentEquipments" : "unlockedBuildings")!;
            if (index < 0 || index >= capacity || index >= choices.Count || data == null || slot < 0 || slot >= data.Length) return false;
            object? selected = Native.Get(state, kind == Kind.Recipe ? "currentRecipe" : kind == Kind.Equipment ? "_currentEquipmentName" : "currentBuildingName");
            // Preserve native empty-page/selection repair rather than assuming
            // every possible caller has a stable ordinary selection.
            return kind == Kind.Recipe ? ReferenceEquals(selected, choices[index]) : Equals(selected, choices[index]);
        }

        private static int MaxCount(Session session, object recipe)
        {
            object state = session.State.Target!;
            if (session.Source.Kind == Kind.Recipe)
                return (int)Native.Get(state, "maxCraftCount")!;
            int money = session.Source.Kind == Kind.Building ? (int)Native.Get(Native.Archive, "CurrentMoney")! : 0;
            return (int)Native.MaxAfford.Invoke(recipe, new[] { Native.Get(state, "inventoriesAround"), money })!;
        }

        private int RefreshDialog(object state, Dialog dialog, bool periodic)
        {
            Refresh(dialog.Parent, periodic);
            int max = Math.Min(999, MaxCount(dialog.Parent, dialog.Recipe));
            if (max <= 0)
            {
                // A zero-capacity native quantity bar cannot represent a choice.
                // Return to the freshly redrawn parent through native cancellation.
                Native.QuantityCancel.Invoke(state, null);
                return 0;
            }
            object panel = Native.Get(state, "panel")!;
            int count = Math.Min(max, Math.Max(1, (int)Native.Get(panel, "currentCount")!));
            object parent = dialog.Parent.State.Target!;
            long renderStarted = Stopwatch.GetTimestamp();
            object data = Native.QuantityConstructor.Invoke(new[] { max, dialog.Recipe, Native.Get(parent, "inventoriesAround"), dialog.Group, count });
            Native.Set(state, "data", data);
            Native.QuantityRender.Invoke(panel, new[] { data });
            RefreshMilliseconds += (Stopwatch.GetTimestamp() - renderStarted) * 1000.0 / Stopwatch.Frequency;
            return max;
        }

        internal void Forget(object state)
        {
            if (sessions.TryGetValue(state, out Session session)) session.Active = false;
            sessions.Remove(state);
            dialogs.Remove(state);
        }

        internal void Reset()
        {
            foreach (Session session in sessions.Values) session.Active = false;
            sessions.Clear(); dialogs.Clear();
            quantityCreation = null;
            sources = new ConditionalWeakTable<Array, Source>();
            dialogData = new ConditionalWeakTable<object, Dialog>();
            QueryCount = 0; PeriodicQueries = 0; RejectedConfirmations = 0; QueryMilliseconds = 0; RefreshMilliseconds = 0;
        }

        private void Failed(Session session, Exception ex)
        {
            session.Clock.Mark(Now);
            if (session.ErrorLogged) return;
            session.ErrorLogged = true;
            monitor.Log("Chest crafting refresh failed closed: " + ex.GetBaseException().Message, LogLevel.Error);
        }

        private static double Now => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        private enum Kind { Recipe, Equipment, Building }
        private sealed class Source
        {
            internal readonly Kind Kind;
            private readonly WeakReference archive = new WeakReference(Native.Archive);
            private readonly WeakReference room = new WeakReference(Native.Room);
            private readonly WeakReference equipment;
            internal Source(Kind kind, object? equipment) { Kind = kind; this.equipment = new WeakReference(equipment); }
            internal Array Query()
            {
                if (!ReferenceEquals(archive.Target, Native.Archive) || !ReferenceEquals(room.Target, Native.Room))
                    throw new InvalidOperationException("The original archive or room changed.");
                if (Kind == Kind.Building) return (Array)Native.BackpackQuery.Invoke(null, new object[] { true })!;
                object source = equipment.Target ?? throw new InvalidOperationException("The source workbench no longer exists.");
                if ((bool)Native.Get(source, "WaitRemove")! || !ReferenceEquals(Native.Get(source, "CurrentRoom"), Native.Room))
                    throw new InvalidOperationException("The source workbench left the original room.");
                return (Array)Native.EquipmentQuery.Invoke(null, new[] { source })!;
            }
        }
        private sealed class Session
        {
            internal readonly WeakReference State;
            internal readonly Source Source;
            internal readonly ChestLocatorUiRefreshClock Clock = new ChestLocatorUiRefreshClock();
            internal bool Active = true, ErrorLogged;
            internal Session(object state, Source source) { State = new WeakReference(state); Source = source; Clock.Mark(Now); }
        }
        private sealed class Dialog
        {
            internal readonly Session Parent;
            internal readonly object Recipe;
            internal readonly object? Group;
            internal Dialog(Session parent, object recipe, object? group) { Parent = parent; Recipe = recipe; Group = group; }
        }

        internal sealed class Patch
        {
            internal readonly MethodBase Target;
            internal readonly MethodInfo Callback;
            internal readonly bool Prefix;
            internal readonly bool Finalizer;
            internal Patch(MethodBase target, string callback, bool prefix, bool finalizer = false)
            { Target = target; Callback = typeof(ChestLocatorEnhancerCallbacks).GetMethod(callback)!; Prefix = prefix; Finalizer = finalizer; }
        }
        internal static Patch[] ResolvePatches() => new[]
        {
            new Patch(Native.EquipmentQuery, "EquipmentInventoriesPostfix", false),
            new Patch(Native.RecipeStartup, "RecipeStartupPostfix", false),
            new Patch(Native.Require(Native.EquipmentState, "HandleStartUpArgs", 2), "EquipmentStartupPostfix", false),
            new Patch(Native.Require(Native.BuildingState, "BeforeRegister", 0), "BuildingStartupPostfix", false),
            new Patch(Native.Require(Native.UiState, "OnUpdate", 1), "UiUpdatePrefix", true),
            new Patch(Native.Require(Native.UiState, "OnExit", 0), "UiExitPostfix", false),
            new Patch(Native.Require(Native.RecipeState, "CheckCondition", 0), "CraftActionPrefix", true),
            new Patch(Native.Require(Native.RecipeState, "OnStartButtonClick", 2), "QuantityScopePrefix", true),
            new Patch(Native.Require(Native.EquipmentState, "StartCraft", 2), "QuantityActionPrefix", true),
            new Patch(Native.Require(Native.BuildingState, "OnStartButtonClick", 1), "QuantityActionPrefix", true),
            new Patch(Native.Require(Native.RecipeState, "OnStartButtonClick", 2), "QuantityScopeFinalizer", false, true),
            new Patch(Native.Require(Native.EquipmentState, "StartCraft", 2), "QuantityScopeFinalizer", false, true),
            new Patch(Native.Require(Native.BuildingState, "OnStartButtonClick", 1), "QuantityScopeFinalizer", false, true),
            new Patch(Native.Require(Native.RecipeState, "TryConfirmCraft", 1), "RecipeConfirmPrefix", true),
            new Patch(Native.Require(Native.EquipmentState, "MakeItem", 2), "CraftConsumePrefix", true),
            new Patch(Native.Require(Native.BuildingState, "MakeItem", 2), "CraftConsumePrefix", true),
            new Patch(Native.Require(Native.RecipeState, "TryConfirmCraft", 1), "CraftConsumeFinalizer", false, true),
            new Patch(Native.Require(Native.EquipmentState, "MakeItem", 2), "CraftConsumeFinalizer", false, true),
            new Patch(Native.Require(Native.BuildingState, "MakeItem", 2), "CraftConsumeFinalizer", false, true),
            new Patch(Native.QuantityConstructor, "QuantityDataPostfix", false),
            new Patch(Native.Require(Native.QuantityBase, "HandleStartUpArgs", 3), "QuantityStartupPostfix", false),
            new Patch(Native.Require(Native.QuantityBase, "Confirm", 1), "QuantityConfirmPrefix", true)
        };

        private static class Native
        {
            private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            private static readonly Dictionary<Tuple<Type, string>, MemberInfo> members = new Dictionary<Tuple<Type, string>, MemberInfo>();
            private static readonly Dictionary<Tuple<Type, string>, MethodInfo> calls = new Dictionary<Tuple<Type, string>, MethodInfo>();
            internal static readonly Type Workbench = Type("DolocTown.GameData.Workbench");
            internal static readonly Type EquipmentWorkbench = Type("DolocTown.GameData.EquipmentWorkbench");
            internal static readonly Type RecipeState = Type("DolocTown.RecipePanelUiState");
            internal static readonly Type EquipmentState = Type("DolocTown.EquipmentPanelUiState");
            internal static readonly Type BuildingState = Type("DolocTown.BuildingPanelUiState");
            internal static readonly Type UiState = Type("DolocTown.DolocGameUiState");
            internal static readonly Type QuantityBase = Type("DolocTown.CraftQuantitySubmitUiState").BaseType!;
            internal static readonly MethodInfo RecipeStartup = Require(RecipeState, "HandleStartUpArgs", 20);
            internal static readonly MethodInfo EquipmentQuery = Require(typeof(DolocAPI), "GetInventoriesAroundEquipment", 1);
            internal static readonly MethodInfo BackpackQuery = Require(typeof(DolocAPI), "GetBackpackWithInsideBoxes", 1);
            internal static readonly MethodInfo MaxAfford = Require(Type("DolocTown.IRecipe"), "MaxAffordScale", 2);
            internal static readonly MethodInfo QuantityCancel = Require(QuantityBase, "Cancel", 0);
            internal static readonly MethodInfo QuantityRender = Require(Type("DolocTown.UI.CraftQuantitySubmitPanel"), "Render", 1);
            internal static readonly ConstructorInfo QuantityConstructor = Type("DolocTown.UI.CraftQuantityData").GetConstructor(new[] { typeof(int), Type("DolocTown.IRecipe"), Type("DolocTown.LinearInventory").MakeArrayType(), Type("DolocTown.IRecipeGroup"), typeof(int) })
                ?? throw new MissingMethodException("CraftQuantityData native constructor");
            internal static object Archive => Get(typeof(DolocAPI), "archiveHandle")!;
            internal static object Room => Get(typeof(DolocAPI), "CurrentRoom")!;
            internal static object? CurrentState => Get(Get(typeof(DolocAPI), "userInput")!, "CurrentState");
            internal static bool ShouldCost => (bool)Get(Get(typeof(DolocAPI), "gameManager")!, "shouldBuilderCostAssets")!;
            private static Type Type(string name) => typeof(DolocAPI).Assembly.GetType(name, true)!;
            internal static MethodInfo Require(Type type, string name, int parameters)
            {
                MethodInfo? found = null;
                foreach (MethodInfo method in type.GetMethods(Flags))
                    if (method.Name == name && method.GetParameters().Length == parameters)
                    {
                        if (found != null) throw new AmbiguousMatchException(type.FullName + "." + name);
                        found = method;
                    }
                return found ?? throw new MissingMethodException(type.FullName, name);
            }
            private static MemberInfo Member(Type type, string name)
            {
                var key = Tuple.Create(type, name);
                if (members.TryGetValue(key, out MemberInfo result)) return result;
                for (Type? current = type; current != null; current = current.BaseType)
                {
                    MemberInfo? member = (MemberInfo?)current.GetField(name, Flags) ?? current.GetProperty(name, Flags);
                    if (member != null) { members.Add(key, member); return member; }
                }
                throw new MissingMemberException(type.FullName, name);
            }
            internal static object? Get(object owner, string name)
            {
                MemberInfo member = Member(owner as Type ?? owner.GetType(), name);
                object? instance = owner is Type ? null : owner;
                return member is FieldInfo field ? field.GetValue(instance) : ((PropertyInfo)member).GetValue(instance, null);
            }
            internal static void Set(object owner, string name, object value)
            {
                MemberInfo member = Member(owner.GetType(), name);
                if (member is FieldInfo field) field.SetValue(owner, value);
                else ((PropertyInfo)member).SetValue(owner, value, null);
            }
            internal static void Call(object owner, string name)
            {
                var key = Tuple.Create(owner.GetType(), name);
                if (!calls.TryGetValue(key, out MethodInfo method))
                {
                    method = AccessTools.Method(owner.GetType(), name, System.Type.EmptyTypes)
                        ?? throw new MissingMethodException(owner.GetType().FullName, name);
                    calls.Add(key, method);
                }
                method.Invoke(owner, null);
            }
        }
    }

    internal sealed class ChestLocatorUiRefreshClock
    {
        private double next;
        internal bool IsDue(double now) => now >= next;
        internal void Mark(double now) { next = now + ChestLocatorUiRefresh.RefreshIntervalSeconds; }
    }
    internal static class ChestLocatorUiRefreshPolicy
    {
        internal static bool CanConsume(int requested, int available, bool shouldCost) => requested > 0 && (!shouldCost || requested <= available);
    }
}
