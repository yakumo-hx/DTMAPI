using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Email;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class EmailManager
{
	[JsonProperty]
	public readonly List<Email> emails;

	private Dictionary<string, EmailInfo> EmailDatas => DolocConfig.Tables.TbEmail.DataMap;

	public EmailManager()
	{
		emails = new List<Email>();
	}

	[JsonConstructor]
	private EmailManager(List<Email> emails)
	{
		this.emails = emails.FindAll((Email email) => !string.IsNullOrEmpty(email.Id)).ToList();
	}

	public bool SendEmail(string emailName)
	{
		if (!EmailDatas.TryGetValue(emailName, out var value))
		{
			return false;
		}
		Email email = new Email(value, DolocAPI.archiveHandle.DateNow.Copy());
		return SendEmail(email);
	}

	public bool ContainsEmailName(string name)
	{
		return emails.Any((Email e) => e.Id == name);
	}

	public bool RemoveEmail(string name)
	{
		return emails.RemoveAll((Email e) => e.Id == name) > 0;
	}

	private bool SendEmail(Email email)
	{
		if (email == null)
		{
			return false;
		}
		emails.Insert(0, email);
		RefreshMailBoxTip();
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_NEW_EMAIL);
		return true;
	}

	public bool HasNewEmail()
	{
		return emails.Any((Email email) => email.IsNew);
	}

	public void RefreshMailBoxTip()
	{
		bool value = HasNewEmail();
		IEquipmentHost currentRoom = DolocAPI.CurrentRoom;
		if (currentRoom == null)
		{
			return;
		}
		MailBox[] equipments = currentRoom.GetEquipments<MailBox>();
		if (!equipments.IsNullOrEmpty())
		{
			MailBox[] array = equipments;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].PlayTipAnim(value);
			}
		}
	}

	public bool SendItemAsEmail(string itemName, int count, string emailName = null, string content = null, string sender = null, string templateName = "send_item_template")
	{
		if (!EmailDatas.TryGetValue(templateName, out var value))
		{
			Debug.LogError("没有指定的模板邮件：" + templateName);
			return false;
		}
		if (DolocAPI.QueryItemProto(itemName, out var _))
		{
			Email email = new Email(value, new EmailAttachReward(new RewardProto(RewardType.ITEM, itemName, count)), DolocAPI.archiveHandle.DateNow.Copy());
			return SendEmail(email);
		}
		return false;
	}

	public bool SendSellBoxEmail(int money)
	{
		string text = "drop_off_box_template";
		if (!EmailDatas.TryGetValue(text, out var value))
		{
			Debug.LogError("没有指定的模板邮件：" + text);
			return false;
		}
		RewardGold reward = new RewardGold((ushort)money);
		Email email = new Email(value, new EmailAttachReward(reward), DolocAPI.archiveHandle.DateNow.Copy());
		return SendEmail(email);
	}

	public bool SendItemsAsEmail(IEnumerable<Reward> rewards, string templateName)
	{
		if (!EmailDatas.TryGetValue(templateName, out var value))
		{
			Debug.LogError("没有指定的模板邮件：" + templateName);
			return false;
		}
		List<EmailAttachBase> list = new List<EmailAttachBase>();
		foreach (Reward reward in rewards)
		{
			list.Add(new EmailAttachReward(reward));
		}
		Email email = new Email(value, list, DolocAPI.archiveHandle.DateNow.Copy());
		return SendEmail(email);
	}

	public void __Unique(string name)
	{
		int num = emails.FindLastIndex((Email x) => x.Id == name);
		if (num < 0)
		{
			return;
		}
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			if (emails[num2].Id == name)
			{
				emails.RemoveAt(num2);
			}
		}
	}
}
