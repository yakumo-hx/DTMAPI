using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class EnvOptimizerPanel : DolocUIPanel
{
	[SerializeField]
	private Text txtTitle;

	[SerializeField]
	private Text txtOverview;

	[SerializeField]
	public EnvOptimizerConfirmButton btnConfirm;

	[SerializeField]
	private EnvOptimizerConsole console;

	[SerializeField]
	private ProgressBar totalProgressBar;

	[SerializeField]
	private RectTransform stepScoreRoot;

	[SerializeField]
	private Text[] stepScores;

	[SerializeField]
	private Image[] stepSplits;

	[SerializeField]
	private RectTransform itemRoot;

	[SerializeField]
	private RectTransform shieldRaycastMask;

	[SerializeField]
	private RectTransform branchRoot;

	private ProgressBar[] branchProgresses;

	private Coroutine coroutine;

	private EnvOptimizerData currentData;

	public EnvOptimizerItemSlot[] itemSlots { get; private set; }

	public IScrollContentRect contentRect => console;

	public bool inActiveAnimation => coroutine != null;

	protected override void __Init()
	{
		base.__Init();
		shieldRaycastMask.gameObject.SetActive(value: false);
		console.Init();
		btnConfirm.Init();
		btnConfirm.onSelect.AddListener(delegate
		{
			if (base.isRender)
			{
				btnConfirm.GetItemBorder(BorderType.Arrow);
				DolocAPI.UIRaiseRoll();
			}
		});
		totalProgressBar.Init();
		branchProgresses = branchRoot.GetComponentsInChildren<ProgressBar>();
		for (int i = 0; i < branchProgresses.Length; i++)
		{
			ProgressBar bar = branchProgresses[i];
			bar.Init();
			bar.index = i;
			bar.onPointerEnter.AddListener(delegate
			{
				ShowBranchHoverBox(bar, isSelect: false);
			});
			bar.onPointerExit.AddListener(delegate
			{
				HideHoverBox(bar, isSelect: false);
			});
			bar.onSelect.AddListener(delegate
			{
				ShowBranchHoverBox(bar, isSelect: true);
			});
			bar.onDeselect.AddListener(delegate
			{
				HideHoverBox(bar, isSelect: true);
			});
		}
		itemSlots = itemRoot.GetComponentsInChildren<EnvOptimizerItemSlot>();
		for (int j = 0; j < itemSlots.Length; j++)
		{
			EnvOptimizerItemSlot slot = itemSlots[j];
			slot.Init();
			slot.index = j;
			slot.onSelect.AddListener(delegate
			{
				if (base.isRender)
				{
					slot.GetItemBorder(BorderType.Arrow);
					DolocAPI.UIRaiseRoll();
				}
			});
		}
		stepScores = stepScoreRoot.GetComponentsInChildren<Text>();
		stepSplits = stepScoreRoot.GetComponentsInChildren<Image>();
	}

	private void ShowBranchHoverBox(ProgressBar bar, bool isSelect)
	{
		int index = bar.index;
		if (base.isRender && currentData.notEmpty && index <= currentData.branches.Length - 1 && index >= 0)
		{
			EnvOptimizerBranchData envOptimizerBranchData = currentData.branches[index];
			bar.HoverText(new TextGroup(envOptimizerBranchData.description, "", envOptimizerBranchData.comment));
			if (isSelect)
			{
				bar.GetItemBorder(BorderType.Arrow);
			}
		}
	}

	private void HideHoverBox(ProgressBar bar, bool isSelect)
	{
		bar.HideHoverBox();
		if (isSelect)
		{
			bar.HideItemBorder();
		}
	}

	private void BuildNavigation()
	{
		List<Selectable> list = ((IEnumerable<EnvOptimizerItemSlot>)itemSlots).Select((Func<EnvOptimizerItemSlot, Selectable>)((EnvOptimizerItemSlot x) => x.button)).ToList();
		list.AddRange(((IEnumerable<ProgressBar>)branchProgresses).Select((Func<ProgressBar, Selectable>)((ProgressBar x) => x.button)));
		if (btnConfirm.gameObject.activeSelf)
		{
			list.Add(btnConfirm.button);
		}
		Selectable[] array = list.ToArray();
		array.RebuildNavigationHorizontal(array);
		array.RebuildNavigationVertical(array, 1f, 90f, wrapAround: false);
	}

	public void Render(EnvOptimizerData data)
	{
		currentData = data;
		SetText(txtTitle, data.title);
		SetText(txtOverview, data.overviewText);
		RefreshProgressBar();
		for (int i = 0; i < data.branches.Length && i < branchProgresses.Length; i++)
		{
			ProgressBar obj = branchProgresses[i];
			EnvOptimizerBranchData envOptimizerBranchData = data.branches[i];
			obj.SetTitle(envOptimizerBranchData.title);
			obj.SetProgress(envOptimizerBranchData.progress, envOptimizerBranchData.progressText);
		}
		RenderItemSlotOnly(data);
		console.Render(data);
		btnConfirm.SetText(data.buttonText);
		btnConfirm.highLighted = data.canActive;
	}

	private void RefreshProgressBar()
	{
		if (!currentData.notEmpty)
		{
			return;
		}
		RebuildLayout();
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < currentData.stepScores.Length; i++)
		{
			int num3 = currentData.stepScores[i];
			stepScores[i].text = num3.ToString();
			if (num3 < currentData.overviewScore)
			{
				num = num3;
				num2 = i + 1;
			}
		}
		float num4 = GetStepSlotWidth(num2);
		int num5 = currentData.stepScores[num2];
		int num6 = currentData.overviewScore - num;
		float num7 = 0f;
		if (num2 > 0)
		{
			num7 = GetStepSlotWidth(num2 - 1);
			num4 -= GetStepSlotWidth(num2 - 1);
			num5 -= currentData.stepScores[num2 - 1];
		}
		float progress = 1f;
		if (num5 > 0)
		{
			float num8 = (float)num6 / (float)num5;
			float num9 = num7 + num4 * num8;
			if (num9 > 0f)
			{
				num9 += 4f;
			}
			progress = num9 / stepScoreRoot.sizeDelta.x;
		}
		totalProgressBar.SetProgress(progress);
		float GetStepSlotWidth(int index)
		{
			if (index < 0 || index >= stepSplits.Length)
			{
				return 0f;
			}
			return stepSplits[index].transform.parent.localPosition.x;
		}
	}

	public void RenderItemSlotOnly(EnvOptimizerData data)
	{
		for (int i = 0; i < data.items.Length && i < itemSlots.Length; i++)
		{
			itemSlots[i].Render(data.items[i]);
		}
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		RebuildLayout();
		DolocAPI.DelayFrame(BuildNavigation);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		StopActiveAnim();
	}

	public EnvOptimizerItemSlot GetItemSlot(int index)
	{
		if (index < 0 || index >= itemSlots.Length)
		{
			return null;
		}
		return itemSlots[index];
	}

	public void Select(int index)
	{
		EnvOptimizerItemSlot itemSlot = GetItemSlot(index);
		if (!(itemSlot == null))
		{
			itemSlot.Select();
		}
	}

	public void PlayActiveAnim(int activeIndex, EnvOptimizerConsoleBlockData data, Action onEnd)
	{
		if (activeIndex < 0 || activeIndex >= itemSlots.Length)
		{
			Debug.LogError($"不合法的index: {activeIndex}");
			return;
		}
		EnvOptimizerItemSlot itemSlot = itemSlots[activeIndex];
		coroutine = StartCoroutine(PlayAnim(itemSlot, data, onEnd));
	}

	private IEnumerator PlayAnim(EnvOptimizerItemSlot itemSlot, EnvOptimizerConsoleBlockData data, Action onEnd)
	{
		EventSystem.current?.SetSelectedGameObject(null);
		DolocAPI.HideItemBorder();
		shieldRaycastMask.gameObject.SetActive(value: true);
		btnConfirm.highLighted = false;
		btnConfirm.SetProgress(0f, setPercentage: false);
		string[] dots = new string[4] { "", ".", "..", "..." };
		string[] splits = new string[4] { " —", "  \\", "  |", "  /" };
		for (int j = 0; j < 8; j++)
		{
			btnConfirm.SetText(base.staticTexts.UiEnvOptimizerInRecognition, dots[j % dots.Length]);
			yield return new WaitForSeconds(0.2f);
		}
		yield return new WaitForSeconds(0.5f);
		btnConfirm.SetText(base.staticTexts.UiEnvOptimizerLoadingData);
		int k = 0;
		for (int j = 0; j < 50; j++)
		{
			float num = (float)j / 50f;
			itemSlot.SetBorderProgress(num);
			if (j % 3 == 0)
			{
				btnConfirm.SetSubText(splits[k++ % splits.Length]);
			}
			btnConfirm.SetProgress(num, setPercentage: true);
			yield return new WaitForSeconds(0.04f);
		}
		btnConfirm.highLighted = true;
		btnConfirm.SetProgress(1f, setPercentage: true);
		btnConfirm.SetText(base.staticTexts.UiEnvOptimizerActiveSuccess);
		bool wait = true;
		console.AppendContent(data, delegate
		{
			wait = false;
		});
		yield return new WaitUntil(() => !wait);
		yield return new WaitForSeconds(1.5f);
		coroutine = null;
		shieldRaycastMask.gameObject.SetActive(value: false);
		onEnd?.Invoke();
	}

	private void StopActiveAnim()
	{
		shieldRaycastMask.gameObject.SetActive(value: false);
		if (coroutine != null)
		{
			StopCoroutine(coroutine);
			coroutine = null;
		}
	}
}
