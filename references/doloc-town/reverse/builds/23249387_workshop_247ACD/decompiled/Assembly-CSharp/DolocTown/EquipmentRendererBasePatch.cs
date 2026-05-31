using UnityEngine;

namespace DolocTown;

public static class EquipmentRendererBasePatch
{
	public static LampRenderer GetLampRenderer(this EquipmentRenderer R)
	{
		return R.GetRenderComponent<LampRenderer>();
	}

	public static ProgressBarRenderer GetProgressBarRenderer(this EquipmentRenderer R)
	{
		if (R == null)
		{
			return null;
		}
		return R.GetRenderComponent<ProgressBarRenderer>();
	}

	public static void RemoveProgressBarRenderer(this EquipmentRenderer R)
	{
		if (!(R == null))
		{
			R.RemoveRenderComponent<ProgressBarRenderer>();
		}
	}

	public static void UpdateProgressBarRenderer(this EquipmentRenderer R, float value, Vector2 pos)
	{
		if (!(R == null))
		{
			ProgressBarRenderer progressBarRenderer = R.GetProgressBarRenderer();
			progressBarRenderer.SetVisible(value > 0f);
			if (progressBarRenderer.isVisible)
			{
				progressBarRenderer.position2d = pos;
				progressBarRenderer.Progress = value;
			}
		}
	}

	public static void UpdateProgressBarRendererOnlyVisible(this EquipmentRenderer R, float value, Vector2 pos)
	{
		if (R == null)
		{
			return;
		}
		ProgressBarRenderer progressBarRenderer = R.FetchRenderComponent<ProgressBarRenderer>();
		if (!(progressBarRenderer == null))
		{
			progressBarRenderer.SetVisible(value > 0f);
			if (progressBarRenderer.isVisible)
			{
				progressBarRenderer.position2d = pos;
				progressBarRenderer.Progress = value;
			}
		}
	}

	public static void UpdateProgressBarRenderer(this EquipmentRenderer R, float value)
	{
		if (!(R == null))
		{
			ProgressBarRenderer progressBarRenderer = R.GetProgressBarRenderer();
			progressBarRenderer.SetVisible(value > 0f);
			if (progressBarRenderer.isVisible)
			{
				progressBarRenderer.Progress = value;
			}
		}
	}

	public static EquipmentStateRenderer GetStateRenderer(this EquipmentRenderer R)
	{
		return R.GetRenderComponent<EquipmentStateRenderer>();
	}

	public static EquipmentStateRenderer FetchStateRenderer(this EquipmentRenderer R)
	{
		return R.FetchRenderComponent<EquipmentStateRenderer>();
	}

	public static void RemoveStateRenderer(this EquipmentRenderer R)
	{
		R.RemoveRenderComponent<EquipmentStateRenderer>();
	}

	public static void HideStateRenderer(this EquipmentRenderer R)
	{
		EquipmentStateRenderer equipmentStateRenderer = R.FetchStateRenderer();
		if (equipmentStateRenderer != null)
		{
			equipmentStateRenderer.Hide();
		}
	}

	public static void SetStateRendererStatus(this EquipmentRenderer R, bool state)
	{
		if (R.Equipment != null)
		{
			EquipmentStateRenderer stateRenderer = R.GetStateRenderer();
			stateRenderer.position2d = R.Equipment.PositionCenter;
			stateRenderer.SetState(state);
		}
	}

	public static void SetStateRendererStatus(this EquipmentRenderer R, bool state, Vector2 pos)
	{
		if (R.Equipment != null)
		{
			EquipmentStateRenderer stateRenderer = R.GetStateRenderer();
			stateRenderer.SetState(state);
			stateRenderer.position2d = pos;
		}
	}

	public static void RenderTreeCrop(this EquipmentRenderer R, TreeCrop crop, Vector3 positionWS)
	{
		TreeCropRenderer renderComponent = R.GetRenderComponent<TreeCropRenderer>();
		if (!(renderComponent == null))
		{
			crop.Renderer = renderComponent;
			renderComponent.crop = crop;
			renderComponent.sprite = crop.CurrentSprite;
			renderComponent.position = positionWS;
			crop.OnRender();
		}
	}
}
