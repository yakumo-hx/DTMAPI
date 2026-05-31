namespace DolocTown;

public interface IAffectorReceiver
{
	void Affect(IAffector affector);

	void AffectNoRender(IAffector affector);
}
