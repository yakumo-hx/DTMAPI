using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class MailBox : Equipment
{
	private GameObject tip;

	private Vector3 offset;

	public MailBox(IEquipmentHost host, int instanceId, EquipmentInfo proto, Vector3 wp, Vector2Int anchor, bool turn)
		: base(host, instanceId, proto, wp, anchor, turn)
	{
		offset = ((EquipmentFuncMailBox)proto.Function).TipOffset;
	}

	[JsonConstructor]
	protected MailBox(int id, Vector2Int anchor, Vector3 position, string equipmentName, WeatherDecoratorManager decorator, ElectronicComponent electronicComponent, DecalInfo decalInfo, bool turn)
		: base(id, anchor, position, equipmentName, decorator, electronicComponent, decalInfo, turn)
	{
		offset = ((EquipmentFuncMailBox)proto.Function).TipOffset;
	}

	~MailBox()
	{
		Object.Destroy(tip);
	}

	public void PlayTipAnim(bool value)
	{
		if (!(tip == null))
		{
			tip.SetActive(value);
			Animator component = tip.GetComponent<Animator>();
			if (value)
			{
				component.Play("running");
				component.updateMode = AnimatorUpdateMode.UnscaledTime;
			}
		}
	}

	protected override void OnRender()
	{
		if (base.Renderer != null && tip == null)
		{
			GameObject asset = DolocAPI.GetAsset<GameObject>(DolocGameAssets.GAME_ENTITY_MAILBOX_TIP);
			if (asset != null)
			{
				tip = Object.Instantiate(asset, base.Renderer.transform);
				tip.transform.localPosition = offset;
				PlayTipAnim(DolocAPI.archiveHandle.HasNewEmail());
			}
			else
			{
				Debug.LogError("邮箱提示符加载失败");
			}
		}
	}

	protected override void OnUnRender()
	{
		Object.Destroy(tip);
		tip = null;
	}

	protected override void OnTouch()
	{
		ShowTip(DolocConfig.StaticTexts.UiOperationView);
	}

	protected override void OnDisTouch()
	{
		HideTip();
	}

	protected override void OnInteract()
	{
		PushTipToHide();
		DolocAPI.EnterUI<EmailPanelUiState>();
		SendUseEquipmentMessage();
	}
}
