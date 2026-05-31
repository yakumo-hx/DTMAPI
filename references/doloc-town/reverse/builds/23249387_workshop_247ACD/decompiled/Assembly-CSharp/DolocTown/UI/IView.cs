using System;
using UnityEngine.Events;

namespace DolocTown.UI;

public interface IView
{
	UnityEvent OnCloseButtonClick { get; }

	void Show(bool useTween = true, Action call = null);

	void Hide(bool useTween = true, Action call = null);
}
