using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown.Native
{
    internal sealed class NativeMenuInputAdapter : IDisposable
    {
        private readonly HarmonyReflectionPatcher patcher;
        private readonly Func<bool> isTitle;
        private readonly DtmApiRuntime runtime;
        private Func<object?>? userInput;
        private readonly Dictionary<string, Func<object, bool>> buttons = new Dictionary<string, Func<object, bool>>();
        private Func<object, double>? x;
        private Func<object, double>? y;
        private bool attempted;
        private bool hooked;
        private bool modal;
        private bool draining;
        private static NativeMenuInputAdapter? active;

        internal NativeMenuInputAdapter(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            patcher = new HarmonyReflectionPatcher(runtime, "dtmapi.title-settings.navigation");
            isTitle = () => runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase);
        }

        internal bool Ready => hooked && userInput != null;
        internal bool Draining => draining;
        internal void SetModal(bool value)
        {
            if (modal && !value) draining = true;
            modal = value;
        }

        internal MenuInput Read()
        {
            if (!isTitle()) { modal = false; draining = false; return default; }
            if (!attempted) Initialize();
            if (!Ready) return default;
            try
            {
                object? input = userInput?.Invoke();
                if (input == null) return default;
                var result = new MenuInput {
                    X = x!(input), Y = y!(input),
                    Up = Read(input, "BaseIsUpInProgress"), Down = Read(input, "BaseIsDownInProgress"),
                    Left = Read(input, "BaseIsLeftInProgress"), Right = Read(input, "BaseIsRightInProgress"),
                    Confirm = Read(input, "BaseIsConfirmInProgress"), Cancel = Read(input, "BaseIsCancelInProgress"),
                    ConfirmEdge = Read(input, "BaseIsConfirmPressedThisFrame"), CancelEdge = Read(input, "BaseIsCancelPressed")
                };
                if (draining && result.IsNeutral) draining = false;
                return result;
            }
            catch { return default; }
        }

        private bool Read(object input, string name) => buttons[name](input);
        private void Initialize()
        {
            attempted = true;
            try
            {
                Type? api = Type.GetType("DolocAPI, Assembly-CSharp");
                PropertyInfo? property = api?.GetProperty("UserInput", BindingFlags.Static | BindingFlags.Public);
                if (property == null) return;
                Type inputType = property.PropertyType;
                userInput = Expression.Lambda<Func<object?>>(Expression.Convert(Expression.Property(null, property), typeof(object))).Compile();
                ParameterExpression input = Expression.Parameter(typeof(object));
                Expression typed = Expression.Convert(input, inputType);
                foreach (string name in new[] { "BaseIsUpInProgress", "BaseIsDownInProgress", "BaseIsLeftInProgress", "BaseIsRightInProgress", "BaseIsConfirmInProgress", "BaseIsCancelInProgress", "BaseIsConfirmPressedThisFrame", "BaseIsCancelPressed" })
                    buttons[name] = Expression.Lambda<Func<object, bool>>(Expression.Property(typed, name), input).Compile();
                Expression move = Expression.Property(typed, "BaseMove");
                x = Expression.Lambda<Func<object, double>>(Expression.Convert(Expression.Field(move, "x"), typeof(double)), input).Compile();
                y = Expression.Lambda<Func<object, double>>(Expression.Convert(Expression.Field(move, "y"), typeof(double)), input).Compile();
                hooked = patcher.TryPatchPrefix("DolocTown.HomePageUiState, Assembly-CSharp", "OnUiUpdate", typeof(NativeMenuInputAdapter).GetMethod(nameof(AllowNativeTitleUpdate), BindingFlags.Public | BindingFlags.Static), 1);
                if (hooked) active = this;
                runtime.SetHookStatus("UI.TitleConfigNavigation", hooked ? "installed" : "degraded", "HomePageUiState.OnUiUpdate(Single)",
                    hooked ? "Title-only modal guard and native menu action readers installed; keyboard/controller journey not yet verified." : "Native title guard unavailable; custom navigation disabled.");
            }
            catch (Exception ex)
            {
                userInput = null;
                runtime.SetHookStatus("UI.TitleConfigNavigation", "degraded", "Native menu action shape", ex.GetType().Name + ": " + ex.Message);
            }
        }

        public static bool AllowNativeTitleUpdate() => active == null || !active.isTitle() || (!active.modal && !active.draining);

        public void Dispose()
        {
            modal = false; draining = false;
            if (ReferenceEquals(active, this)) active = null;
            patcher.TryUnpatchAllOwnedPatches();
            userInput = null; buttons.Clear(); x = null; y = null; hooked = false;
        }
    }

    internal struct MenuInput
    {
        internal double X, Y;
        internal bool Up, Down, Left, Right, Confirm, Cancel, ConfirmEdge, CancelEdge;
        internal bool IsNeutral => Math.Abs(X) < 0.35 && Math.Abs(Y) < 0.35 && !Up && !Down && !Left && !Right && !Confirm && !Cancel && !ConfirmEdge && !CancelEdge;
    }
}
