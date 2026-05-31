using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Email;
using DolocTown.GameData;
using DolocTown.UI;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class Email
{
	public readonly EmailInfo proto;

	[JsonProperty]
	protected bool isNew = true;

	[JsonProperty]
	private List<EmailAttachBase> emailAttaches = new List<EmailAttachBase>();

	[JsonProperty]
	protected DateInfo sendData;

	[JsonProperty]
	private bool recycled;

	[JsonProperty]
	private bool collected;

	[JsonProperty("id")]
	public string Id => proto?.Id;

	public string IconUrl => proto.IconUrl;

	public string Title => proto.Title;

	public string Content => proto.Content;

	public string Sender => proto.Sender;

	public bool IsAccept
	{
		get
		{
			bool flag = true;
			foreach (EmailAttachBase emailAttach in emailAttaches)
			{
				flag &= emailAttach.IsAccept;
			}
			return flag;
		}
	}

	public bool IsNew => isNew;

	public string SendTime => sendData.GetDateSimpleInfo();

	public bool Recycled
	{
		get
		{
			return recycled;
		}
		set
		{
			recycled = value;
		}
	}

	public bool Collected
	{
		get
		{
			return collected;
		}
		set
		{
			collected = value;
		}
	}

	public bool HasAttach
	{
		get
		{
			bool flag = false;
			foreach (EmailAttachBase emailAttach in emailAttaches)
			{
				flag |= emailAttach.HasAttach;
			}
			return flag;
		}
	}

	public bool HasUnReceivedMission
	{
		get
		{
			bool flag = false;
			foreach (EmailAttachBase emailAttach in emailAttaches)
			{
				if (emailAttach is EmailAttachMission emailAttachMission)
				{
					flag |= !emailAttachMission.AutoAccept && !emailAttachMission.IsAccept;
				}
			}
			return flag;
		}
	}

	public bool HasUnReceivedReward
	{
		get
		{
			bool flag = true;
			foreach (EmailAttachBase emailAttach in emailAttaches)
			{
				if (emailAttach is EmailAttachReward)
				{
					flag &= emailAttach.IsAccept;
				}
			}
			return !flag;
		}
	}

	public List<RewardAcceptState> RewardsStates
	{
		get
		{
			List<RewardAcceptState> list = new List<RewardAcceptState>();
			foreach (EmailAttachBase emailAttach in emailAttaches)
			{
				if (emailAttach is EmailAttachReward attachReward)
				{
					list.Add(new RewardAcceptState(attachReward));
				}
			}
			return list;
		}
	}

	public Email(EmailInfo info, DateInfo dateInfo)
	{
		proto = info;
		sendData = dateInfo;
		CfgEmailAttachBase[] attaches = info.Attaches;
		foreach (CfgEmailAttachBase cfgEmailAttachBase in attaches)
		{
			if (cfgEmailAttachBase is CfgEmailAttachNone)
			{
				continue;
			}
			if (!(cfgEmailAttachBase is CfgEmailAttachReward cfgEmailAttachReward))
			{
				if (cfgEmailAttachBase is CfgEmailAttachMission cfgEmailAttachMission)
				{
					emailAttaches.Add(new EmailAttachMission(cfgEmailAttachMission.MissionChainId, cfgEmailAttachMission.AutoAccept));
				}
			}
			else
			{
				emailAttaches.Add(new EmailAttachReward(cfgEmailAttachReward.RewardProto));
			}
		}
	}

	public Email(EmailInfo info, List<EmailAttachBase> emailAttaches, DateInfo dateInfo)
	{
		proto = info;
		this.emailAttaches = emailAttaches;
		sendData = dateInfo;
	}

	public Email(EmailInfo info, EmailAttachReward attachReward, DateInfo dateInfo)
	{
		proto = info;
		emailAttaches.Add(attachReward);
		sendData = dateInfo;
	}

	[JsonConstructor]
	protected Email(string id, bool isNew, List<EmailAttachBase> emailAttaches, DateInfo dateInfo, bool recycled, bool collected)
	{
		DolocConfig.Tables.TbEmail.DataMap.TryGetValue(id, out proto);
		this.isNew = isNew;
		this.emailAttaches = emailAttaches;
		sendData = dateInfo;
		this.recycled = recycled;
		this.collected = collected;
	}

	public override string ToString()
	{
		return "邮件:<" + proto.Id + ":" + Title + ">\n" + Content;
	}

	protected void OnFirstRead()
	{
		DolocAPI.BroadcastString(GameEventType.READ_EMAIL, Id);
	}

	public bool Read()
	{
		if (!isNew)
		{
			return false;
		}
		foreach (EmailAttachBase emailAttach in emailAttaches)
		{
			emailAttach.OnFirstRead();
		}
		OnFirstRead();
		isNew = false;
		return true;
	}

	public bool Accept()
	{
		if (IsAccept)
		{
			return false;
		}
		bool flag = true;
		foreach (EmailAttachBase emailAttach in emailAttaches)
		{
			flag &= emailAttach.OnAccept();
		}
		return flag;
	}
}
