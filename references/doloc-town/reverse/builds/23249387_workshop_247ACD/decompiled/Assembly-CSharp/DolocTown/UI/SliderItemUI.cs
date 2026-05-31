using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class SliderItemUI : SettingValueItemUI<int>
{
	[SerializeField]
	private DolocSliderComponent slider;

	[SerializeField]
	private Text txtMinValue;

	[SerializeField]
	private Text txtCurrentValue;

	private int minValue;

	private int maxValue;

	private float valueScale;

	public override Selectable selectable => slider;

	public override int currentValue => DenormalizeValue(slider.value);

	private float NormalizeValue(int value)
	{
		return ((float)value - (float)minValue) / (float)(maxValue - minValue);
	}

	private int DenormalizeValue(float value)
	{
		return Mathf.RoundToInt((float)minValue + (float)(maxValue - minValue) * value);
	}

	protected override void __Init()
	{
		base.__Init();
		slider.minValue = 0f;
		slider.maxValue = 1f;
		slider.SetValueWithoutNotify(0f);
		slider.onValueChanged.AddListener(OnSliderValueChanged);
	}

	private void OnSliderValueChanged(float normalizedValue)
	{
		int num = DenormalizeValue(normalizedValue);
		onValueChanged.Invoke(num);
		txtCurrentValue.text = (valueScale * (float)num).ToString("0.##");
	}

	public void InitValue(int currentValue, int minValue, int maxValue, float valueScale)
	{
		this.minValue = minValue;
		this.maxValue = maxValue;
		this.valueScale = valueScale;
		txtMinValue.text = (valueScale * (float)minValue).ToString("0.##");
		currentValue = Mathf.Clamp(currentValue, minValue, maxValue);
		slider.SetValueWithoutNotify(NormalizeValue(currentValue));
		txtCurrentValue.text = (valueScale * (float)currentValue).ToString("0.##");
	}
}
