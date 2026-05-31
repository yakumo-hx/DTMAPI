using UnityEngine;

namespace DolocTown;

public abstract class Scanner<T> : IScanner where T : class
{
	protected T lastContent;

	public void OnPosChanged(Vector2Int pos)
	{
		T content = GetContent(pos);
		if (content != lastContent)
		{
			OnContentChanged(lastContent, content);
			lastContent = content;
		}
	}

	public abstract void OnRoomChanged(Room newRoom);

	protected abstract T GetContent(Vector2Int pos);

	public virtual void OnWorldPosChanged(Vector2 positionWS)
	{
	}

	protected virtual void OnContentChanged(T lastContent, T newContent)
	{
	}
}
