namespace DolocTown;

public abstract class MonsterAI_Group : MonsterAI
{
	protected MonsterGroup group { get; private set; }

	public virtual void OnAddedToGroup(MonsterGroup group)
	{
		this.group = group;
	}

	public override void OnDead()
	{
		group.Remove(this);
	}
}
