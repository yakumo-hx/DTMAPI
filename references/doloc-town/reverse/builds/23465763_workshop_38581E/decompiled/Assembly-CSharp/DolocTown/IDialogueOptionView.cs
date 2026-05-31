using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public interface IDialogueOptionView : IView
{
	bool InRender { get; }

	bool InAnimation { get; }

	void SetClickCallbacks(UnityAction<int> callback);

	void Render(OptionGroupData groupData);

	void Select(int index);

	void SelectByDataIndex(int dataIndex);

	bool SelectThenFireClickExitOption();

	void Pause();

	void Resume();
}
