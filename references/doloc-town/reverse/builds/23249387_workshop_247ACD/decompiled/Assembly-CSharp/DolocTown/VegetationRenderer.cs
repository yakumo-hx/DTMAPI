using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D), typeof(Animator))]
[GameEntityManager("/dungeon/vegetations", DolocGameAssets.GAME_ENTITY_VEGETATION)]
public class VegetationRenderer : GameRendererEntity, IAttackable, IFellable, IInteractable, IWindInteractive, IBombInteractive, IMonsterInteractable
{
	private Animator _animator;

	public SpriteRenderer Sr { get; private set; }

	public override SpriteRenderer outlineTarget => Sr;

	public PolygonCollider2D Collider2d { get; private set; }

	public Vegetation Vegetation { get; set; }

	public AttackableType attackableType => AttackableType.None;

	public bool ShouldCostEnergy => false;

	public bool ShouldCostChopCounter => false;

	public bool OnlyTouch
	{
		get
		{
			if (Vegetation != null)
			{
				return Vegetation.OnlyTouch;
			}
			return true;
		}
	}

	public bool CanInteractContinues => false;

	protected override void __Init()
	{
		base.__Init();
		Sr = GetComponent<SpriteRenderer>();
		Collider2d = GetComponent<PolygonCollider2D>();
		_animator = GetComponent<Animator>();
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		if (Vegetation != null)
		{
			Vegetation.OnUnRender();
			Vegetation.Renderer = null;
			Vegetation = null;
		}
	}

	public override void OnReuse()
	{
		base.OnReuse();
		_animator.enabled = false;
		Collider2d.enabled = true;
		Sr.sharedMaterial = LocMaterials.GAME_MAT_2D;
	}

	public void StopAnimation()
	{
		_animator.enabled = false;
		base.transform.rotation = Quaternion.identity;
	}

	public void SwingOnTouch(bool light = false)
	{
		if (!_animator.enabled && base.gameObject.activeSelf)
		{
			_animator.enabled = true;
			DolocAPI.SwingPlantOnTouch(_animator, light);
		}
	}

	public void SwingOnBlow()
	{
		if (!_animator.enabled && base.gameObject.activeSelf)
		{
			_animator.enabled = true;
			DolocAPI.SwingPlantOnBlow(_animator);
		}
	}

	public bool OnAttacked(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		isDead = false;
		return Vegetation?.OnAttack(attack, criticalRate, pos) ?? false;
	}

	public bool OnSwordAttack(float attack, bool criticalRate, Vector2 pos, out bool isDead)
	{
		return OnAttacked(attack, criticalRate, pos, out isDead);
	}

	public bool OnFell(ItemTool tool, Vector2 hitPoint)
	{
		return Vegetation?.OnFell(tool, hitPoint) ?? false;
	}

	public void OnTouch()
	{
		Vegetation?.OnTouch();
	}

	public void OnDisTouch()
	{
		Vegetation?.OnDisTouch();
	}

	public void OnInteract()
	{
		Vegetation?.OnInteract();
	}

	public void OnMonsterTouch(Vector2 interactPosition)
	{
		Vegetation?.OnMonsterTouch(interactPosition);
	}

	public void OnMonsterDisTouch(Vector2 interactPosition)
	{
		Vegetation?.OnMonsterDisTouch(interactPosition);
	}

	public void OnWindBlow(Vector2 pos)
	{
		Vegetation?.OnWindBlow(pos);
	}

	public void OnBomb(float d, bool c, Vector2 pos)
	{
		Vegetation?.OnBomb(d, c, pos);
	}
}
