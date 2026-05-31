using System;
using DolocTown.Config;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class UILocalizationImage : MonoBehaviour
{
	public string KeyStr;

	private Image _image;

	public void SetL10nKey(string str)
	{
		KeyStr = str;
		Refresh();
	}

	private void Awake()
	{
		_image = GetComponent<Image>();
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
		if (_image != null)
		{
			_image.overrideSprite = _image.LoadLangSprite(KeyStr);
		}
	}
}
