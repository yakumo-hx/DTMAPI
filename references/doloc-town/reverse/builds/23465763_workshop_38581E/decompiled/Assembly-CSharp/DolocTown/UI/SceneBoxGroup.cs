using DolocTown.Config.Localization;
using UnityEngine;

namespace DolocTown.UI;

public class SceneBoxGroup : DolocUIPanel
{
	[SerializeField]
	private SceneTextBox sceneTextBox;

	[SerializeField]
	private CropInfoBox cropInfoBox;

	[SerializeField]
	private AnimalInfoBox animalInfoBox;

	private SceneBoxBase[] sceneBoxes;

	private SceneBoxBase currentSceneBox;

	private Transform followedTransform;

	private Vector2 offset;

	private bool shouldFollow;

	private float startTime;

	private float showDuration;

	private IHasCropInfo currentPlantBasin;

	private Animal currentAnimal;

	public override bool redoDisplayAnimation => false;

	public override bool activeAllWidgetOnShow => false;

	protected override void __Init()
	{
		base.__Init();
		sceneBoxes = new SceneBoxBase[3] { sceneTextBox, cropInfoBox, animalInfoBox };
		SceneBoxBase[] array = sceneBoxes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Init();
		}
		currentSceneBox = sceneTextBox;
		HideAll();
	}

	public void RenderAndShow(Vector2 worldPos, SceneTextBoxArgs data, Transform followedTrans, float duration)
	{
		HideAll();
		showDuration = duration;
		RefreshStartTime();
		HoverTo(sceneTextBox, worldPos, followedTrans);
		sceneTextBox.Render(data);
		ShowBox();
	}

	public void RenderAndShow(float worldX, Animal animal, float duration)
	{
		AnimalBaseInfoData animalBaseInfoData = new AnimalBaseInfoData(animal);
		if (!animalBaseInfoData.notEmpty || animal.Renderer == null)
		{
			Hide();
			return;
		}
		showDuration = duration;
		RefreshStartTime();
		if (currentAnimal == animal && currentSceneBox == animalInfoBox)
		{
			HoverTo(animalInfoBox, new Vector2(worldX, DolocAPI.agent.PositionHeadTop.y), animal.Renderer.transform);
			UpdateAnimalInfo();
		}
		else
		{
			HideAll();
			currentAnimal = animal;
			HoverTo(animalInfoBox, new Vector2(worldX, DolocAPI.agent.PositionHeadTop.y), animal.Renderer.transform);
			animalInfoBox.Render(animalBaseInfoData);
		}
		ShowBox();
	}

	public void RenderAndShow(float worldX, IHasCropInfo plantBasin, float duration)
	{
		if (!plantBasin.HasCrop)
		{
			return;
		}
		CropInfoData data = new CropInfoData(plantBasin);
		if (!data.notEmpty)
		{
			Hide();
			return;
		}
		showDuration = duration;
		RefreshStartTime();
		if (currentPlantBasin == plantBasin && currentSceneBox == cropInfoBox)
		{
			HoverTo(cropInfoBox, GetWorldPosition(cropInfoBox, worldX), null);
			UpdateCropInfo();
		}
		else
		{
			HideAll();
			currentPlantBasin = plantBasin;
			HoverTo(cropInfoBox, GetWorldPosition(cropInfoBox, worldX), null);
			cropInfoBox.Render(data);
		}
		ShowBox();
	}

	private void ShowBox()
	{
		Show();
		currentSceneBox.SetVisible(value: true);
	}

	private void HideAll()
	{
		SceneBoxBase[] array = sceneBoxes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetVisible(value: false);
		}
	}

	private Vector2 GetWorldPosition(SceneBoxBase sceneBox, float worldX)
	{
		Vector2 pos = DolocAPI.WorldToScreen(new Vector2(worldX, DolocAPI.agent.PositionHeadTop.y));
		pos = sceneBox.GetAdaptionPosition(pos);
		return DolocAPI.ScreenToWorld(pos);
	}

	private void HoverTo(SceneBoxBase sceneBox, Vector2 worldPos, Transform targetTrans)
	{
		followedTransform = targetTrans;
		offset = worldPos;
		shouldFollow = followedTransform != null;
		if (shouldFollow)
		{
			offset -= (Vector2)targetTrans.position;
		}
		currentSceneBox = sceneBox;
		currentSceneBox.transform.position = UpdateScreenPos();
		currentSceneBox.SetVisible(value: true);
	}

	private void RefreshStartTime()
	{
		startTime = Time.time;
	}

	private Vector2 UpdateScreenPos()
	{
		if (!shouldFollow)
		{
			return DolocAPI.WorldToScreen(offset);
		}
		return DolocAPI.WorldToScreen((Vector2)followedTransform.position + offset);
	}

	private void LateUpdate()
	{
		if (base.isRender)
		{
			if (currentSceneBox.isHover)
			{
				RefreshStartTime();
			}
			if (Time.time - startTime > showDuration && !currentSceneBox.isHover)
			{
				Hide();
			}
			else
			{
				currentSceneBox.transform.position = UpdateScreenPos();
			}
		}
	}

	public void TryUpdateInfo()
	{
		if (base.isRender)
		{
			UpdateCropInfo();
			UpdateAnimalInfo();
		}
	}

	public void UpdateCropInfo()
	{
		if (!cropInfoBox.isVisible || currentPlantBasin == null)
		{
			return;
		}
		if (!currentPlantBasin.HasCrop)
		{
			cropInfoBox.SetVisible(value: false);
			return;
		}
		CropInfoData data = new CropInfoData(currentPlantBasin);
		if (!data.notEmpty)
		{
			cropInfoBox.SetVisible(value: false);
		}
		else
		{
			cropInfoBox.Render(data);
		}
	}

	public void UpdateAnimalInfo()
	{
		if (!animalInfoBox.isVisible || currentAnimal == null)
		{
			return;
		}
		if (!currentAnimal.IsRender)
		{
			animalInfoBox.SetVisible(value: false);
			return;
		}
		AnimalBaseInfoData animalBaseInfoData = new AnimalBaseInfoData(currentAnimal);
		if (!animalBaseInfoData.notEmpty)
		{
			animalInfoBox.SetVisible(value: false);
		}
		else
		{
			animalInfoBox.Render(animalBaseInfoData);
		}
	}
}
