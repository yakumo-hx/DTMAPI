using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Platform;
using Newtonsoft.Json;

namespace DolocTown;

public class ItemPlatform : Item
{
	private PlatformItemBuilderTip BuilderTip => DolocAPI.dolocBuilder.GetBuilderTip<PlatformItemBuilderTip>();

	public PlatformInfo PlatformProto => DolocConfig.Tables.TbPlatform.GetOrDefault(base.proto?.Id ?? "");

	public ItemPlatform(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemPlatform(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		BuilderTip.ConfirmBuild();
	}

	protected override void OnUseAsItem()
	{
		BuilderTip.CancelBuild();
	}

	protected override void OnQuickSelect()
	{
		BuilderTip.RunBuilder(this);
		base.OnQuickSelect();
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		BuilderTip.ExitBuilder();
	}
}
