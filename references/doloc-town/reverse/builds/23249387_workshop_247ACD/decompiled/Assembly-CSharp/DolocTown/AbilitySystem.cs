namespace DolocTown;

public class AbilitySystem
{
	public MotionAbility motionAbility { get; protected set; }

	public CollectionAbility collectionAbility { get; protected set; }

	public RecorveryAbility recorveryAbility { get; protected set; }

	public BattleAbility battleAbility { get; private set; }

	public StateAbility stateAbility { get; private set; }

	public AbilitySystem(IMotionParam baseMotionParam)
	{
		motionAbility = new MotionAbility(baseMotionParam);
		collectionAbility = new CollectionAbility();
		recorveryAbility = new RecorveryAbility();
		battleAbility = new BattleAbility();
		stateAbility = new StateAbility();
	}

	public void Recalculate()
	{
		motionAbility.Clear();
		collectionAbility.Clear();
		recorveryAbility.Clear();
		battleAbility.Clear();
		stateAbility.Clear();
	}
}
