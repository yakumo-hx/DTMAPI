using UnityEngine;

namespace DolocTown;

[GameEntityManager("/sub_entity/following_flag", DolocGameAssets.GAME_ENTITY_FOLLOWING_FLAG)]
public class FollowingFlag : GameEntity
{
	[SerializeField]
	private SpriteRenderer flagRenderer;

	private bool _shouldFollow;

	private Transform _target;

	private Sprite _originBackgroundSprite;

	public Transform Target
	{
		get
		{
			return _target;
		}
		set
		{
			_target = value;
			_shouldFollow = value != null;
			SetVisible(_shouldFollow);
		}
	}

	public Vector3 Offset { get; set; } = Vector3.zero;


	public Sprite FlagSprite
	{
		get
		{
			return flagRenderer.sprite;
		}
		set
		{
			flagRenderer.sprite = value;
		}
	}

	public bool ShowBackground
	{
		get
		{
			return GetComponent<SpriteRenderer>().enabled;
		}
		set
		{
			GetComponent<SpriteRenderer>().enabled = value;
		}
	}

	public void Update()
	{
		if (_shouldFollow)
		{
			base.transform.position = _target.position + Offset;
		}
	}
}
