using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("体力、血量、精力判断", 0)]
[Description("判断玩家的体力、血量或精力的数值状态")]
public class DialogueConditionTask_CheckStatus : DialogueConditionTask
{
	public enum PlayerStatus
	{
		Health,
		Energy,
		Spirit
	}

	[SerializeField]
	[SliderField(0, 1)]
	public float _value = 1f;

	[SerializeField]
	public PlayerStatus status;

	[SerializeField]
	public CompareMethod _compareMethod = CompareMethod.GreaterThan;

	public override string taskTitle => $"{statusName} {_compareMethod} {_value}";

	private string statusName => status switch
	{
		PlayerStatus.Health => "血量", 
		PlayerStatus.Energy => "体力", 
		PlayerStatus.Spirit => "精力", 
		_ => string.Empty, 
	};

	public float getValue()
	{
		return status switch
		{
			PlayerStatus.Health => DolocAPI.archiveHandle.CurrentHealthPercent, 
			PlayerStatus.Energy => DolocAPI.archiveHandle.CurrentEnergyPercent, 
			PlayerStatus.Spirit => DolocAPI.archiveHandle.CurrentSpiritPercent, 
			_ => 0f, 
		};
	}

	protected override bool CheckCondition()
	{
		return _compareMethod switch
		{
			CompareMethod.EqualTo => getValue() == _value, 
			CompareMethod.LessThan => getValue() < _value, 
			CompareMethod.LessOrEqualTo => getValue() <= _value, 
			CompareMethod.GreaterThan => getValue() > _value, 
			CompareMethod.GreaterOrEqualTo => getValue() >= _value, 
			_ => false, 
		};
	}
}
