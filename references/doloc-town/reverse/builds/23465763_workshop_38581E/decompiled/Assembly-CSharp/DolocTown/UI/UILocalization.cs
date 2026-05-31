using System;
using DolocTown.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class UILocalization : MonoBehaviour
{
	public string KeyStr;

	public bool OverflowIsEllipsis;

	private Text _text;

	private TextMeshProUGUI _textMeshProUgui;

	public string ValueStr => DolocConfig.GetL10nText(KeyStr);

	public void SetL10nKey(string str)
	{
		KeyStr = str;
		Refresh();
	}

	private void Awake()
	{
		_text = GetComponent<Text>();
		_textMeshProUgui = GetComponent<TextMeshProUGUI>();
		Tables.LanguageChange = (Action)Delegate.Combine(Tables.LanguageChange, new Action(Refresh));
	}

	private void Start()
	{
		if (KeyStr.IsNullOrEmpty())
		{
			KeyStr = base.name;
		}
		Refresh();
	}

	private void OnDestroy()
	{
		if (DolocConfig.Tables != null)
		{
			Tables.LanguageChange = (Action)Delegate.Remove(Tables.LanguageChange, new Action(Refresh));
		}
	}

	public void Refresh()
	{
		string l10nText = DolocConfig.GetL10nText(KeyStr);
		if (_text != null)
		{
			if (OverflowIsEllipsis)
			{
				_text.SetTextWithEllipsis(l10nText);
			}
			else
			{
				_text.text = l10nText;
			}
		}
		if (_textMeshProUgui != null)
		{
			_textMeshProUgui.text = l10nText;
		}
	}
}
