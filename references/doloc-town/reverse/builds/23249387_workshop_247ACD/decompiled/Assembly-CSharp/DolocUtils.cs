using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DG.Tweening;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.Sound;
using DolocTown.Config.Tile;
using DolocTown.UI;
using RedSaw;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class DolocUtils
{
	public static TextGenerator textGen = new TextGenerator();

	private static Dictionary<string, TileMaterial> _tileMaterialCache;

	public static string timeStr => "[" + PadZero(DateTime.Now.Hour) + ":" + PadZero(DateTime.Now.Minute) + ":" + PadZero(DateTime.Now.Second) + "]";

	public static Transform GetSubContainer(Transform parent, string name)
	{
		Transform transform = parent.Find(name);
		if (transform == null)
		{
			transform = new GameObject(name).transform;
			transform.SetParent(parent);
		}
		return transform;
	}

	public static void moveToHCenter(RectTransform transform)
	{
		if (transform.parent != null)
		{
			RectTransform component = transform.parent.GetComponent<RectTransform>();
			if (component != null)
			{
				transform.localPosition = new Vector3((component.sizeDelta.x - transform.sizeDelta.x) / 2f, transform.localPosition.y, 0f);
			}
		}
	}

	public static void moveToVCenter(RectTransform transform)
	{
		if (transform.parent != null)
		{
			RectTransform component = transform.parent.GetComponent<RectTransform>();
			if (component != null)
			{
				transform.localPosition = new Vector3(transform.localPosition.x, (component.sizeDelta.y - transform.sizeDelta.y) / 2f, 0f);
			}
		}
	}

	public static void moveToCenter(RectTransform transform)
	{
		if (transform.parent != null)
		{
			RectTransform component = transform.parent.GetComponent<RectTransform>();
			if (component != null)
			{
				transform.localPosition = new Vector3((component.sizeDelta.x - transform.sizeDelta.x) / 2f, (component.sizeDelta.y - transform.sizeDelta.y) / 2f, 0f);
			}
		}
	}

	public static void moveToTop(RectTransform transform, float top)
	{
		if (transform.parent != null)
		{
			RectTransform component = transform.parent.GetComponent<RectTransform>();
			if (component != null)
			{
				transform.localPosition = new Vector3(transform.localPosition.x, component.sizeDelta.y - top - transform.sizeDelta.y, 0f);
			}
		}
	}

	public static void moveToRight(RectTransform transform, float right)
	{
		if (transform.parent != null)
		{
			RectTransform component = transform.parent.GetComponent<RectTransform>();
			if (component != null)
			{
				transform.localPosition = new Vector3(component.sizeDelta.x - transform.sizeDelta.x - right, transform.localPosition.y, 0f);
			}
		}
	}

	public static Color setAlpha(Color c, float alpha)
	{
		Color result = c;
		result.a = alpha;
		return result;
	}

	public static void setAlpha(Image img, float alpha)
	{
		if (!(img == null))
		{
			Color color = img.color;
			color.a = alpha;
			img.color = color;
		}
	}

	public static void setAlpha(Text text, float alpha)
	{
		Color color = text.color;
		color.a = alpha;
		text.color = color;
	}

	public static void setAlpha(TextMeshProUGUI text, float alpha)
	{
		Color color = text.color;
		color.a = alpha;
		text.color = color;
	}

	public static float CalcVerticalGroupHeight(VerticalLayoutGroup layoutGroup, float elemHeight, int count)
	{
		return (float)(count - 1) * layoutGroup.spacing + (float)layoutGroup.padding.vertical + elemHeight * (float)count;
	}

	public static float CalcVerticalGroupHeight(VerticalLayoutGroup layoutGroup)
	{
		float num = 0f;
		for (int i = 0; i < layoutGroup.transform.childCount; i++)
		{
			num += layoutGroup.transform.GetChild(i).GetComponent<RectTransform>().rect.height;
		}
		return num + ((float)(layoutGroup.transform.childCount - 1) * layoutGroup.spacing + (float)layoutGroup.padding.top + (float)layoutGroup.padding.bottom);
	}

	public static float getTextWidth(Text text)
	{
		Vector2 sizeDelta = text.rectTransform.sizeDelta;
		return textGen.GetPreferredWidth(text.text, text.GetGenerationSettings(sizeDelta));
	}

	public static string PadZero(int value)
	{
		if (value < 10)
		{
			return $"0{value}";
		}
		return value.ToString();
	}

	public static void SetTextWithEllipsis(this Text textComponent, string value, int offset = 0)
	{
		if (!textComponent)
		{
			return;
		}
		if (string.IsNullOrEmpty(value))
		{
			textComponent.text = value;
			return;
		}
		if (textComponent.verticalOverflow != 0)
		{
			textComponent.verticalOverflow = VerticalWrapMode.Truncate;
		}
		TextGenerator textGenerator = new TextGenerator();
		RectTransform component = textComponent.GetComponent<RectTransform>();
		if (!component)
		{
			return;
		}
		TextGenerationSettings generationSettings = textComponent.GetGenerationSettings(component.rect.size);
		textGenerator.Populate(value, generationSettings);
		int num = textGenerator.characterCountVisible - offset;
		if (num < 1)
		{
			textComponent.text = value;
			return;
		}
		string text = value;
		if (value.Length > num)
		{
			text = value.Substring(0, num);
			text += "…";
		}
		textComponent.text = text;
	}

	public static string GetVisibleText(this Text textComponent, string text)
	{
		textComponent.text = text;
		TextGenerator cachedTextGenerator = textComponent.cachedTextGenerator;
		cachedTextGenerator.Populate(settings: textComponent.GetGenerationSettings(textComponent.rectTransform.rect.size), str: textComponent.text);
		int characterCountVisible = cachedTextGenerator.characterCountVisible;
		return textComponent.text.Substring(0, characterCountVisible);
	}

	public static Sprite LoadLangSprite(this Image obj, string imgId, string defaultname = "")
	{
		if (obj == null)
		{
			return null;
		}
		if (string.IsNullOrEmpty(imgId))
		{
			return null;
		}
		return DolocConfig.Tables.TbL10nImage.Get(imgId, DolocAPI.CurrentL10nId)?.SpriteAsset.Asset;
	}

	public static int[] RandIntList(int num)
	{
		int[] array = new int[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = i;
		}
		return array.Shuffle();
	}

	public static List<List<string>> GeneratePermutations(this List<string> list)
	{
		List<List<string>> list2 = new List<List<string>>();
		if (list.Count <= 1)
		{
			list2.Add(new List<string>(list));
			return list2;
		}
		for (int i = 0; i < list.Count; i++)
		{
			string item = list[i];
			List<string> list3 = new List<string>(list);
			list3.RemoveAt(i);
			foreach (List<string> item2 in list3.GeneratePermutations())
			{
				item2.Insert(0, item);
				list2.Add(item2);
			}
		}
		return list2;
	}

	public static List<List<T>> GetUniquePermutations<T>(this List<T> list)
	{
		List<List<T>> result = new List<List<T>>();
		list.Sort();
		bool[] used = new bool[list.Count];
		Backtrack(list, new List<T>(), used, result);
		return result;
	}

	private static void Backtrack<T>(List<T> list, List<T> tempList, bool[] used, List<List<T>> result)
	{
		if (tempList.Count == list.Count)
		{
			result.Add(new List<T>(tempList));
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			if (!used[i] && (i <= 0 || !list[i].Equals(list[i - 1]) || used[i - 1]))
			{
				used[i] = true;
				tempList.Add(list[i]);
				Backtrack(list, tempList, used, result);
				used[i] = false;
				tempList.RemoveAt(tempList.Count - 1);
			}
		}
	}

	public static void AddValue<TK, TV>(this Dictionary<TK, List<TV>> dict, Dictionary<TK, List<TV>> other)
	{
		foreach (var (key, collection) in other)
		{
			if (!dict.ContainsKey(key))
			{
				dict.TryAdd(key, new List<TV>());
			}
			dict[key].AddRange(collection);
		}
	}

	public static void AddValue<TK>(this Dictionary<TK, int> dict, Dictionary<TK, int> other)
	{
		foreach (KeyValuePair<TK, int> item in other)
		{
			item.Deconstruct(out var key, out var value);
			TK val = key;
			int num = value;
			if (!dict.ContainsKey(val))
			{
				dict.TryAdd(val, 0);
			}
			key = val;
			dict[key] += num;
		}
	}

	public static string ClearNoWrapSpace(this string str)
	{
		return str.Replace(" ", "\u00a0");
	}

	public static string ReplaceNoWrapSpace(this string str)
	{
		return str.Replace("\u00a0", " ");
	}

	public static string ClearRichTextLabel(this string richText)
	{
		return Regex.Replace(richText, "<[^>]+>", string.Empty);
	}

	public static string ClearRichTextLabelExceptColor(this string richText)
	{
		if (string.IsNullOrEmpty(richText))
		{
			return richText;
		}
		string pattern = "<(?!color\\b)[^>]+>";
		string input = Regex.Replace(richText, pattern, "");
		pattern = "</(?!color\\b)[^>]+>";
		return Regex.Replace(input, pattern, "");
	}

	public static string ClearMarkUp(this string text)
	{
		return Regex.Replace(text, "\\[[^\\]]+\\]", string.Empty);
	}

	public static string ClearLineBreaks(this string text)
	{
		return Regex.Replace(text, "\\r\\n?|\\n", "");
	}

	public static string Format(this string str, object arg0)
	{
		return string.Format(str, arg0);
	}

	public static string Format(this string str, object arg0, object arg1)
	{
		return string.Format(str, arg0, arg1);
	}

	public static string Format(this string str, object arg0, object arg1, object arg2)
	{
		return string.Format(str, arg0, arg1, arg2);
	}

	public static string Format(this string str, params object[] args)
	{
		return string.Format(str, args);
	}

	public static string Colored(this string str, Color color)
	{
		return "<color=" + color.ToHex() + ">" + str + "</color>";
	}

	public static string Colored(this string str, string hex)
	{
		return "<color=" + hex + ">" + str + "</color>";
	}

	public static string ChangeSpriteColor(this string str, Color color)
	{
		return str.ChangeSpriteColor(color.ToHex());
	}

	public static string ChangeSpriteColor(this string str, string hex)
	{
		string pattern = "<sprite name=\"([^\"]+)\" color=#\\w{6}>";
		string pattern2 = "<sprite name=\"([^\"]+)\">";
		if (Regex.IsMatch(str, pattern))
		{
			return Regex.Replace(str, pattern, (Match m) => "<sprite name=\"" + m.Groups[1].Value + "\" color=" + hex + ">");
		}
		if (Regex.IsMatch(str, pattern2))
		{
			return Regex.Replace(str, pattern2, (Match m) => "<sprite name=\"" + m.Groups[1].Value + "\" color=" + hex + ">");
		}
		return str;
	}

	public static T ConvertToEnumOrDefault<T>(this string str, bool ignoreCase = true) where T : Enum
	{
		if (!Enum.TryParse(typeof(T), str, ignoreCase, out var result))
		{
			return default(T);
		}
		return (T)result;
	}

	public static T Next<T>(this T src) where T : Enum
	{
		T[] array = (T[])Enum.GetValues(src.GetType());
		int num = Array.IndexOf(array, src) + 1;
		if (num != array.Length)
		{
			return array[num];
		}
		return array[0];
	}

	public static T Previous<T>(this T src) where T : struct, Enum
	{
		T[] array = (T[])Enum.GetValues(src.GetType());
		int num = Array.IndexOf(array, src) - 1;
		if (num >= 0)
		{
			return array[num];
		}
		return array[^1];
	}

	public static T CreateUiEntity<T>(DolocGameAssets name, Transform container) where T : DolocUiObject
	{
		GameObject asset = DolocAPI.GetAsset<GameObject>(name);
		if (asset == null)
		{
			return null;
		}
		T component = UnityEngine.Object.Instantiate(asset, container).GetComponent<T>();
		if ((object)component != null)
		{
			component.Init();
			return component;
		}
		return component;
	}

	public static Tween RollNumber(int value, int target, Action<int> setter, Ease ease = Ease.OutQuart, float duration = 0.5f)
	{
		return DOTween.To(() => value, delegate(int v)
		{
			value = v;
		}, target, duration).OnUpdate(delegate
		{
			setter(value);
		}).SetEase(ease);
	}

	public static string GenerateCountItemInfo(CountItem[] countItems, string separator = ", ")
	{
		string[] titles = countItems.Select((CountItem x) => DolocAPI.GetItemTitle(x.itemName)).ToArray();
		int[] counts = countItems.Select((CountItem x) => x.itemCount).ToArray();
		return GenerateCountItemInfo(titles, counts);
	}

	public static string GenerateCountItemInfo(string[] titles, int[] counts, string separator = ", ", bool useWrapSpace = false)
	{
		if (titles.Length != counts.Length)
		{
			Debug.LogWarning("数组长度不一致");
		}
		int num = Mathf.Min(titles.Length, counts.Length);
		string[] array = new string[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = string.Format(DolocConfig.StaticTexts.UiItemTip, titles[i], counts[i]);
		}
		string str = string.Join("<color=#FFFDE3>" + separator + "</color>", array);
		if (!useWrapSpace)
		{
			return str.ClearNoWrapSpace();
		}
		return str.ReplaceNoWrapSpace();
	}

	public static Dictionary<string, TileMaterial> GetTileToMatLut()
	{
		if (_tileMaterialCache != null)
		{
			return _tileMaterialCache;
		}
		_tileMaterialCache = new Dictionary<string, TileMaterial>();
		foreach (KeyValuePair<string, FootStepInfo> item in DolocConfig.Tables.TbFootStep.DataMap)
		{
			_tileMaterialCache.Add(item.Key, item.Value.TileMaterial);
		}
		return _tileMaterialCache;
	}

	private static bool QueryTileMaterial(Dictionary<string, TileMaterial> tile2MatLut, TileBase tile, out TileMaterial mat)
	{
		if (tile != null && tile2MatLut.ContainsKey(tile.name))
		{
			mat = tile2MatLut[tile.name];
			return true;
		}
		mat = TileMaterial.NONE;
		return false;
	}

	public static Dictionary<Vector2Int, TileMaterial> GenPosToMaterialLut(Dictionary<string, TileMaterial> tileToMatLut, Tilemap baseMap, Tilemap dctMap)
	{
		Dictionary<Vector2Int, TileMaterial> dictionary = new Dictionary<Vector2Int, TileMaterial>();
		for (int i = 0; i < baseMap.size.x; i++)
		{
			for (int j = 0; j < baseMap.size.y; j++)
			{
				Vector3Int vector3Int = new Vector3Int(i, j, 0);
				TileMaterial mat = TileMaterial.NONE;
				if (dctMap != null && QueryTileMaterial(tileToMatLut, dctMap.GetTile(vector3Int), out mat))
				{
					dictionary.Add((Vector2Int)vector3Int, mat);
				}
				else if (QueryTileMaterial(tileToMatLut, baseMap.GetTile(vector3Int), out mat))
				{
					dictionary.Add((Vector2Int)vector3Int, mat);
				}
			}
		}
		return dictionary;
	}

	public static Dictionary<Vector2Int, TileMaterial> GenPosToMaterialLut(Dictionary<string, TileMaterial> tileToMatLut, Tilemap baseMap, Tilemap dctMap, Vector2Int offset, Vector2Int size)
	{
		Dictionary<Vector2Int, TileMaterial> dictionary = new Dictionary<Vector2Int, TileMaterial>();
		for (int i = offset.x; i < size.x + offset.x; i++)
		{
			for (int j = offset.y; j < size.y + offset.y; j++)
			{
				Vector3Int vector3Int = new Vector3Int(i, j, 0);
				TileMaterial mat = TileMaterial.NONE;
				if (dctMap != null && QueryTileMaterial(tileToMatLut, dctMap.GetTile(vector3Int), out mat))
				{
					dictionary.Add((Vector2Int)vector3Int, mat);
				}
				else if (QueryTileMaterial(tileToMatLut, baseMap.GetTile(vector3Int), out mat))
				{
					dictionary.Add((Vector2Int)vector3Int, mat);
				}
			}
		}
		return dictionary;
	}

	public static Dictionary<Vector2Int, TileMaterial> GenPosToMaterialLut(Dictionary<string, TileMaterial> tileToMat, Vector2Int offset, Vector2Int size, Tilemap tilemap)
	{
		Dictionary<Vector2Int, TileMaterial> dictionary = new Dictionary<Vector2Int, TileMaterial>();
		if (tilemap == null)
		{
			return dictionary;
		}
		for (int i = offset.x; i < size.x + offset.x; i++)
		{
			for (int j = offset.y; j < size.y + offset.y; j++)
			{
				Vector3Int vector3Int = new Vector3Int(i, j, 0);
				if (QueryTileMaterial(tileToMat, tilemap.GetTile(vector3Int), out var mat))
				{
					dictionary.Add((Vector2Int)vector3Int, mat);
				}
			}
		}
		return dictionary;
	}

	public static bool IsInteractWithUI()
	{
		if (DolocAPI.UserInput.DeviceType == DolocInputDeviceType.KeyboardMouse && EventSystem.current != null)
		{
			return EventSystem.current.IsPointerOverGameObject();
		}
		return false;
	}

	public static bool IsInteractWithUI(out GameObject current)
	{
		current = GetTopPointerOverUIObject();
		if (DolocAPI.UserInput.DeviceType == DolocInputDeviceType.KeyboardMouse && EventSystem.current != null)
		{
			return EventSystem.current.IsPointerOverGameObject();
		}
		return false;
	}

	public static GameObject GetTopPointerOverUIObject()
	{
		if (EventSystem.current == null)
		{
			return null;
		}
		PointerEventData eventData = new PointerEventData(EventSystem.current)
		{
			position = DolocAPI.UserInput.MousePosition
		};
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventData, list);
		if (list.Count <= 0)
		{
			return null;
		}
		return list[0].gameObject;
	}

	public static Vector3 AnchorToParent(this Transform transform, RectTransform parent, UIAlignmentType targetAnchor)
	{
		transform.SetParent(parent.transform);
		transform.localPosition = parent.sizeDelta * (GetPivotVector(targetAnchor) - parent.pivot);
		return transform.position;
	}

	public static void AnchorToParent(this Transform transform, RectTransform parent, UIAlignmentType targetAnchor, Vector2 offset)
	{
		transform.SetParent(parent.transform);
		transform.localPosition = parent.sizeDelta * (GetPivotVector(targetAnchor) - parent.pivot) + offset;
	}

	public static Vector2 GetLocalPositionByAnchor(this RectTransform rectTransform, UIAlignmentType targetAnchor)
	{
		return rectTransform.sizeDelta * (GetPivotVector(targetAnchor) - rectTransform.pivot);
	}

	public static void SetPivot(this DolocUiObject obj, UIAlignmentType anchorType)
	{
		obj.rectTransform.pivot = GetPivotVector(anchorType);
	}

	public static void SetPivot(this RectTransform rectTransform, UIAlignmentType anchorType)
	{
		rectTransform.pivot = GetPivotVector(anchorType);
	}

	public static void SetAnchorAndPivot(this DolocUiObject obj, UIAlignmentType anchorType)
	{
		obj.rectTransform.SetAnchorAndPivot(anchorType);
	}

	public static void SetAnchorAndPivot(this RectTransform rectTransform, UIAlignmentType anchorType)
	{
		Vector2 anchorMax = (rectTransform.anchorMin = (rectTransform.pivot = GetPivotVector(anchorType)));
		rectTransform.anchorMax = anchorMax;
	}

	public static Vector2 GetPivotVector(UIAlignmentType anchorType)
	{
		return anchorType switch
		{
			UIAlignmentType.TopMiddle => new Vector2(0.5f, 1f), 
			UIAlignmentType.BottomMiddle => new Vector2(0.5f, 0f), 
			UIAlignmentType.LeftMiddle => new Vector2(0f, 0.5f), 
			UIAlignmentType.RightMiddle => new Vector2(1f, 0.5f), 
			UIAlignmentType.LeftTop => new Vector2(0f, 1f), 
			UIAlignmentType.RightTop => new Vector2(1f, 1f), 
			UIAlignmentType.LeftBottom => new Vector2(0f, 0f), 
			UIAlignmentType.RightBottom => new Vector2(1f, 0f), 
			UIAlignmentType.Center => new Vector2(0.5f, 0.5f), 
			_ => new Vector2(0f, 0f), 
		};
	}

	public static T GetOrCreateComponent<T>(this GameObject go) where T : Component
	{
		if (!go.TryGetComponent<T>(out var component))
		{
			return go.AddComponent<T>();
		}
		return component;
	}

	public static T GetOrCreateComponent<T>(this GameObject go, Action<T> onCreate) where T : Component
	{
		if (!go.TryGetComponent<T>(out var component))
		{
			component = go.AddComponent<T>();
			onCreate?.Invoke(component);
		}
		return component;
	}

	public static DolocButtonComponent GetOrCreateButton(this GameObject go)
	{
		return go.GetOrCreateComponent(delegate(DolocButtonComponent cmp)
		{
			cmp.SetNavigationModeNone();
			cmp.transition = Selectable.Transition.None;
		});
	}

	public static void SetSpriteState(this DolocButtonComponent button, Sprite sprite)
	{
		button.SetSpriteState(sprite, sprite, sprite, sprite);
	}

	public static void SetSpriteState(this DolocButtonComponent button, Sprite highlightedSprite, Sprite selectedSprite, Sprite pressedSprite, Sprite disabledSprite)
	{
		SpriteState spriteState = button.spriteState;
		spriteState.highlightedSprite = highlightedSprite;
		spriteState.selectedSprite = selectedSprite;
		spriteState.pressedSprite = pressedSprite;
		spriteState.disabledSprite = disabledSprite;
		button.spriteState = spriteState;
	}

	public static void SetAlpha(this Image image, float alpha)
	{
		if (!(image == null))
		{
			Color color = image.color;
			image.color = new Color(color.r, color.g, color.b, alpha);
		}
	}

	public static void RaiseUiSpriteFadeUp(this DolocIcon icon)
	{
		DolocAPI.RaiseUiSpriteFadeUp(icon.position, icon.iconSprite, 0.7f, Ease.OutExpo, 3f * icon.iconSprite.rect.height);
	}

	public static void RaiseUiSpriteFadeUp(this Image icon)
	{
		icon.RaiseUiSpriteFadeUp(icon.sprite);
	}

	public static void RaiseUiSpriteFadeDown(this Image icon)
	{
		icon.RaiseUiSpriteFadeDown(icon.sprite);
	}

	public static void RaiseUiSpriteFadeUp(this Image icon, Sprite sprite)
	{
		DolocAPI.RaiseUiSpriteFadeUp(icon.GetLeftBottom(), sprite, 0.7f, Ease.OutExpo, 3f * icon.sprite.rect.height);
	}

	public static void RaiseUiSpriteFadeDown(this Image icon, Sprite sprite)
	{
		DolocAPI.RaiseUiSpriteFadeDown(icon.GetLeftBottom(), sprite, 0.7f, Ease.OutQuart, 3f * icon.sprite.rect.height);
	}

	public static Vector2 GetLeftBottom(this Image icon)
	{
		Vector3[] array = new Vector3[4];
		icon.rectTransform.GetWorldCorners(array);
		return array[0];
	}

	public static void RollNumber(this Text textCmp, int target)
	{
		int.TryParse(textCmp.text, out var result);
		RollNumber(result, target, delegate(int value)
		{
			textCmp.text = value.ToString();
		});
	}

	public static void RollMoney(this Text textCmp, int target)
	{
		int.TryParse(textCmp.text.Replace(" G", ""), out var result);
		RollNumber(result, target, delegate(int value)
		{
			textCmp.text = value + " G";
		});
	}

	public static void RebuildNavigation(this IEnumerable<Selectable> srcSelectablesArray, Selectable[] candidates, float distanceWeight = 1f, float angleLimit = 90f, bool wrapAround = true)
	{
		foreach (Selectable item in srcSelectablesArray)
		{
			if (!item.IsInteractable())
			{
				item.SetNavigation();
				continue;
			}
			Navigation navigation = item.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			item.navigation = navigation;
			item.SetNavigationOnUp(item.FindSelectableOnUp(candidates, distanceWeight, angleLimit, wrapAround));
			item.SetNavigationOnDown(item.FindSelectableOnDown(candidates, distanceWeight, angleLimit, wrapAround));
			item.SetNavigationOnLeft(item.FindSelectableOnLeft(candidates, distanceWeight, angleLimit, wrapAround));
			item.SetNavigationOnRight(item.FindSelectableOnRight(candidates, distanceWeight, angleLimit, wrapAround));
		}
	}

	public static void RebuildNavigation(this INavPanel panel, Selectable[] candidates, float distanceWeight = 1f, float angleLimit = 90f, bool wrapAround = true)
	{
		panel.allSelectablesArray.RebuildNavigation(candidates, distanceWeight, angleLimit, wrapAround);
	}

	public static void RebuildNavigationHorizontal(this IEnumerable<Selectable> srcSelectablesArray, Selectable[] candidates, float distanceWeight = 1f, float angleLimit = 90f, bool wrapAround = true, bool clearVerticalNav = false)
	{
		foreach (Selectable item in srcSelectablesArray)
		{
			if (!item.IsInteractable())
			{
				item.SetNavigation();
				continue;
			}
			Navigation navigation = item.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			item.navigation = navigation;
			item.SetNavigationOnLeft(item.FindSelectableOnLeft(candidates, distanceWeight, angleLimit, wrapAround));
			item.SetNavigationOnRight(item.FindSelectableOnRight(candidates, distanceWeight, angleLimit, wrapAround));
			if (clearVerticalNav)
			{
				item.SetNavigationOnUp(null);
				item.SetNavigationOnDown(null);
			}
		}
	}

	public static void RebuildNavigationHorizontal(this INavPanel panel, Selectable[] candidates, float distanceWeight = 1f, float angleLimit = 90f, bool wrapAround = true, bool clearVerticalNav = false)
	{
		panel.allSelectablesArray.RebuildNavigationHorizontal(candidates, distanceWeight, angleLimit, wrapAround, clearVerticalNav);
	}

	public static void RebuildNavigationVertical(this IEnumerable<Selectable> srcSelectablesArray, Selectable[] candidates, float distanceWeight = 1f, float angleLimit = 90f, bool wrapAround = true, bool clearHorizontalNav = false)
	{
		foreach (Selectable item in srcSelectablesArray)
		{
			if (!item.IsInteractable())
			{
				item.SetNavigation();
				continue;
			}
			Navigation navigation = item.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			item.navigation = navigation;
			item.SetNavigationOnUp(item.FindSelectableOnUp(candidates, distanceWeight, angleLimit, wrapAround));
			item.SetNavigationOnDown(item.FindSelectableOnDown(candidates, distanceWeight, angleLimit, wrapAround));
			if (clearHorizontalNav)
			{
				item.SetNavigationOnLeft(null);
				item.SetNavigationOnRight(null);
			}
		}
	}

	public static void RebuildNavigationVertical(this INavPanel panel, Selectable[] candidates, float distanceWeight = 1f, float angleLimit = 90f, bool wrapAround = true, bool clearHorizontalNav = false)
	{
		panel.allSelectablesArray.RebuildNavigationVertical(candidates, distanceWeight, angleLimit, wrapAround, clearHorizontalNav);
	}

	public static void RebuildNavigationVerticalByOrder(this Selectable[] srcSelectablesArray, bool wrapAround = true)
	{
		List<Selectable> list = new List<Selectable>();
		foreach (Selectable selectable in srcSelectablesArray)
		{
			if (!selectable.IsInteractable())
			{
				selectable.SetNavigation();
			}
			else
			{
				list.Add(selectable);
			}
		}
		int count = list.Count;
		if (count == 1)
		{
			list[0].SetNavigation();
			return;
		}
		for (int j = 0; j < count; j++)
		{
			Selectable selectable2 = list[j];
			Navigation navigation = selectable2.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			selectable2.navigation = navigation;
			if (j == 0)
			{
				selectable2.SetNavigationOnUp(wrapAround ? list[count - 1] : null);
			}
			else
			{
				selectable2.SetNavigationOnUp(list[j - 1]);
			}
			if (j == count - 1)
			{
				selectable2.SetNavigationOnDown(wrapAround ? list[0] : null);
			}
			else
			{
				selectable2.SetNavigationOnDown(list[j + 1]);
			}
		}
	}

	public static void SetNavigationOnUp(this Selectable src, Selectable dst)
	{
		if (!(src == null))
		{
			Navigation navigation = src.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			navigation.selectOnUp = dst;
			src.navigation = navigation;
		}
	}

	public static void SetNavigationOnDown(this Selectable src, Selectable dst)
	{
		if (!(src == null))
		{
			Navigation navigation = src.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			navigation.selectOnDown = dst;
			src.navigation = navigation;
		}
	}

	public static void SetNavigationOnLeft(this Selectable src, Selectable dst)
	{
		if (!(src == null))
		{
			Navigation navigation = src.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			navigation.selectOnLeft = dst;
			src.navigation = navigation;
		}
	}

	public static void SetNavigationOnRight(this Selectable src, Selectable dst)
	{
		if (!(src == null))
		{
			Navigation navigation = src.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			navigation.selectOnRight = dst;
			src.navigation = navigation;
		}
	}

	public static void SetNavigation(this Selectable src, Selectable dstUp = null, Selectable dstDown = null, Selectable dstLeft = null, Selectable dstRight = null)
	{
		if (!(src == null))
		{
			Navigation navigation = src.navigation;
			navigation.mode = Navigation.Mode.Explicit;
			navigation.selectOnUp = dstUp;
			navigation.selectOnDown = dstDown;
			navigation.selectOnLeft = dstLeft;
			navigation.selectOnRight = dstRight;
			src.navigation = navigation;
		}
	}

	public static void SetNavigationModeNone(this Selectable src)
	{
		if (!(src == null))
		{
			Navigation navigation = src.navigation;
			navigation.mode = Navigation.Mode.None;
			src.navigation = navigation;
		}
	}

	public static Selectable FindSelectable(this Selectable selectable, Vector3 dir, Selectable[] candidates, float distanceWeight, float angleLimit, bool wrapAround)
	{
		if (distanceWeight < 0f)
		{
			Debug.LogError($"distanceWeight: {distanceWeight} 应大于0");
		}
		dir = dir.normalized;
		Vector3 vector = Quaternion.Inverse(selectable.transform.rotation) * dir;
		Vector3 vector2 = selectable.transform.TransformPoint(GetPointOnRectEdge(selectable.transform as RectTransform, vector));
		Vector3 vector3 = vector2 - 5000f * dir;
		float num = float.NegativeInfinity;
		float num2 = float.NegativeInfinity;
		float num3 = 0f;
		Selectable selectable2 = null;
		Selectable result = null;
		float num4 = 0f;
		float num5 = 0f;
		Vector3 vector4 = Vector3.zero;
		Vector3 vector5 = Vector3.zero;
		foreach (Selectable selectable3 in candidates)
		{
			if (selectable3 == selectable || !selectable3.IsInteractable() || !selectable3.gameObject.activeSelf || selectable3.navigation.mode == Navigation.Mode.None)
			{
				continue;
			}
			RectTransform rectTransform = selectable3.transform as RectTransform;
			Vector3 position = ((rectTransform != null) ? ((Vector3)rectTransform.rect.center) : Vector3.zero);
			Vector3 vector6 = selectable3.transform.TransformPoint(position) - vector2;
			float num6 = Vector3.Dot(dir, vector6);
			if (wrapAround && num6 < 0f)
			{
				Vector3 vector7 = selectable3.transform.TransformPoint(position) - vector3;
				float num7 = Vector3.Dot(dir, vector7);
				num3 = num7 / Mathf.Pow(vector7.magnitude, 1f + Mathf.Max(0f, distanceWeight));
				if (num3 > num2)
				{
					num2 = num3;
					result = selectable3;
					num5 = num7;
					vector5 = vector7;
				}
			}
			else if (!(num6 <= 0f))
			{
				num3 = num6 / Mathf.Pow(vector6.magnitude, 1f + Mathf.Max(0f, distanceWeight));
				if (num3 > num)
				{
					num = num3;
					selectable2 = selectable3;
					num4 = num6;
					vector4 = vector6;
				}
			}
		}
		if (wrapAround && null == selectable2)
		{
			if (Mathf.Abs(num5 / vector5.magnitude) < Mathf.Cos(Mathf.Clamp(angleLimit / 180f * 3.14159f, 0f, 90f)))
			{
				return null;
			}
			return result;
		}
		if (Mathf.Abs(num4 / vector4.magnitude) < Mathf.Cos(Mathf.Clamp(angleLimit / 180f * 3.14159f, 0f, 90f)))
		{
			return null;
		}
		return selectable2;
	}

	public static Selectable FindSelectableOnUp(this Selectable selectable, Selectable[] candidates, float distanceWeight, float angleLimit, bool wrapAround)
	{
		return selectable.FindSelectable(selectable.transform.rotation * Vector3.up, candidates, distanceWeight, angleLimit, wrapAround);
	}

	public static Selectable FindSelectableOnDown(this Selectable selectable, Selectable[] candidates, float distanceWeight, float angleLimit, bool wrapAround)
	{
		return selectable.FindSelectable(selectable.transform.rotation * Vector3.down, candidates, distanceWeight, angleLimit, wrapAround);
	}

	public static Selectable FindSelectableOnLeft(this Selectable selectable, Selectable[] candidates, float distanceWeight, float angleLimit, bool wrapAround)
	{
		return selectable.FindSelectable(selectable.transform.rotation * Vector3.left, candidates, distanceWeight, angleLimit, wrapAround);
	}

	public static Selectable FindSelectableOnRight(this Selectable selectable, Selectable[] candidates, float distanceWeight, float angleLimit, bool wrapAround)
	{
		return selectable.FindSelectable(selectable.transform.rotation * Vector3.right, candidates, distanceWeight, angleLimit, wrapAround);
	}

	private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir)
	{
		if (rect == null)
		{
			return Vector3.zero;
		}
		if (dir != Vector2.zero)
		{
			dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
		}
		dir = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);
		return dir;
	}
}
