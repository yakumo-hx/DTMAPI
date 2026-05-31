using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RedSaw.CommandLineInterface.UnityImpl;

public class GameConsoleAlternativeOptionsPanel : MonoBehaviour
{
	[SerializeField]
	private Text textPanel;

	[SerializeField]
	private Color selectedColor;

	private List<string> options;

	private string selectedColorHex;

	public int SelectionIndex
	{
		set
		{
			textPanel.text = string.Empty;
			if (options != null && options.Count != 0)
			{
				string text = string.Empty;
				for (int i = 0; i < options.Count; i++)
				{
					text = ((i != value) ? (text + options[i] + "\n") : (text + "<color=\"#" + selectedColorHex + "\">" + options[i] + "</color>\n"));
				}
				textPanel.text = text;
			}
		}
	}

	private void Start()
	{
		selectedColorHex = ColorUtility.ToHtmlStringRGB(selectedColor);
	}

	public void SetOptions(List<string> values)
	{
		options = values;
	}
}
