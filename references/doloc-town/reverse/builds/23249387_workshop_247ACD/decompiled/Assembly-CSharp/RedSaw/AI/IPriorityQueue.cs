namespace RedSaw.AI;

public interface IPriorityQueue<T>
{
	bool notEmpty { get; }

	T get();

	void set(T obj, int priority);

	void clear();
}
