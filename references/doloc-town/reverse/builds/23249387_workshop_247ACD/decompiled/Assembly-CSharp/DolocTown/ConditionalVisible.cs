using UnityEngine;

namespace DolocTown;

public class ConditionalVisible : MonoBehaviour, IConditionValidator
{
	[SerializeField]
	private GameObject target;

	[SerializeField]
	private ConditionGroupChecker conditionChecker;

	private void Start()
	{
		ValidateCondition();
	}

	public void ValidateCondition()
	{
		if (!(target == null))
		{
			target.SetActive(conditionChecker.IsConditionMet(out var _));
		}
	}
}
