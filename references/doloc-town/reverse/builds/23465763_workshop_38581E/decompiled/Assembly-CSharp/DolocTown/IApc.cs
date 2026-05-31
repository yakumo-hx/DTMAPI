namespace DolocTown;

public interface IApc
{
	void InitApc(BattleSystem battleSystem, MonsterEnv env);

	void OnUpdate(float dt);

	void OnFixedUpdate(float dt);

	void OnPause();

	void OnResume();
}
