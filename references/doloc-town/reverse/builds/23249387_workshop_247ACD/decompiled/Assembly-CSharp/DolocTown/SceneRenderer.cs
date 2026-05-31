using UnityEngine;

namespace DolocTown;

public abstract class SceneRenderer
{
	private readonly Transform container;

	private bool _neat;

	public bool Visible
	{
		get
		{
			return container.gameObject.activeSelf;
		}
		set
		{
			if (value != Visible)
			{
				container.gameObject.SetActive(value);
			}
		}
	}

	public bool IsDirty => !_neat;

	protected SceneRenderer(Transform container)
	{
		this.container = container;
		_neat = true;
		Visible = false;
	}

	public void SetDirty()
	{
		_neat = false;
	}

	public void Clear()
	{
		if (!_neat)
		{
			_neat = true;
			ClearRenderers();
		}
	}

	protected abstract void ClearRenderers();

	protected Transform GetContainer(string name)
	{
		Transform transform = container.Find(name);
		if (transform != null)
		{
			return transform;
		}
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(container);
		return gameObject.transform;
	}

	public void SetVisible(bool value)
	{
		if (value != Visible)
		{
			Visible = value;
		}
	}
}
