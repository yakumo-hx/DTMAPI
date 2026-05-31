using DolocTown.UI;
using UnityEngine;

namespace DolocTown;

public interface IDialogueLineView : IView
{
	bool InRender { get; }

	bool InAnimation { get; }

	bool IsPlaying { get; }

	void SetLineViewPosition(Vector2 worldPosition);

	void WaitOption();

	void Render(string npcName, string content);

	void Skip();

	void Pause();

	void Resume();
}
