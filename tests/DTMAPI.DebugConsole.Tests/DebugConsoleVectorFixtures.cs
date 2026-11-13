namespace UnityEngine
{
    internal readonly struct Vector2
    {
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public readonly float x;
        public readonly float y;
    }
}

namespace DTMAPI.UnitTests
{
    internal sealed class DebugConsoleAgentPositionFixture
    {
        public float x;
        public float y;
        public float z;
    }
}
