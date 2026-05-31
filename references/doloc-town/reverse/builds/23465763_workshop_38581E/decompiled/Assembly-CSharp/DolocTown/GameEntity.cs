namespace DolocTown;

public abstract class GameEntity : DolocRecyclableObject
{
	public virtual void PostSoundEventByEnum(SoundEvents soundEvent)
	{
		DolocAPI.Sound.PostSoundEvent(soundEvent, base.gameObject);
	}

	public virtual void PostSoundEvent(string soundEvent)
	{
		DolocAPI.Sound.PostSoundEvent(soundEvent, base.gameObject);
	}
}
