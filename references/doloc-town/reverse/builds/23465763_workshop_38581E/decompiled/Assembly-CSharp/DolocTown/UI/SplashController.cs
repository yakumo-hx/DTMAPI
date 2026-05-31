using System;
using System.IO;
using DG.Tweening;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace DolocTown.UI;

public class SplashController : MonoBehaviour
{
	public GameObject container;

	public CanvasGroup canvasGroup;

	public TextMeshProUGUI splashText;

	public float thanksDuration = 1.5f;

	public float warningDuration = 1f;

	public float fadeDuration = 0.3f;

	private JObject splashJson;

	public static bool InSplash { get; private set; }

	private bool disabled => false;

	private string settingFilePath => Path.Combine(Path.Combine(Application.persistentDataPath, "SAVE"), "user_settings.json");

	private void Awake()
	{
		if (disabled)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		container.SetActive(value: true);
		InSplash = true;
		base.gameObject.SetActive(value: true);
	}

	private void Start()
	{
		if (!disabled)
		{
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			LoadSplashJson();
			StartSplashInternal(GetL10nId());
		}
	}

	private void LoadSplashJson()
	{
		TextAsset textAsset = Resources.Load<TextAsset>("splash_text");
		if (textAsset == null)
		{
			Debug.LogError("Splash text json not found!");
		}
		else
		{
			splashJson = JObject.Parse(textAsset.text);
		}
	}

	private string GetL10nId()
	{
		if (File.Exists(settingFilePath))
		{
			try
			{
				string propertyName = "settings";
				string propertyName2 = "LANGUAGE_TEXT";
				JObject jObject = JObject.Parse(File.ReadAllText(settingFilePath));
				if (jObject.ContainsKey(propertyName) && jObject[propertyName] is JObject jObject2 && jObject2.TryGetValue(propertyName2, out JToken value))
				{
					string text = value.ToString();
					if (text == "cn")
					{
						return "zh-CN";
					}
					if (text == "tw")
					{
						return "zh-TW";
					}
					return text;
				}
				return GetL10nIdBySystem();
			}
			catch (Exception)
			{
				return GetL10nIdBySystem();
			}
		}
		return GetL10nIdBySystem();
	}

	private string GetL10nIdBySystem()
	{
		switch (Application.systemLanguage)
		{
		case SystemLanguage.ChineseTraditional:
			return "zh-TW";
		case SystemLanguage.Chinese:
		case SystemLanguage.ChineseSimplified:
			return "zh-CN";
		case SystemLanguage.Japanese:
			return "ja";
		case SystemLanguage.Korean:
			return "ko";
		case SystemLanguage.Portuguese:
			return "pt-BR";
		default:
			return "en";
		}
	}

	private void StartSplashInternal(string l10nId)
	{
		DOTween.Sequence().Append(splashText.DOFade(0f, 0f)).AppendCallback(delegate
		{
			splashText.text = GetThanksText(l10nId).Trim();
		})
			.Append(splashText.DOFade(1f, fadeDuration))
			.AppendInterval(thanksDuration)
			.Append(splashText.DOFade(0f, fadeDuration))
			.AppendCallback(delegate
			{
				splashText.text = GetWarningText(l10nId).Trim();
			})
			.Append(splashText.DOFade(1f, fadeDuration))
			.AppendInterval(warningDuration)
			.Append(splashText.DOFade(0f, fadeDuration))
			.Append(canvasGroup.DOFade(0f, 1.5f))
			.OnComplete(delegate
			{
				InSplash = false;
				UnityEngine.Object.Destroy(base.gameObject);
			});
	}

	private string GetText(string category, string l10nId)
	{
		if (splashJson == null)
		{
			return "";
		}
		if (!(splashJson[category] is JObject jObject))
		{
			return "";
		}
		if (jObject.TryGetValue(l10nId, out JToken value))
		{
			return value.ToString();
		}
		return "";
	}

	private string GetThanksText(string l10nId)
	{
		string text = GetText("thanks", l10nId);
		if (!text.IsNullOrEmpty())
		{
			return text;
		}
		return "\r\nDear Scavengers, <color=#20AB8B>Doloc Town</color> is officially live in Early Access! The team would like to thank everyone for your support and love along the way. Now, it's time to grab your gear and dive into the wild. We hope you enjoy the adventure!\r\nIf you have any feedback or suggestions, please reach out to us at <color=#DF8235>qa@logoi.net</color> - we'd love to hear from you.\r\nWant to stay updated? Join our Discord or follow us on X (<color=#4682B4>@RedSawGames</color>) for the latest news.\r\nThanks again, and happy scavenging!\r\n";
	}

	private string GetWarningText(string l10nId)
	{
		string text = GetText("warning", l10nId);
		if (!text.IsNullOrEmpty())
		{
			return text;
		}
		return "\r\n<size=48><align=center><color=#E05290>Warning: Read Before Playing</color></align></size>\r\nSome flashing lights or visual effects in this game may trigger discomfort in individuals who are photosensitive, have epilepsy, or experience similar conditions. If you experience any symptoms such as eye pain, headaches, or blurred vision, please stop playing and rest.\r\nThank you for playing and have fun!\r\n";
	}
}
