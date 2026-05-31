using System;
using DG.Tweening;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(PolygonCollider2D), typeof(SpriteRenderer), typeof(Animator))]
[GameEntityManager("/dungeon/dungeon_resource", DolocGameAssets.GAME_ENTITY_DUNGEON_RESOURCE)]
public class DungeonResourceRenderer : GameRendererEntity, IFellable, IAttackable, IWaterable, IInteractable, IWindInteractive, IMonsterInteractable, IBombInteractive, IAnimalTouchable
{
	public static readonly SpriteShapeBuffer shapeBuffer = new SpriteShapeBuffer();

	private bool isShine;

	private Tween anim;

	private Tween falldownAnim;

	public override Vector3 position
	{
		get
		{
			return base.transform.position;
		}
		set
		{
			base.transform.position = new Vector3(value.x, value.y - value.x * -1E-07f, value.z);
		}
	}

	public SpriteRenderer Sr { get; protected set; }

	public override SpriteRenderer outlineTarget => Sr;

	public PolygonCollider2D PolygonCollider { get; protected set; }

	public Animator Animator { get; protected set; }

	public virtual DungeonResource DungeonResource { get; set; }

	public bool ShouldCostEnergy => true;

	public bool ShouldCostChopCounter => true;

	public virtual bool OnlyTouch => DungeonResource?.OnlyTouch ?? false;

	public bool CanInteractContinues => false;

	public AttackableType attackableType => AttackableType.None;

	protected override void __Init()
	{
		base.__Init();
		Sr = GetComponent<SpriteRenderer>();
		PolygonCollider = GetComponent<PolygonCollider2D>();
		Animator = GetComponent<Animator>();
		Animator.enabled = false;
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		StopAnimation();
		falldownAnim?.Kill();
		anim?.Kill();
		if (DungeonResource != null)
		{
			DungeonResource.UnRender();
			DungeonResource.Renderer = null;
			DungeonResource = null;
		}
	}

	public void FallDown(int dir, Action downCallback = null, Action afterCallback = null)
	{
		dir = ((dir == 0) ? (-1) : dir);
		falldownAnim?.Kill();
		float treeHeight = Sr.sprite.rect.height * 0.125f;
		base.transform.rotation = Quaternion.identity;
		Sequence s = DOTween.Sequence();
		s.Append(base.transform.DORotate(new Vector3(0f, 0f, dir * 80), 2.5f).SetEase(Ease.InExpo));
		s.AppendCallback(delegate
		{
			DolocAPI.RaiseInstantPSEffects(base.transform.position, InstantParticleEffectsType.SMOKE_BRUST_02);
			DolocAPI.RaiseInstantPSEffects(base.transform.position, InstantParticleEffectsType.SAWDUST);
			DolocAPI.RaiseInstantPSEffects(base.transform.position - new Vector3((float)dir * treeHeight * 0.65f, 0f, 0f), InstantParticleEffectsType.BUNCH_OF_LEAVES);
			DolocAPI.cameraController.ShakeScreen();
			downCallback?.Invoke();
		});
		s.Append(base.transform.DORotate(new Vector3(0f, 0f, dir * 90), 1.15f).SetEase(Ease.OutElastic));
		s.AppendCallback(delegate
		{
			afterCallback?.Invoke();
		});
		falldownAnim = s;
	}

	public void Shine(float duration = 0.3f)
	{
	}

	public void _Shine(float duration = 0.3f)
	{
		if (!isShine)
		{
			isShine = true;
			Material originMat = Sr.material;
			Sr.material = LocMaterials.GAME_MAT_HIT;
			DolocAPI.Delay(duration, delegate
			{
				isShine = false;
				Sr.sharedMaterial = originMat;
			});
		}
	}

	public void Shake(float duration = 0.3f, float shakeStrength = 0.1f)
	{
		anim?.Kill();
		anim = base.transform.DOShakePosition(duration, new Vector3(shakeStrength, 0f, 0f));
	}

	public void PlayAnimation(string name)
	{
		if (!Animator.enabled && base.gameObject.activeSelf)
		{
			Animator.enabled = true;
			Animator.Play(name, 0, 0f);
		}
	}

	public void ForcePlayAnimation(string name)
	{
		if (!base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: true);
		}
		Animator.enabled = true;
		Animator.Play(name, 0, 0f);
	}

	protected virtual void StopAnimation()
	{
		Animator.enabled = false;
		base.transform.rotation = Quaternion.identity;
	}

	public void SwingOnTouch(bool light = false)
	{
		if (!Animator.enabled && base.gameObject.activeSelf)
		{
			Animator.enabled = true;
			DolocAPI.SwingPlantOnTouch(Animator, light);
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

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(Sr.material);
	}

	public virtual bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		return DungeonResource?.OnFell(tool, hitPoint) ?? false;
	}

	public virtual void OnInteract()
	{
		DungeonResource?.OnInteract();
	}

	public virtual void OnTouch()
	{
		DungeonResource?.OnTouch();
	}

	public virtual void OnDisTouch()
	{
		DungeonResource?.OnDisTouch();
	}

	public bool OnAttacked(float attack, bool isCritical, Vector2 pos, out bool isDead)
	{
		isDead = false;
		if (DungeonResource == null)
		{
			return false;
		}
		return DungeonResource.OnHit(pos);
	}

	public bool OnSwordAttack(float atk, bool isCritical, Vector2 pos, out bool isDead)
	{
		return OnAttacked(atk, isCritical, pos, out isDead);
	}

	public void OnMonsterTouch(Vector2 interactPosition)
	{
		DungeonResource?.OnMonsterTouch(interactPosition);
	}

	public void OnMonsterDisTouch(Vector2 interactPosition)
	{
		DungeonResource?.OnMonsterDisTouch(interactPosition);
	}

	public void OnWater()
	{
		DungeonResource?.OnWater();
	}

	public void OnWindBlow(Vector2 pos)
	{
		DungeonResource?.OnWindBlow(pos);
	}

	public void OnBomb(float damage, bool criticalRate, Vector2 pos)
	{
		DungeonResource?.OnBomb(damage, criticalRate, pos);
	}

	void IAnimalTouchable.OnAnimalTouch()
	{
		DungeonResource?.OnAnimalTouch();
	}

	void IAnimalTouchable.OnAnimalDistouch()
	{
		DungeonResource?.OnAnimalDistouch();
	}
}
