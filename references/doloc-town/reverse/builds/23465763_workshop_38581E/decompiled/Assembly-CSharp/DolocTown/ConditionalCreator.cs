using UnityEngine;

namespace DolocTown;

public class ConditionalCreator : MonoBehaviour, IConditionValidator
{
	[SerializeField]
	private GameObject prefab;

	[SerializeField]
	private ConditionGroupChecker conditionChecker;

	private GameObject entity;

	private void Start()
	{
		ValidateCondition();
	}

	public void ValidateCondition()
	{
		if (!(prefab == null))
		{
			TextTipConfig conditionFailedText;
			bool flag = conditionChecker.IsConditionMet(out conditionFailedText);
			if (flag && entity == null)
			{
				DolocAPI.output("准备创建预制体:" + prefab.name);
				entity = Object.Instantiate(prefab, base.transform);
				entity.transform.localPosition = Vector3.zero;
				entity.GetComponent<DolocObject>()?.Init();
			}
			else if (!flag && entity != null)
			{
				Object.Destroy(entity.gameObject);
				entity = null;
			}
		}
	}
}
