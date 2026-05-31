using UnityEngine.Events;

namespace DolocTown.UI;

public interface ISettingUiItemWithValue<T> : ISettingUiItem
{
	T currentValue { get; }

	UnityEvent<T> onValueChanged { get; }
}
