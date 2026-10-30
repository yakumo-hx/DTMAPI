using System;
namespace DTMAPI.Modules.Monsters
{
    internal sealed class NativeIdentityConflictException : InvalidOperationException
    {
        internal NativeIdentityConflictException(string message) : base(message) { }
    }
}
