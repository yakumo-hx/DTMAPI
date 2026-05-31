using System.Collections.Generic;
using DolocTown.Config.Fishing;
using UnityEngine;

namespace DolocTown;

public class FishingNoteSpawner
{
	private FishInfo fishProto;

	private bool isStable;

	private List<FishingNoteData> notes = new List<FishingNoteData>();

	private float startTimeOffset;

	public readonly float speedMultiplier;

	public readonly float delayTime;

	private const int noteCountPerSpawn = 50;

	private const int noteBufferCount = 15;

	public FishingNoteSpawner(FishInfo fishProto)
	{
		this.fishProto = fishProto;
		isStable = fishProto.InitialStableProbability > Random.value;
		delayTime = DolocAPI.userSettings.fishingGameDelayTime;
		startTimeOffset = delayTime;
		speedMultiplier = fishProto.NoteSpeedMultiplier * DolocAPI.userSettings.fishingGameSpeedMultiplier;
		if (delayTime > 0f)
		{
			notes.Add(new FishingNoteData(0, FishingNoteType.Delay, 0f, delayTime, speedMultiplier));
		}
		SpawnNotesToTotal(50);
	}

	private void SpawnNotesToTotal(int totalCount)
	{
		while (notes.Count < totalCount)
		{
			SpawnNext();
		}
	}

	private void SpawnNext()
	{
		int count = notes.Count;
		float num = (isStable ? fishProto.StableDuration.RandomCount : fishProto.StruggleDuration.RandomCount);
		num *= DolocAPI.GlobalParameter.FishingOperationInterval;
		float num2 = startTimeOffset;
		float endTime = startTimeOffset + num;
		FishingNoteData fishingNoteData = null;
		if (!isStable)
		{
			if (fishProto.BonusProbability > Random.value)
			{
				float value = (float)fishProto.BonusDuration.RandomCount * DolocAPI.GlobalParameter.FishingOperationInterval;
				value = Mathf.Clamp(value, 0f, num);
				float num3 = num - value;
				if (num3 * 0.8f > value)
				{
					float num4 = Random.Range(0.1f * num3, num - value - 0.1f * num3);
					float num5 = num2 + num4;
					fishingNoteData = new FishingNoteData(count, FishingNoteType.Bonus, num5, num5 + value, speedMultiplier);
				}
			}
		}
		else
		{
			fishingNoteData = new FishingNoteData(count, FishingNoteType.Stable, startTimeOffset, endTime, speedMultiplier);
		}
		isStable = !isStable;
		startTimeOffset = endTime;
		if (fishingNoteData != null)
		{
			notes.Add(fishingNoteData);
		}
	}

	public FishingNoteData GetNote(int index)
	{
		index = Mathf.Max(index, 0);
		if (index >= notes.Count - 15)
		{
			SpawnNotesToTotal(notes.Count + 50);
		}
		return notes[index];
	}
}
