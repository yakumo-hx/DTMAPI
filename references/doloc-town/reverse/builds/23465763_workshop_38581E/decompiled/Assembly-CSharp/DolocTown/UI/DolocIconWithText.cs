using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DolocIconWithText : DolocIcon
{
	[SerializeField]
	private Text text;

	public string description
	{
		set
		{
			text.text = value;
		}
	}
}
