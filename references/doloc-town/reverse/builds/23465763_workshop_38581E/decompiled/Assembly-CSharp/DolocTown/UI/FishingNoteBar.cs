using System;
using System.Collections;
using RedSaw;
using UnityEngine;

namespace DolocTown.UI;

public class FishingNoteBar : DolocUiObject
{
	[SerializeField]
	private RectTransform noteRoot;

	[SerializeField]
	private Sprite stableSprite;

	[SerializeField]
	private Sprite bonusSprite;

	[SerializeField]
	private Sprite delaySprite;

	[SerializeField]
	private Animator holdEffectAnimator;

	[SerializeField]
	private Animator punishEffectAnimator;

	private ObjectPool<FishingBehaviorNote> notePool;

	private Coroutine spawnNotesCoroutine;

	private Coroutine readyTipCoroutine;

	private FishingNoteSpawner noteSpawner;

	private float presetTime;

	private bool isHoldEffectVisible;

	private bool isPunishEffectVisible;

	protected override void __Init()
	{
		base.__Init();
		GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.UI_ELEMENT_FISHING_NOTE);
		notePool = new ObjectPool<FishingBehaviorNote>(asset, noteRoot, usePreset: true);
		RecycleAllNotes();
		SetHoldEffectVisible(value: false, force: true);
		SetPunishEffectVisible(value: false, force: true);
	}

	public void StartSpawn(FishingNoteSpawner noteSpawner)
	{
		RecycleAllNotes();
		if (noteSpawner != null)
		{
			this.noteSpawner = noteSpawner;
			StopSpawn();
			float num = (float)DolocAPI.GlobalParameter.FishingNoteScrollSpeedBase * noteSpawner.speedMultiplier;
			presetTime = base.width / (num * 4f);
			DolocAPI.DelayFrame(delegate
			{
				spawnNotesCoroutine = StartCoroutine(SpawnNotes());
			});
		}
	}

	private IEnumerator SpawnNotes()
	{
		int noteIndex = 0;
		float startTime = Time.time;
		while (true)
		{
			float num = Time.time - startTime + presetTime;
			FishingNoteData data = noteSpawner.GetNote(noteIndex++);
			if (data != null)
			{
				if (num < data.startTime)
				{
					yield return new WaitForSeconds(data.startTime - num);
				}
				FishingBehaviorNote next = notePool.Next;
				float num2 = data.endTime - data.startTime;
				next.width = data.widthMultiplier * num2;
				switch (data.noteType)
				{
				case FishingNoteType.Delay:
					next.width += 4f;
					next.transform.SetAsLastSibling();
					next.sprite = delaySprite;
					break;
				case FishingNoteType.Stable:
					next.transform.SetAsFirstSibling();
					next.sprite = stableSprite;
					break;
				case FishingNoteType.Bonus:
					next.transform.SetAsLastSibling();
					next.sprite = bonusSprite;
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
				next.anchoredPositionX = data.scrollSpeed * Mathf.Min(presetTime, data.startTime);
				next.StartScroll(data.scrollSpeed, notePool);
			}
		}
	}

	public void StopSpawn()
	{
		if (spawnNotesCoroutine != null)
		{
			StopCoroutine(spawnNotesCoroutine);
		}
		SetHoldEffectVisible(value: false, force: true);
		SetPunishEffectVisible(value: false, force: true);
	}

	public void RecycleAllNotes()
	{
		notePool.RecycleAll();
	}

	public void SetHoldEffectVisible(bool value, bool force = false)
	{
		if (isHoldEffectVisible != value || force)
		{
			isHoldEffectVisible = value;
			holdEffectAnimator.gameObject.SetActive(value);
			if (value)
			{
				holdEffectAnimator.Play("HoldEffect", 0, 0f);
			}
		}
	}

	public void SetPunishEffectVisible(bool value, bool force = false)
	{
		if (isPunishEffectVisible != value || force)
		{
			isPunishEffectVisible = value;
			punishEffectAnimator.gameObject.SetActive(value);
			if (value)
			{
				punishEffectAnimator.Play("PunishEffect", 0, 0f);
			}
		}
	}
}
