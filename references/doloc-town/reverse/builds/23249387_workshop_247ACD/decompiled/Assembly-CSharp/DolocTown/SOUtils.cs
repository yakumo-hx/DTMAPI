using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Localization;

namespace DolocTown;

public static class SOUtils
{
	public static string[] l10nIds => DolocConfig.Tables.TbLocalization.DataMap.Keys.ToArray();

	public static string[] l10nTextIds => DolocConfig.Tables.TbL10nText.DataList.Select((L10nTextInfo x) => x.Id).ToArray();

	public static string[] l10nImageIds => DolocConfig.Tables.TbL10nImage.DataList.Select((L10nImageInfo x) => x.ImgId).ToArray();

	public static string[] textTipIds => DolocConfig.Tables.TbTextTip.DataList.Select((TextTipInfo x) => x.Id).ToArray();

	public static string[] linkIds => DolocConfig.Tables.TbL10nLink.DataList.Select((L10nLinkInfo x) => x.LinkId).ToArray();

	public static string[] GameActionIds => DolocConfig.Tables.TbGameKeyAction.DataMap.Keys.ToArray();

	public static string[] RebindActionIds => DolocConfig.Tables.TbRebindAction.DataMap.Keys.ToArray();
}
