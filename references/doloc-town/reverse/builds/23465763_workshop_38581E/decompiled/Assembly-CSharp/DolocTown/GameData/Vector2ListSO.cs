using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[CreateAssetMenu(menuName = "多洛可小镇/其他/Vector2列表")]
public class Vector2ListSO : SerializedScriptableObject
{
	[SerializeField]
	private Vector2[] _list;

	public Vector2 this[int index] => _list[index];

	public int Length => _list.Length;

	public bool valid
	{
		get
		{
			if (_list != null)
			{
				return _list.Length != 0;
			}
			return false;
		}
	}

	public bool invalid
	{
		get
		{
			if (_list != null)
			{
				return _list.Length == 0;
			}
			return true;
		}
	}
}
