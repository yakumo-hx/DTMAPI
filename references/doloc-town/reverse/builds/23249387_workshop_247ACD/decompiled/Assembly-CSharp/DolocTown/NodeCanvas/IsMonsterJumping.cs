using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Name("是否正在跳跃", 0)]
[Category("多洛可小镇/怪物")]
public class IsMonsterJumping : ConditionTask<MonsterController>
{
	protected override string info => "怪物正在跳跃";

	protected override bool OnCheck()
	{
		return base.agent.IsJumping;
	}
}
