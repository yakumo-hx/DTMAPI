using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.BepInExBootstrap;
using DTMAPI.Core.Runtime;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void BootstrapFrameCallbacksTickAndSampleOnceInEitherOrder()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                foreach (bool nativeFirst in new[] { true, false })
                {
                    using var fixture = new BootstrapFrameFixture();
                    fixture.Frame(0, down: true, pressed: true);
                    if (nativeFirst)
                    {
                        fixture.Native();
                        fixture.PlayerLoop();
                    }
                    else
                    {
                        fixture.PlayerLoop();
                        fixture.Native();
                    }
                    fixture.Native();
                    fixture.PlayerLoop();
                    fixture.AssertProgress(1);
                    Assert(fixture.Presses == 1, "Either callback order must dispatch the physical press once.");

                    fixture.Frame(1, down: true, pressed: true);
                    fixture.PlayerLoop();
                    fixture.AssertProgress(1);
                    fixture.Native();
                    fixture.PlayerLoop();
                    fixture.Native();
                    fixture.AssertProgress(2);
                    Assert(fixture.Presses == 1, "A repeated backend pressed flag while held must not create another press.");

                    fixture.Frame(2, released: true);
                    fixture.Native();
                    fixture.PlayerLoop();
                    fixture.AssertProgress(3);
                    Assert(fixture.Releases == 1, "The release is delivered once to the original press recipient.");
                }
            }
            finally { RestorePersistentRoot(previousRoot); }
        }

        private static void BootstrapPlayerLoopResumesAfterNativeStops()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                using var fixture = new BootstrapFrameFixture();
                fixture.Frame(200, down: true, pressed: true);
                fixture.Native();
                fixture.AssertProgress(1);

                fixture.Frame(201, released: true);
                fixture.PlayerLoop();
                fixture.PlayerLoop();
                fixture.AssertProgress(1);
                Assert(fixture.Releases == 0, "The frame after native progress must leave input untouched for the native callback.");

                fixture.Frame(202, released: true);
                fixture.PlayerLoop();
                fixture.PlayerLoop();
                fixture.AssertProgress(2);
                Assert(fixture.Releases == 1, "PlayerLoop resumes on the next frame without native progress and settles the held input.");

                fixture.Frame(203, down: true, pressed: true);
                fixture.PlayerLoop();
                fixture.AssertProgress(3);
                Assert(fixture.Presses == 2, "Continued PlayerLoop frames accept a new physical press without waiting for a timer.");
            }
            finally { RestorePersistentRoot(previousRoot); }
        }

        private static void BootstrapPlayerLoopReinstallPreservesFrameState()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                using var fixture = new BootstrapFrameFixture();
                fixture.Frame(300, down: true, pressed: true);
                fixture.PlayerLoop();
                fixture.Reinstall("SaveLoaded");
                fixture.PlayerLoop();
                fixture.Native();
                fixture.AssertProgress(1);

                fixture.Frame(301, down: true);
                fixture.PlayerLoop();
                fixture.Reinstall("ReturnedToTitle");
                fixture.PlayerLoop();
                fixture.AssertProgress(1);
                fixture.Native();
                fixture.Reinstall("LoadGameReturned:1:True");
                fixture.PlayerLoop();
                fixture.Native();
                fixture.AssertProgress(2);

                fixture.Frame(302, down: true);
                fixture.PlayerLoop();
                fixture.AssertProgress(2);
                fixture.Frame(303, released: true);
                fixture.PlayerLoop();
                fixture.AssertProgress(3);
                Assert(fixture.Presses == 1 && fixture.Releases == 1, "Reinstallation must preserve the physical input cycle across both drivers.");

                fixture.Plugin.OnApplicationQuit();
                fixture.Frame(304);
                fixture.PlayerLoop();
                fixture.Native();
                fixture.AssertProgress(3);
            }
            finally { RestorePersistentRoot(previousRoot); }
        }

        private sealed class BootstrapFrameFixture : IDisposable
        {
            private const string Button = "F6";
            private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;
            private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
            private readonly FieldInfo timeType = typeof(ReflectedUnityInput).GetField("unityTimeType", PrivateStatic)!;
            private readonly FieldInfo frameCount = typeof(ReflectedUnityInput).GetField("unityFrameCount", PrivateStatic)!;
            private readonly IDictionary controls = (IDictionary)typeof(ReflectedUnityInput).GetField("cachedInputSystemControls", PrivateStatic)!.GetValue(null)!;
            private readonly HashSet<string> invalidControls = (HashSet<string>)typeof(ReflectedUnityInput).GetField("cachedInputSystemInvalidKeys", PrivateStatic)!.GetValue(null)!;
            private readonly object? previousTimeType;
            private readonly object? previousFrameCount;
            private readonly object? previousControl;
            private readonly bool hadPreviousControl;
            private readonly bool wasInvalidControl;
            private readonly FrameButtonControl control = new FrameButtonControl();
            private readonly DtmApiRuntime runtime;
            public BootstrapPlugin Plugin { get; }
            public int Ticks { get; private set; }
            public int Presses { get; private set; }
            public int Releases { get; private set; }

            public BootstrapFrameFixture()
            {
                runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                runtime.Start();
                const string owner = "DTMAPI.Tests.FrameDriver";
                IEventsHelper events = CreateEventsProxy(runtime, owner);
                events.GameLoop.UpdateTicked += (_, _) => Ticks++;
                events.Input.ButtonPressed += (_, _) => Presses++;
                events.Input.ButtonReleased += (_, _) => Releases++;
                runtime.Input.CreateOwnerBound(owner).RegisterKeybind("probe", Button, DtmInputScope.Always);
                Plugin = new BootstrapPlugin();
                SetPrivateField(Plugin, "runtime", runtime);
                SetPrivateField(Plugin, "initialized", true);
                SetPrivateField(Plugin, "startupAttempted", true);

                previousTimeType = timeType.GetValue(null);
                previousFrameCount = frameCount.GetValue(null);
                hadPreviousControl = controls.Contains(Button);
                previousControl = controls[Button];
                wasInvalidControl = invalidControls.Remove(Button);
                Type cachedControl = typeof(ReflectedUnityInput).GetNestedType("CachedInputSystemControl", BindingFlags.NonPublic)!;
                controls[Button] = Activator.CreateInstance(cachedControl, control)!;
                timeType.SetValue(null, typeof(FrameClock));
                frameCount.SetValue(null, typeof(FrameClock).GetProperty(nameof(FrameClock.frameCount))!);
                ReflectedUnityInput.ClearTransientState();
            }

            public void Frame(int number, bool down = false, bool pressed = false, bool released = false)
            {
                FrameClock.frameCount = number;
                control.Down = down;
                control.Pressed = pressed;
                control.Released = released;
            }

            public void Native() => Invoke("OnNativeGameFrame");
            public void PlayerLoop() => Invoke("OnPlayerLoopUpdate");
            public void Reinstall(string reason) => Invoke("ReinstallPlayerLoopFrameDriver", reason);

            private void Invoke(string method, params object[] arguments) =>
                typeof(BootstrapPlugin).GetMethod(method, PrivateInstance)!.Invoke(Plugin, arguments);

            public void AssertProgress(int expected)
            {
                Assert(Ticks == expected, "Real Bootstrap callbacks must update Runtime once per accepted frame; expected=" + expected + ", actual=" + Ticks + ".");
                Assert(control.DownReads == expected && control.PressReads == expected && control.ReleaseReads == expected,
                    "Each accepted frame must read each button state once; expected=" + expected + ", held=" + control.DownReads + ", pressed=" + control.PressReads + ", released=" + control.ReleaseReads + ".");
            }

            public void Dispose()
            {
                try { Plugin.OnApplicationQuit(); }
                finally
                {
                    timeType.SetValue(null, previousTimeType);
                    frameCount.SetValue(null, previousFrameCount);
                    if (hadPreviousControl) controls[Button] = previousControl;
                    else controls.Remove(Button);
                    if (wasInvalidControl) invalidControls.Add(Button);
                    else invalidControls.Remove(Button);
                    ReflectedUnityInput.ClearTransientState();
                }
            }
        }

        private static class FrameClock
        {
            public static int frameCount { get; set; }
        }

        private sealed class FrameButtonControl
        {
            public bool Down;
            public bool Pressed;
            public bool Released;
            public int DownReads;
            public int PressReads;
            public int ReleaseReads;
            public bool isPressed { get { DownReads++; return Down; } }
            public bool wasPressedThisFrame { get { PressReads++; return Pressed; } }
            public bool wasReleasedThisFrame { get { ReleaseReads++; return Released; } }
        }
    }
}
