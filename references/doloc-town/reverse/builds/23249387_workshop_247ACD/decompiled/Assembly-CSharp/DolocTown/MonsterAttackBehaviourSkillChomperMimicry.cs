using DG.Tweening;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DolocTown;

public class MonsterAttackBehaviourSkillChomperMimicry : MonsterAttackBehaviourSkill
{
	private new readonly IChomperMimicry _proto;

	private Tween _tween;

	private bool shouldQuit;

	public MonsterAttackBehaviourSkillChomperMimicry(Transform host, IChomperMimicry proto, SkillManager skillManager)
		: base(host, proto, skillManager)
	{
		_proto = proto;
	}

	public override void Invoke()
	{
		if (!MakeSkill(out ChomperMimicry skill))
		{
			_tween = null;
			return;
		}
		skill.position2d = host.position;
		skill.Duration = Random.Range(_proto.DurationRange.x, _proto.DurationRange.y);
		MonsterController controller = host.GetComponent<MonsterController>();
		Sequence sequence = DOTween.Sequence();
		sequence.AppendCallback(delegate
		{
			controller.Renderer.PlayAnimation("drill_in", 0, 0f, delegate
			{
				controller.GetComponent<Collider2D>().enabled = false;
			});
		});
		sequence.AppendInterval(Random.Range(2f, 3f));
		sequence.AppendCallback(delegate
		{
			host.position = controller.Env.RandomGroundPosition;
			controller.GetComponent<Collider2D>().enabled = true;
			controller.Renderer.PlayAnimation("drill_out");
		});
		sequence.OnComplete(Stop);
		sequence.SetUpdate(UpdateType.Manual, isIndependentUpdate: true);
		_tween = sequence;
	}

	private void Stop()
	{
		End();
		_tween?.Kill();
		_tween = null;
	}

	public override bool OnUpdate(float dt)
	{
		if (_tween == null)
		{
			return true;
		}
		_tween.ManualUpdate(dt, dt);
		return false;
	}

	protected override void OnDispose()
	{
		_tween?.Kill();
		_tween = null;
	}
}
