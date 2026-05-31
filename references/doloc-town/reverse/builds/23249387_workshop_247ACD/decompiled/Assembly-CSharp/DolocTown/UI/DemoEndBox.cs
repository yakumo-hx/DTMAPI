using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
public class DemoEndBox : AutoSizeUIPanel
{
	[SerializeField]
	public LinkButton buttonWishlist;

	[SerializeField]
	public LinkButton buttonSurvey;

	private bool isBtnSurveySelected;

	protected override void __Init()
	{
		base.__Init();
		buttonSurvey.Init();
		buttonWishlist.Init();
		buttonSurvey.onSelect.AddListener(delegate
		{
			isBtnSurveySelected = true;
		});
		buttonSurvey.onDeselect.AddListener(delegate
		{
			isBtnSurveySelected = false;
		});
		buttonWishlist.onSelect.AddListener(delegate
		{
			isBtnSurveySelected = false;
		});
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		isBtnSurveySelected = false;
	}

	public bool CanCancel()
	{
		if (!isBtnSurveySelected)
		{
			buttonSurvey.Select();
			return false;
		}
		return true;
	}
}
