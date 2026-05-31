using System.Collections.Generic;

namespace DolocTown;

public class Liking
{
	public float likingValue;

	public HashSet<string> giftRecord;

	public int giftCount;

	public int extraGiftCount;

	public bool haveSaid;

	public bool isParticipateActivity;

	public int LikingLevel => (int)(likingValue / (float)DolocAPI.GlobalParameter.LikingCeiling);

	public Liking()
	{
		likingValue = 0f;
		giftRecord = new HashSet<string>();
		giftCount = 0;
		extraGiftCount = 0;
		haveSaid = false;
		isParticipateActivity = false;
	}
}
