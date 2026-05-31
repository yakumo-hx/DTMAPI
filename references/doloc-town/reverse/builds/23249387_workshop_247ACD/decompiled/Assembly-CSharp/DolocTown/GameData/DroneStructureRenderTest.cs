using UnityEngine;
using UnityEngine.Serialization;

namespace DolocTown.GameData;

public class DroneStructureRenderTest : MonoBehaviour
{
	[SerializeField]
	private DroneStructDebugSO[] droneStructSOs;

	[FormerlySerializedAs("droneStructSO")]
	[SerializeField]
	private DroneStructDebugSO droneStructDebugSo;

	[SerializeField]
	private SpriteRenderer moduleSpriteRenderer;

	[SerializeField]
	private Sprite[] moduleSprites;

	private int weaponIndex;

	[SerializeField]
	private SpriteRenderer engineSpriteRenderer;

	[SerializeField]
	private Sprite[] engineSprites;

	private int engineIndex;

	[SerializeField]
	private SpriteRenderer assistSpriteRenderer;

	[SerializeField]
	private Sprite[] assistSprites;

	private int assistIndex;

	private void OnDroneStructureChanged()
	{
		if (droneStructDebugSo != null)
		{
			GetComponent<SpriteRenderer>().sprite = droneStructDebugSo.SceneSprite;
		}
		SetWeapon(weaponIndex);
		SetEngine(engineIndex);
		SetAssist(assistIndex);
	}

	public void RemoveWeapon()
	{
		moduleSpriteRenderer.sprite = null;
	}

	private void SetWeapon(int weaponIndex)
	{
		if (weaponIndex >= 0 && weaponIndex < moduleSprites.Length)
		{
			this.weaponIndex = weaponIndex;
			Sprite sprite = moduleSprites[weaponIndex];
			DroneStructDebugSlot droneStructDebugSlot = droneStructDebugSo.Slots[0];
			moduleSpriteRenderer.sprite = sprite;
			moduleSpriteRenderer.transform.localPosition = droneStructDebugSlot.VisibleSuitableOffset;
		}
	}

	public void Install_1()
	{
		SetWeapon(0);
	}

	public void Install_2()
	{
		SetWeapon(1);
	}

	public void Install_3()
	{
		SetWeapon(2);
	}

	public void Install_4()
	{
		SetWeapon(3);
	}

	public void Install_5()
	{
		SetWeapon(4);
	}

	public void RemoveEngine()
	{
		engineSpriteRenderer.sprite = null;
	}

	private void SetEngine(int engineIndex)
	{
		if (engineIndex >= 0 && engineIndex < engineSprites.Length)
		{
			this.engineIndex = engineIndex;
			Sprite sprite = engineSprites[engineIndex];
			DroneStructDebugSlot droneStructDebugSlot = droneStructDebugSo.Slots[1];
			engineSpriteRenderer.sprite = sprite;
			engineSpriteRenderer.transform.localPosition = droneStructDebugSlot.VisibleSuitableOffset;
		}
	}

	private void InstallEngine_1()
	{
		SetEngine(0);
	}

	private void InstallEngine_2()
	{
		SetEngine(1);
	}

	public void RemoveAssist()
	{
		assistSpriteRenderer.sprite = null;
	}

	private void SetAssist(int assistIndex)
	{
		if (assistIndex >= 0 && assistIndex < assistSprites.Length)
		{
			this.assistIndex = assistIndex;
			Sprite sprite = assistSprites[assistIndex];
			DroneStructDebugSlot droneStructDebugSlot = droneStructDebugSo.Slots[2];
			assistSpriteRenderer.sprite = sprite;
			assistSpriteRenderer.transform.localPosition = droneStructDebugSlot.VisibleSuitableOffset;
		}
	}

	private void InstallAssist_1()
	{
		SetAssist(0);
	}

	private void InstallAssist_2()
	{
		SetAssist(1);
	}

	private void RandomDrone()
	{
		if (!droneStructSOs.IsNullOrEmpty())
		{
			droneStructDebugSo = droneStructSOs[Random.Range(0, droneStructSOs.Length)];
			GetComponent<SpriteRenderer>().sprite = droneStructDebugSo.SceneSprite;
			SetWeapon(Random.Range(0, moduleSprites.Length));
			SetEngine(Random.Range(0, engineSprites.Length));
			SetAssist(Random.Range(0, assistSprites.Length));
		}
	}
}
