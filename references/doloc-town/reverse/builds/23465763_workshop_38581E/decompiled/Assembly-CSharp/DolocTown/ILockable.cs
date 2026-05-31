namespace DolocTown;

public interface ILockable
{
	string lockObjectId { get; }

	void SetLockState(bool value);
}
