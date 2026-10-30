using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DTMAPI.DemandCallerFixture
{
    public static class Caller
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ReadWithoutRequester(Func<Assembly> resolve) => Read(resolve());

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ReadDynamic() => Read(Assembly.Load("DTMAPI.DemandPayloadFixture"));

        private static string Read(Assembly assembly) => (string)assembly.GetType("DTMAPI.DemandPayloadFixture.Payload").GetMethod("Read").Invoke(null, null);
    }
}
