using System;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class PageInfo : DolocUiObject
{
	[SerializeField]
	private Text pageInfo;

	[SerializeField]
	public DolocNavigationButton leftButton;

	[SerializeField]
	public DolocNavigationButton rightButton;

	protected override void __Init()
	{
		base.__Init();
		leftButton.Init();
		rightButton.Init();
	}

	public void BindChangePageAction(Action prevPage, Action nextPage)
	{
		leftButton.onClick.AddListener(delegate
		{
			prevPage();
		});
		rightButton.onClick.AddListener(delegate
		{
			nextPage();
		});
	}

	public void SetPage(int current, int total)
	{
		pageInfo.text = ((total <= 0) ? string.Empty : $"- {current + 1}/{total} -");
		leftButton.gameObject.SetActive(total > 1);
		rightButton.gameObject.SetActive(total > 1);
	}

	public void FireClickLeft()
	{
		if (leftButton.gameObject.activeSelf)
		{
			leftButton.FireClick();
		}
	}

	public void FireClickRight()
	{
		if (rightButton.gameObject.activeSelf)
		{
			rightButton.FireClick();
		}
	}
}
