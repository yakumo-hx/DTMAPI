using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.Config.Room;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class RewardInteractableObjectUnlock : Reward
{
	[JsonProperty]
	public readonly string lockObjectId;

	private LockableObjectInfo lockableProto;

	public override int Count => 1;

	public override Sprite RewardIcon => lockableProto?.RewardIcon.Asset;

	public override string RewardBriefInfo => lockableProto?.RewardInfo ?? "";

	public override string RewardInfo => RewardBriefInfo;

	[JsonConstructor]
	public RewardInteractableObjectUnlock(string lockObjectId)
		: base(RewardType.INTERACTABLE_UNLOCK)
	{
		this.lockObjectId = lockObjectId;
		lockableProto = DolocConfig.Tables.TbLockableObject.GetOrDefault(lockObjectId ?? "");
	}

	public override bool CheckValid()
	{
		return lockableProto != null;
	}

	public override bool CashReward()
	{
		if (!DolocAPI.SetObjectLockState(lockObjectId, value: false))
		{
			return false;
		}
		DolocAPI.ShowMessageBoxNodeComplete(lockableProto.UnlockInfo);
		return true;
	}

	public override bool TryCombine(Reward other, out Reward reward)
	{
		if (other is RewardInteractableObjectUnlock rewardInteractableObjectUnlock && rewardInteractableObjectUnlock.lockObjectId == lockObjectId)
		{
			reward = new RewardInteractableObjectUnlock(lockObjectId);
			return true;
		}
		reward = null;
		return false;
	}
}
