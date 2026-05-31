using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public class FloatingObjectPreset : MonoBehaviour
{
	[SerializeField]
	private Sprite sprite;

	[SerializeField]
	private Material material;

	[SerializeField]
	private FloatType floatType;

	[SerializeField]
	private FloatingParamSO floatingParam;

	private FloatingObjectBase _floatingObjectBase;

	public FloatType FloatType => floatType;

	public void InitFloatingObject(WaterController controller)
	{
		switch (floatType)
		{
		case FloatType.SIMPLE:
			_floatingObjectBase = DolocAPI.EntitySystem.Next<FloatingObjectSimple>();
			break;
		case FloatType.DEFAULT:
			_floatingObjectBase = DolocAPI.EntitySystem.Next<FloatingObject>();
			break;
		}
		_floatingObjectBase._controller = controller;
		_floatingObjectBase.FloatingParam = floatingParam;
		_floatingObjectBase.Sprite = sprite;
		_floatingObjectBase.Material = ((material == null) ? LocMaterials.GAME_MAT_2D : material);
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		if (component != null)
		{
			_floatingObjectBase.SpriteRenderer.sortingLayerID = component.sortingLayerID;
			_floatingObjectBase.SpriteRenderer.sortingOrder = component.sortingOrder;
		}
		_floatingObjectBase.transform.position = base.transform.position;
		_floatingObjectBase.ResetFloatObject();
	}

	private void OnSpriteChanged()
	{
		base.gameObject.name = sprite?.name ?? "未知物体";
		SpriteRenderer component = GetComponent<SpriteRenderer>();
		component.sprite = sprite;
		component.color = new Color(1f, 1f, 1f, 0.6f);
	}
}
