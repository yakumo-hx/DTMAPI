using DG.Tweening;
using UnityEngine;
using UnityEngine.Sprites;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
[GameEntityManager("/farm/crop", DolocGameAssets.GAME_ENTITY_CROP_FORAGE_GRASS)]
public class ForageGrassRenderer : GameEntity, IWindInteractive, IFellable, IAttackable
{
	private MaterialPropertyBlock propertyBlock;

	public ForageGrass Crop { get; set; }

	protected SpriteRenderer SR { get; private set; }

	public Animator Animator { get; private set; }

	public BoxCollider2D Collider2D { get; private set; }

	public Sprite Sprite
	{
		set
		{
			if (!(SR == null))
			{
				if (value == null)
				{
					SR.sprite = null;
				}
				else if (!(SR.sprite == value))
				{
					SR.sprite = value;
					ResizeColliderToFitSprite(Collider2D, SR);
				}
			}
		}
	}

	public bool ShouldCostEnergy => false;

	public bool ShouldCostChopCounter => false;

	public AttackableType attackableType => AttackableType.None;

	protected override void __Init()
	{
		base.__Init();
		SR = GetComponent<SpriteRenderer>();
		Animator = GetComponent<Animator>();
		Animator.enabled = false;
		Collider2D = GetComponent<BoxCollider2D>();
		ResizeColliderToFitSprite(Collider2D, SR);
		propertyBlock = new MaterialPropertyBlock();
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		StopAnimation();
		if (Crop != null)
		{
			Crop.grassRenderer = null;
			Crop = null;
		}
	}

	private void ResizeColliderToFitSprite(BoxCollider2D boxCollider, SpriteRenderer spriteRenderer)
	{
		if (boxCollider != null && spriteRenderer != null && spriteRenderer.sprite != null)
		{
			Vector2 size = spriteRenderer.sprite.bounds.size;
			boxCollider.size = size;
			boxCollider.offset = spriteRenderer.sprite.bounds.center;
		}
		else
		{
			Debug.LogError("CropRenderer: Missing BoxCollider2D or SpriteRenderer, or SpriteRenderer has no sprite.");
		}
	}

	public void ToNext(Sprite sprite)
	{
		ToNext_(sprite, new Vector2(0.2f, 0.3f), Ease.OutBack, 0.5f, 5f);
	}

	protected void ToNext_(Sprite sprite, Vector2 scaleTime, Ease ease, float shakeTime, float angle)
	{
		BoxCollider2D component = GetComponent<BoxCollider2D>();
		component.size = sprite.rect.size * 0.125f;
		component.offset = new Vector2(0f, component.size.y / 2f);
		Vector2 vector = SR.sprite.rect.size / sprite.rect.size;
		Sprite = sprite;
		base.transform.localScale = new Vector3(vector.x, vector.y, 1f);
		base.transform.DOScaleX(1f, scaleTime.x).SetEase(ease);
		base.transform.DOScaleY(1f, scaleTime.y).SetEase(ease);
		base.transform.DOShakeRotation(shakeTime, Vector3.forward * angle, 30);
		DolocAPI.effectProvider.RaiseInstPS(position, InstantParticleEffectsType.LEAVES_AND_SOILS);
	}

	protected void StopAnimation()
	{
		Animator.enabled = false;
		base.transform.rotation = Quaternion.identity;
	}

	public void SwingOnTouch()
	{
		if (!Animator.enabled && base.gameObject.activeSelf)
		{
			Animator.enabled = true;
			DolocAPI.SwingPlantOnTouch(Animator);
		}
	}

	public void SwingLightOnTouch()
	{
		if (!Animator.enabled && base.gameObject.activeSelf)
		{
			Animator.enabled = true;
			DolocAPI.SwingPlantOnTouch(Animator, light: true);
		}
	}

	public void SwingOnBlow()
	{
		if (!Animator.enabled && base.gameObject.activeSelf)
		{
			Animator.enabled = true;
			DolocAPI.SwingPlantOnBlow(Animator);
		}
	}

	public void OnWindBlow(Vector2 position)
	{
		SwingOnBlow();
	}

	public void ToggleSweepLight(bool value)
	{
		SR.GetPropertyBlock(propertyBlock);
		propertyBlock.SetFloat("_ToggleLightSweep", value ? 1 : 0);
		Vector4 value2 = new Vector4(0f, 0f, 1f, 1f);
		if (SR.sprite != null)
		{
			value2 = DataUtility.GetOuterUV(SR.sprite);
		}
		propertyBlock.SetVector("_MainTex_UVInfos", value2);
		SR.SetPropertyBlock(propertyBlock);
	}

	public bool OnFell(ItemTool tool, Vector2 hitPosition)
	{
		return Crop?.OnFell(tool, hitPosition) ?? false;
	}

	public bool OnAttacked(float attack, bool isCritical, Vector2 position, out bool isDead)
	{
		isDead = false;
		Crop?.OnAttack(attack, isCritical, position, out isDead);
		return false;
	}

	public bool OnSwordAttack(float atk, bool isCritical, Vector2 position, out bool isDead)
	{
		return OnAttacked(atk, isCritical, position, out isDead);
	}
}
