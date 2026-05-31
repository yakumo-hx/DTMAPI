using System;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
[CreateAssetMenu(menuName = "多洛可小镇/背景(新)", fileName = "Background")]
public class EnvBackgroundSO : SerializedScriptableObject
{
	[SerializeField]
	private BackgroundLayer skyLayer;

	[SerializeField]
	private BackgroundLayer[] layers = new BackgroundLayer[0];

	public BackgroundLayer[] Layers => layers.Where((BackgroundLayer x) => x.sprite != null).ToArray();

	public BackgroundLayer Sky => skyLayer;
}
