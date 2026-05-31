using UnityEngine;

namespace DolocTown.GameServiceLocator;

public interface ISystemConfigProvider
{
	Color backgroundColor_lv0 { get; }

	Color backgroundColor_lv1 { get; }

	Color backgroundColor_lv2 { get; }

	Color backgroundColor_lv3 { get; }

	Color backgroundColor_lv1_95alpha { get; }

	Color backgroundColor_lv1_78alpha { get; }

	Color silentColor_purple { get; }
}
