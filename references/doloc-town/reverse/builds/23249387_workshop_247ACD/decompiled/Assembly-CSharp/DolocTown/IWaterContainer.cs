namespace DolocTown;

public interface IWaterContainer
{
	int Water { get; }

	int TakeWater(int require);

	void Evaporation(int value, bool shouldRender = false);
}
