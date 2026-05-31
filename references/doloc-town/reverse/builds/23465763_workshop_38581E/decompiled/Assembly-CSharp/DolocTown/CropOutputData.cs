using Newtonsoft.Json;

namespace DolocTown;

[JsonObject]
public struct CropOutputData
{
	[JsonProperty]
	public int originCount;

	[JsonProperty]
	public int finalCountAddition;

	[JsonProperty]
	public float finalCountMultiplication;

	[JsonConstructor]
	public CropOutputData(int originCount, int finalCountAddition = 0, float finalCountMultiplication = 0f)
	{
		this.originCount = originCount;
		this.finalCountAddition = finalCountAddition;
		this.finalCountMultiplication = finalCountMultiplication;
	}
}
