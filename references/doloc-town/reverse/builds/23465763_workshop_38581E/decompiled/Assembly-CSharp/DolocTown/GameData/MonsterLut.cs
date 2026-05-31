namespace DolocTown.GameData;

public struct MonsterLut
{
	public readonly string name;

	public readonly string[] names;

	public readonly int[] probabilities;

	public readonly float[] probabilitiesf;

	public bool IsLutInvalid
	{
		get
		{
			if (names == null || probabilities == null)
			{
				return true;
			}
			if (names.Length == 0)
			{
				return true;
			}
			if (names.Length != probabilities.Length)
			{
				return true;
			}
			return false;
		}
	}

	public bool IsLutValid
	{
		get
		{
			if (names == null || probabilities == null)
			{
				return false;
			}
			if (names.Length == 0)
			{
				return false;
			}
			if (names.Length != probabilities.Length)
			{
				return false;
			}
			return true;
		}
	}

	public MonsterLut(string name, string[] monsterNames, int[] monsterProbabilities)
	{
		this.name = name;
		names = monsterNames;
		probabilities = monsterProbabilities;
		probabilitiesf = new float[monsterProbabilities.Length];
		float num = 0f;
		for (int i = 0; i < monsterProbabilities.Length; i++)
		{
			num += (float)monsterProbabilities[i];
		}
		for (int j = 0; j < monsterProbabilities.Length; j++)
		{
			probabilitiesf[j] = (float)monsterProbabilities[j] / num;
		}
	}
}
