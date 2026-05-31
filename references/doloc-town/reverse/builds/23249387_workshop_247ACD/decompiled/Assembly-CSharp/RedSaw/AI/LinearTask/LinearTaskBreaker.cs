namespace RedSaw.AI.LinearTask;

public abstract class LinearTaskBreaker
{
	public static LinearTaskBreakerEmpty Empty = new LinearTaskBreakerEmpty();

	private LinearTaskBreaker nextBreaker;

	protected abstract bool ShouldBreak { get; }

	public bool Check()
	{
		if (ShouldBreak)
		{
			return true;
		}
		if (nextBreaker == null)
		{
			return false;
		}
		return nextBreaker.Check();
	}

	public LinearTaskBreaker LinkAfter(LinearTaskBreaker breaker)
	{
		nextBreaker = breaker;
		return breaker;
	}
}
