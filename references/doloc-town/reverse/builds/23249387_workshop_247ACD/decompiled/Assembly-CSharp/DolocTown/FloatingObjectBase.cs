using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class FloatingObjectBase : GameEntity
{
	[SerializeField]
	public WaterController _controller;

	[SerializeField]
	protected FloatingParamSO floatingParam;

	private SpriteRenderer Sp;

	protected float springConst => floatingParam.springConst;

	protected float dropVelocity => floatingParam.dropVelocity;

	protected float floatVelocity => floatingParam.floatVelocity;

	protected float damping => floatingParam.damping;

	protected float objectWidth => floatingParam.objectWidth;

	protected int springCount => floatingParam.forcePointCount;

	public SpriteRenderer SpriteRenderer => Sp;

	public Sprite Sprite
	{
		set
		{
			Sp.sprite = value;
		}
	}

	public Material Material
	{
		set
		{
			Sp.sharedMaterial = value;
		}
	}

	public FloatingParamSO FloatingParam
	{
		set
		{
			floatingParam = value;
		}
	}

	protected override void __Init()
	{
		base.__Init();
		Sp = GetComponent<SpriteRenderer>();
	}

	public abstract void ResetFloatObject();

	public void LoadWaterControllerFromScene()
	{
		if (DolocAPI.CurrentRoom == null)
		{
			return;
		}
		WaterController[] componentsInScene = DolocAPI.CurrentRoom.SceneHandle.GetComponentsInScene<WaterController>(includeInactive: true);
		if (componentsInScene.Length == 0)
		{
			Debug.LogError("<color=red>没有找到任何水体..</color>");
			return;
		}
		if (componentsInScene.Length == 1)
		{
			_controller = componentsInScene[0];
			return;
		}
		float num = float.MaxValue;
		WaterController controller = null;
		WaterController[] array = componentsInScene;
		foreach (WaterController waterController in array)
		{
			float num2 = Vector2.Distance(base.transform.position, waterController.transform.position);
			if (num2 < num)
			{
				num = num2;
				controller = waterController;
			}
		}
		_controller = controller;
	}
}
