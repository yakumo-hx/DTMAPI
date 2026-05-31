using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Name("生日检查", 0)]
[Description("检查是否为角色生日")]
public class DialogueTask_BirthdayCheck : DialogueConditionTask
{
	public string npcName = "player";

	public int dayOffset;

	public override string taskTitle => "是否为<" + npcName + ">生日" + dayOffsetInfo;

	private string dayOffsetInfo
	{
		get
		{
			int num = dayOffset;
			if (num <= 0)
			{
				if (num < 0)
				{
					return $"({dayOffset}Day)";
				}
				return "";
			}
			return $"(+{dayOffset}Day)";
		}
	}

	protected override bool CheckCondition()
	{
		return DolocAPI.Command_IsNpcBirthday(npcName, dayOffset);
	}
}
