namespace RedSaw;

public interface IHasIndex
{
	int index { get; set; }

	bool isDeserializationValid { get; }
}
