using DolocTown.Config;
using UnityEngine;
using UnityEngine.Serialization;

namespace DolocTown.UI;

public class LinkButton : DolocNavigationButton
{
	[FormerlySerializedAs("linkIds")]
	[SerializeField]
	private string linkId;

	public string LinkId => linkId;

	protected override void __Init()
	{
		base.__Init();
		onClick.AddListener(delegate
		{
			OpenOuterLink();
		});
	}

	public virtual void OpenOuterLink()
	{
		try
		{
			string text = DolocConfig.Tables.TbL10nLink.Get(linkId, DolocAPI.CurrentL10nId)?.Url;
			if (!text.IsNullOrEmpty())
			{
				Application.OpenURL(text);
			}
		}
		catch
		{
			Debug.LogError("无法打开浏览器");
		}
	}
}
