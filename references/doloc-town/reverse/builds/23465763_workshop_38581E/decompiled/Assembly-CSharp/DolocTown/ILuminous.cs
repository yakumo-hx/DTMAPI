namespace DolocTown;

public interface ILuminous
{
	void OnLightParamChanged(bool isInitial = false);

	void TurnOn(bool isInitial);

	void TurnOff(bool isInitial);
}
