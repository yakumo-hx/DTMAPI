namespace DolocTown;

public static class BuildingLinkTypePatch
{
	public static BuildingLinkType Invert(this BuildingLinkType type)
	{
		return type switch
		{
			BuildingLinkType.Bottom => BuildingLinkType.Top, 
			BuildingLinkType.Top => BuildingLinkType.Bottom, 
			BuildingLinkType.Left => BuildingLinkType.Right, 
			BuildingLinkType.Right => BuildingLinkType.Left, 
			_ => type, 
		};
	}

	public static float GetBoxColliderHeight(this BuildingLinkType type)
	{
		return type switch
		{
			BuildingLinkType.Bottom => 0.2f, 
			BuildingLinkType.Top => 0.7f, 
			BuildingLinkType.Left => 0.1f, 
			BuildingLinkType.Right => 0.1f, 
			_ => 0.1f, 
		};
	}

	public static float GetGateRotationZ(this BuildingLinkType type)
	{
		return type switch
		{
			BuildingLinkType.Bottom => 0f, 
			BuildingLinkType.Top => 180f, 
			BuildingLinkType.Left => -90f, 
			BuildingLinkType.Right => 90f, 
			_ => 0f, 
		};
	}
}
