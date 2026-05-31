using System;

namespace RedSaw.CommandLineInterface;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
public class DebugObjectAttribute : Attribute
{
}
