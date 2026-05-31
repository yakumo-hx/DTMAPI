namespace XLua.Cast;

public class Any<T> : RawObject
{
	private T mTarget;

	public object Target => mTarget;

	public Any(T i)
	{
		mTarget = i;
	}
}
