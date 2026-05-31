using DolocTown.Config;
using DolocTown.Config.UI;

namespace DolocTown.UI;

public abstract class DolocUiEntity : DolocUiObject
{
	public string id => GetType().Name;

	public UIEntityInfo proto => DolocConfig.Tables.TbUIEntity.GetOrDefault(id) ?? DolocAPI.GlobalParameter.DefaultUiEntityInfo_Ref;

	public int SortOrderInGroup => proto.SortingOrder;

	public string GroupName => proto.Group;
}
