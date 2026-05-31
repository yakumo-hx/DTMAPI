using System;

namespace DolocTown;

public class ApcManager
{
	private event Action<float> OnUpdateEvent = delegate
	{
	};

	private event Action<float> OnFixedUpdateEvent = delegate
	{
	};

	private event Action OnPauseEvent = delegate
	{
	};

	private event Action OnResumeEvent = delegate
	{
	};

	public void Add(IApc entity)
	{
		Remove(entity);
		OnUpdateEvent += entity.OnUpdate;
		OnFixedUpdateEvent += entity.OnFixedUpdate;
		OnPauseEvent += entity.OnPause;
		OnResumeEvent += entity.OnResume;
	}

	public void Remove(IApc apc)
	{
		OnUpdateEvent -= apc.OnUpdate;
		OnFixedUpdateEvent -= apc.OnFixedUpdate;
		OnPauseEvent -= apc.OnPause;
		OnResumeEvent -= apc.OnResume;
	}

	public void Clear()
	{
		this.OnUpdateEvent = delegate
		{
		};
		this.OnFixedUpdateEvent = delegate
		{
		};
		this.OnPauseEvent = delegate
		{
		};
		this.OnResumeEvent = delegate
		{
		};
	}

	public void OnUpdate(float dt)
	{
		this.OnUpdateEvent(dt);
	}

	public void OnFixedUpdate(float dt)
	{
		this.OnFixedUpdateEvent(dt);
	}

	public void OnPause()
	{
		this.OnPauseEvent();
	}

	public void OnResume()
	{
		this.OnResumeEvent();
	}
}
