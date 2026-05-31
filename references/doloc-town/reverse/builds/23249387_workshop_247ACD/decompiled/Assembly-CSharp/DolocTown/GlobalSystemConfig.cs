using DolocTown.GameServiceLocator;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown;

[CreateAssetMenu(menuName = "多洛可小镇/配置/系统配置")]
public class GlobalSystemConfig : SerializedScriptableObject, ISystemConfigProvider
{
	[SerializeField]
	private Color _backgroundColor_lv0;

	[SerializeField]
	private Color _backgroundColor_lv1;

	[SerializeField]
	private Color _backgroundColor_lv2;

	[SerializeField]
	private Color _backgroundColor_lv3;

	[SerializeField]
	private Color _backgroundColor_lv1_95alpha;

	[SerializeField]
	private Color _backgroundColor_lv1_78alpha;

	[SerializeField]
	private Color _silentColor_purple;

	public Color backgroundColor_lv0 => _backgroundColor_lv0;

	public Color backgroundColor_lv1 => _backgroundColor_lv1;

	public Color backgroundColor_lv2 => _backgroundColor_lv2;

	public Color backgroundColor_lv3 => _backgroundColor_lv3;

	public Color backgroundColor_lv1_95alpha => _backgroundColor_lv1_95alpha;

	public Color backgroundColor_lv1_78alpha => _backgroundColor_lv1_78alpha;

	public Color silentColor_purple => _silentColor_purple;
}
