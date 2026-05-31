using System;

namespace RedSaw.Data;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class FingerPrintAttribute : Attribute
{
}
