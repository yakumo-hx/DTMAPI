using System;
using DolocTown.Config;
using UnityEngine;

namespace DolocTown;

[Serializable]
public struct TextConfig
{
	[SerializeField]
	private string textId;

	public string Text
	{
		get
		{
			object obj;
			if (!textId.IsNullOrEmpty())
			{
				obj = DolocConfig.Tables.TbL10nText.GetOrDefault(textId ?? "")?.Text;
				if (obj == null)
				{
					return textId;
				}
			}
			else
			{
				obj = string.Empty;
			}
			return (string)obj;
		}
	}

	public bool isEmpty => Text.IsNullOrEmpty();

	public string Id => textId ?? string.Empty;

	public override string ToString()
	{
		return Text;
	}
}
