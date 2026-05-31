namespace RedSaw;

public class LongPressTimer
{
	public float len;

	public float current;

	public float Progress => current / len;

	public bool HasProgress => current > 0f;

	public LongPressTimer(float len)
	{
		this.len = len;
		current = 0f;
	}

	public bool TickHold(float dt)
	{
		current += dt;
		if (current >= len)
		{
			current = 0f;
			return true;
		}
		return false;
	}

	public void TickRelease(float dt)
	{
		current -= dt;
		if (current < 0f)
		{
			current = 0f;
		}
	}
}
