using RedSaw.Physical;
using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

[GameEntityManager("/global/drop_items", DolocGameAssets.GAME_ENTITY_DROPITEM)]
public class DropItemRenderer : WorldContentRenderer
{
	[SerializeField]
	private Color normalColor;

	[SerializeField]
	private SpriteRenderer subscriptRenderer;

	private Material defaultMaterial;

	private BounceSimulator simulator;

	private bool shouldUpdate;

	private MaterialPropertyBlock propertyBlock;

	public bool shieldCollector
	{
		get
		{
			if (base.WorldContent is DropItemBase dropItemBase)
			{
				return dropItemBase.shieldCollector;
			}
			return false;
		}
	}

	public Color BorderColor
	{
		set
		{
			GetComponent<SpriteRenderer>().color = value;
		}
	}

	public override bool OnlyTouch => true;

	protected override void __Init()
	{
		base.__Init();
		simulator = default(BounceSimulator);
		BorderColor = normalColor;
		defaultMaterial = Sr.sharedMaterial;
		subscriptRenderer.gameObject.SetActive(value: false);
	}

	public override void SetSprite(Sprite itemSprite)
	{
		base.SetSprite(itemSprite);
		if (!(itemSprite == null))
		{
			Vector4 outerUV = DataUtility.GetOuterUV(itemSprite);
			if (propertyBlock == null)
			{
				propertyBlock = new MaterialPropertyBlock();
			}
			Sr.GetPropertyBlock(propertyBlock);
			propertyBlock.SetVector("_MainTex_UVInfos", outerUV);
			Sr.SetPropertyBlock(propertyBlock);
			subscriptRenderer.gameObject.SetActive(value: false);
		}
	}

	public void SetSprite(Sprite itemSprite, Sprite subscriptSprite)
	{
		SetSprite(itemSprite);
		if (subscriptSprite != null)
		{
			subscriptRenderer.sprite = subscriptSprite;
			subscriptRenderer.gameObject.SetActive(value: true);
		}
	}

	public void Raise(Vector2 start, Vector2 target, bool useBounce = true)
	{
		Col2d.enabled = false;
		base.transform.position = start;
		simulator.InitSimulator(start, target, new Vector2(0f, DolocAPI.eftConfig.dropItemGravityScale), DolocAPI.eftConfig.dropSpringRate, DolocAPI.eftConfig.dropSpringSpeed, DolocAPI.eftConfig.dropItemRaiseYForce, useBounce);
		Vector3 vector = base.transform.position;
		base.transform.position = new Vector3(vector.x, vector.y, DolocAPI.eftConfig.dropItemZRange);
		shouldUpdate = true;
	}

	public void Parabola(Vector2 start, Vector2 target, bool useBounce = true)
	{
		Col2d.enabled = false;
		base.transform.position = start;
		simulator.InitSimulator(start, target, new Vector2(0f, DolocAPI.eftConfig.dropItemGravityScale), DolocAPI.eftConfig.dropSpringRate, DolocAPI.eftConfig.dropSpringSpeed, 0f, useBounce);
		Vector3 vector = base.transform.position;
		base.transform.position = new Vector3(vector.x, vector.y, DolocAPI.eftConfig.dropItemZRange);
		shouldUpdate = true;
	}

	private void FixedUpdate()
	{
		if (shouldUpdate)
		{
			Vector2 vector = simulator.OnUpdate(Time.fixedDeltaTime);
			base.transform.position = new Vector3(vector.x, vector.y, base.transform.position.z);
			if (!simulator.ShouldUpdate)
			{
				shouldUpdate = false;
				Col2d.enabled = true;
			}
		}
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		shouldUpdate = false;
		Col2d.enabled = true;
		BorderColor = normalColor;
		Sr.sharedMaterial = defaultMaterial;
		subscriptRenderer.gameObject.SetActive(value: false);
	}

	public void SetShieldCollector(bool shield)
	{
		if (base.WorldContent is DropItemBase dropItemBase)
		{
			dropItemBase.SetShieldCollector(shield);
		}
	}
}
