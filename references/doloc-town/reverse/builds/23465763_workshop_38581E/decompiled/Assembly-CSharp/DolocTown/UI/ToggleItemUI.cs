using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ToggleItemUI : SettingValueItemUI<bool>
{
	[SerializeField]
	private DolocToggleComponent toggle;

	[SerializeField]
	private GameObject splitLine;

	private DolocToggleHelper toggleHelper;

	private string _title;

	public override bool currentValue => toggle.isOn;

	public override Selectable selectable => toggle;

	public override string title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			RefreshText();
		}
	}

	protected override void __Init()
	{
		base.__Init();
		toggleHelper = GetComponent<DolocToggleHelper>();
		toggleHelper.Init();
		toggle.onValueChanged.AddListener(OnToggleValueChanged);
		toggle.onMove.AddListener(delegate(MoveDirection dir)
		{
			switch (dir)
			{
			case MoveDirection.Left:
				toggle.isOn = false;
				break;
			case MoveDirection.Right:
				toggle.isOn = true;
				break;
			}
		});
	}

	private void OnToggleValueChanged(bool value)
	{
		toggleHelper.SetToggleState(value);
		onValueChanged.Invoke(value);
	}

	public void InitValue(bool value, bool showSplitLine)
	{
		toggle.SetIsOnWithoutNotify(value);
		toggleHelper.SetToggleState(value);
		splitLine.SetActive(showSplitLine);
	}

	public void FireClick(bool select = true)
	{
		toggle.FireClick(select);
	}

	public void RefreshText()
	{
		txtTitle.text = DolocAPI.GetParsedKeystrokeText(_title);
	}
}
