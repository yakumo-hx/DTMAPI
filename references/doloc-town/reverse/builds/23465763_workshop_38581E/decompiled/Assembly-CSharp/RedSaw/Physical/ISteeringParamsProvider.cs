namespace RedSaw.Physical;

public interface ISteeringParamsProvider
{
	float MaxSpeed { get; }

	float ParkingRadius { get; }

	float ResponseDuration { get; }
}
