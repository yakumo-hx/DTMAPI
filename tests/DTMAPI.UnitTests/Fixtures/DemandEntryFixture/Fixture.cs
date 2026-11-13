using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;

namespace DTMAPI.DemandEntryFixture
{
    // Deliberately not deployed: enumerating all types would break the valid entry.
    public sealed class UnusedType : DTMAPI.UnusedReferenceFixture.OptionalBase { }

    public sealed class ExplicitMod : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        public static int EntryCount, UpdateCount, DisposeCount;
        public override void Entry(IDtmHelper value)
        {
            helper = value;
            EntryCount++;
            helper.Events.GameLoop.UpdateTicked += Tick;
            helper.Input.RegisterButton("F11");
        }
        private void Tick(object sender, UpdateTickedEventArgs args) => UpdateCount++;
        public void Dispose()
        {
            helper.Events.GameLoop.UpdateTicked -= Tick;
            helper.Input.UnregisterButton("F11");
            DisposeCount++;
        }
    }

    public static class Entry
    {
        public static string Run() => Read(Assembly.Load("DTMAPI.DemandPayloadFixture"));

        // Exercise the missing RequestingAssembly shape used by Unity Mono.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string RunWithoutRequester(Func<Assembly> resolve) => Read(resolve());

        private static string Read(Assembly payload) => (string)payload.GetType("DTMAPI.DemandPayloadFixture.Payload").GetMethod("Read").Invoke(null, null);
    }
}
