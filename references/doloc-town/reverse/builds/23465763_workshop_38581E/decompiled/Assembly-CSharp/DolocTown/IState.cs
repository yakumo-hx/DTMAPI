namespace DolocTown;

public interface IState
{
	void OnInit();

	void OnEnter();

	void OnExit();
}
