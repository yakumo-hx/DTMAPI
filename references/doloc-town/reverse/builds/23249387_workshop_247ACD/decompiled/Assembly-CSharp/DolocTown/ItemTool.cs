using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemTool : Item
{
	public ItemFunctionTool functionTool => base.proto.Function as ItemFunctionTool;

	public int MaxChopObjects => functionTool.MaxChopObjects;

	public int Level => functionTool.Level;

	public int ChopNumber => functionTool.ChopNumber;

	public int Attack => functionTool.Attack;

	public ToolType ToolType => functionTool.ToolType;

	public ItemTool(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemTool(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	public override bool IsSame(Item other)
	{
		return false;
	}

	protected override void OnUseAsTool()
	{
		DolocAPI.agent.UseTool(this);
	}

	protected override void OnUseAsItem()
	{
		DolocAPI.agent.UseTool(this);
	}

	public override string GetExtraInfo1()
	{
		return DolocConfig.StaticTexts.ItemToolLevelFormat.Format(Level + 1);
	}
}
