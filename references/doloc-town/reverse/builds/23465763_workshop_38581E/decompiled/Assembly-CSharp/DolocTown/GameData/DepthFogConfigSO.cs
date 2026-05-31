using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(menuName = "多洛可小镇/天气系统/深度雾效果配置")]
public class DepthFogConfigSO : SerializedScriptableObject
{
	[SerializeField]
	private Color colorSky;

	[SerializeField]
	private Color colorRemote;

	[SerializeField]
	private Color colorFront01;

	[SerializeField]
	private Color colorFront02;

	public Color ColorSky => colorSky;

	public Color ColorRemote => colorRemote;

	public Color ColorFront01 => colorFront01;

	public Color ColorFront02 => colorFront02;

	public Color GetColor(int index)
	{
		return index switch
		{
			0 => colorSky, 
			1 => colorRemote, 
			2 => colorFront01, 
			3 => colorFront02, 
			_ => Color.white, 
		};
	}

	public void SetColor(int index, Color color)
	{
		switch (index)
		{
		case 0:
			colorSky = color;
			break;
		case 1:
			colorRemote = color;
			break;
		case 2:
			colorFront01 = color;
			break;
		case 3:
			colorFront02 = color;
			break;
		}
	}
}
