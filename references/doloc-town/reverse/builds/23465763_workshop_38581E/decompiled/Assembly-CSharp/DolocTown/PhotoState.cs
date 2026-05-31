using System;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using DolocTown.Config;
using DolocTown.UI;
using RedSaw;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown;

public class PhotoState : DolocTownGameStateBase
{
	private readonly List<Material> _materials = new List<Material>();

	private readonly OperationTipInUI _operationTip;

	private readonly RSTimer timerDelay = new RSTimer();

	private int _filterIdx;

	private bool _isTakingPhoto;

	private bool _isPhotoStateReady;

	private static string Filepath
	{
		get
		{
			string path = "doloctown_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
			folderPath = Path.Combine(folderPath, "DolocTown");
			if (!Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}
			return Path.Combine(folderPath, path);
		}
	}

	public static void Enter()
	{
		new PhotoState(DolocAPI.userInput).Startup();
	}

	private static OperationTipInUI SetupOperationUI()
	{
		OperationTipInUI operationTipInUI = DolocGameAssets.UI_ENTITY_OPERATION_TEXT.CreateEntity<OperationTipInUI>(DolocAPI.uiSystem.rectTransform);
		RectTransform component = operationTipInUI.GetComponent<RectTransform>();
		component.pivot = new Vector2(0f, 1f);
		component.anchorMin = new Vector2(0f, 0f);
		component.anchorMax = new Vector2(1f, 1f);
		component.anchoredPosition = new Vector2(32f, -32f);
		Image component2 = operationTipInUI.GetComponent<Image>();
		component2.color = component2.color.Alpha(0.46f);
		operationTipInUI.GetComponent<LayoutGroup>().padding = new RectOffset(18, 18, 18, 18);
		operationTipInUI.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;
		string textKey = DolocConfig.StaticTexts.UiTipPrevFilter + "\n" + DolocConfig.StaticTexts.UiTipNextFilter + "\n" + DolocConfig.StaticTexts.UiTipCapture + "\n" + DolocConfig.StaticTexts.UiTipQuit;
		operationTipInUI.SetTextKey(textKey);
		return operationTipInUI;
	}

	private static void ApplyFilter(Material material)
	{
		if (material == null)
		{
			DolocAPI.ppm.SetEnabled(PPTypes.CUSTOMFILTER, value: false);
			return;
		}
		DolocAPI.ppm.SetEnabled(PPTypes.CUSTOMFILTER, value: true);
		DolocAPI.ppm.ApplyCustomFilter(material);
	}

	public PhotoState(GameStateMachine userInput)
		: base(userInput, DolocInputType.BASE, shouldPauseGame: true, shouldLateUpdate: false, supportCutscenes: false)
	{
		_filterIdx = 0;
		_materials.Add(null);
		_materials.Add(LocMaterials.GAME_MAT_PP_OLD_PHOTO);
		_materials.Add(LocMaterials.GAME_MAT_PP_TELEVISION);
		_materials.Add(LocMaterials.GAME_MAT_PP_TELEVISION_GREY_SCALE);
		_materials.Add(LocMaterials.GAME_MAT_PP_GREY_SCALE);
		_materials.Add(LocMaterials.GAME_MAT_PP_SIMPLE_BLUR);
		_materials.Add(LocMaterials.GAME_MAT_PP_NIGHTVISION);
		_operationTip = SetupOperationUI();
	}

	private void MoveToLastFilter()
	{
		_filterIdx--;
		if (_filterIdx < 0)
		{
			_filterIdx = _materials.Count - 1;
		}
		ApplyFilter(_materials[_filterIdx]);
	}

	private void MoveToNextFilter()
	{
		_filterIdx++;
		if (_filterIdx >= _materials.Count)
		{
			_filterIdx = 0;
		}
		ApplyFilter(_materials[_filterIdx]);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		DolocAPI.uiSystem.SetPhotoMode(value: true);
		DolocAPI.ppm.DisableResidentFilter();
		DolocAPI.ppm.SetEnabled(PPTypes.CUSTOMFILTER, value: true);
		DolocAPI.ppm.SetEnabled(PPTypes.CIRCLEDIFFUSION, value: true);
		DolocAPI.ppm.CircleDiffusion.value = 1f;
		ApplyFilter(_materials[_filterIdx]);
	}

	public override void OnExit()
	{
		base.OnExit();
		DolocAPI.uiSystem.SetPhotoMode(value: false);
		DolocAPI.ppm.SetEnabled(PPTypes.CUSTOMFILTER, value: false);
		DolocAPI.ppm.SetEnabled(PPTypes.CIRCLEDIFFUSION, value: false);
		DolocAPI.ppm.ResumeResidentFilter();
		UnityEngine.Object.Destroy(_operationTip.gameObject);
	}

	public override void OnUpdate(float deltaTime)
	{
		if (!_isPhotoStateReady && timerDelay.Tick(deltaTime))
		{
			_isPhotoStateReady = true;
		}
		if (userInput.BaseIsCancelPressed)
		{
			DolocAPI.userInput.PopState();
		}
		else if (userInput.BaseIsLeftPressed)
		{
			MoveToLastFilter();
		}
		else if (userInput.BaseIsRightPressed)
		{
			MoveToNextFilter();
		}
		else if (_isPhotoStateReady && userInput.BaseIsConfirmPressed && !_isTakingPhoto)
		{
			_isTakingPhoto = true;
			DolocAPI.ppm.CircleDiffusion.PlayBack(0.6f, 0.3f, Ease.OutExpo, Ease.Linear, delegate
			{
				_isTakingPhoto = false;
			});
			string filepath = Filepath;
			DolocAPI.CaptureScreen(filepath);
			DolocAPI.ShowMessageBox(LocSprites.UI_INFOICON_ATTENTION, DolocUtils.Format(DolocConfig.StaticTexts.UiTipPhotoSaving, filepath));
		}
	}
}
