namespace DolocTown;

public interface ICropFunction
{
	int MoodContribution { get; }

	int MaxLifespan { get; }

	bool Regrow(bool shouldRender);

	bool GrowBack(bool shouldRender, bool shouldClearGrowth);

	bool GrowForward(bool shouldRender, bool shouldClearGrowth);

	void Water(bool shouldRender, bool invokeCallback);

	void Protected(bool shouldRender);

	void UpdateNormal(bool shouldRender, bool isMoist, float addition, float fertilizerAddition);

	void UpdateAcidRain(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage);

	void UpdateScorchSun(bool shouldRender, bool isMoist, float addition, float fertilizerAddition, bool isProtected, float damage);

	void Thunder(bool shouldRender);

	bool CheckGrowthMonth(bool shouldRender);

	void GenCropOutput(bool putInBackpack);

	bool TryGetNeedWaterOrClear(out bool value);
}
