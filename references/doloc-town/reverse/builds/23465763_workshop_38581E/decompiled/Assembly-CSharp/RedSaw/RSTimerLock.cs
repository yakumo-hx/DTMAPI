using Newtonsoft.Json;

namespace RedSaw;

[JsonObject(MemberSerialization.OptIn)]
public class RSTimerLock
{
	[JsonProperty]
	private readonly RSTimer timer;

	[JsonProperty]
	private bool locked;

	public bool IsLocked => locked;

	public RSTimerLock(float interval = 1f)
	{
		timer = new RSTimer(interval);
		locked = false;
	}

	[JsonConstructor]
	public RSTimerLock(RSTimer timer, bool locked)
	{
		this.timer = timer;
		this.locked = locked;
	}

	public void Lock()
	{
		locked = true;
		timer.Reset();
	}

	public void Unlock()
	{
		locked = false;
		timer.Reset();
	}

	public void SetInterval(float interval)
	{
		timer.SetInterval(interval);
	}

	public void LockWithNewInterval(float interval)
	{
		locked = true;
		timer.SetInterval(interval);
	}

	public bool Tick(float dt)
	{
		if (!locked)
		{
			return false;
		}
		if (!timer.Tick(dt))
		{
			return false;
		}
		locked = false;
		return true;
	}
}
