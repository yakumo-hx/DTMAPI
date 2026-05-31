using System;

namespace XLua;

[Flags]
public enum HotfixFlag
{
	Stateless = 0,
	[Obsolete("use xlua.util.state instead!", true)]
	Stateful = 1,
	ValueTypeBoxing = 2,
	IgnoreProperty = 4,
	IgnoreNotPublic = 8,
	Inline = 0x10,
	IntKey = 0x20,
	AdaptByDelegate = 0x40,
	IgnoreCompilerGenerated = 0x80,
	NoBaseProxy = 0x100
}
