namespace DolocTown;

public static class BodyEnumPatch
{
	public static FaintReason ToFaintReason(this HurtReason reason)
	{
		switch (reason)
		{
		case HurtReason.MonsterAttack:
		case HurtReason.NatureAttack:
			return FaintReason.Hurt;
		case HurtReason.BadFood:
			return FaintReason.Food;
		default:
			return FaintReason.None;
		}
	}
}
