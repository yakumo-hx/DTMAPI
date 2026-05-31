using System.Runtime.InteropServices;

namespace DolocTown;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct NoneCondition : ICondition
{
	public bool IsConditionMet(bool reverseCondition)
	{
		return true;
	}
}
