namespace RedSaw.Physical;

public class FloatingModel2D
{
	private readonly float _springConstant;

	private readonly float _damping;

	private float _velocity;

	private float _acceleration;

	public FloatingModel2D(float springConstant, float damping)
	{
		_springConstant = springConstant;
		_damping = damping;
	}

	public float Update(float offset, float dt)
	{
		_acceleration = offset * _springConstant;
		_acceleration -= _velocity * _damping;
		_velocity += _acceleration * dt;
		return _velocity * dt;
	}

	public void Reset()
	{
		_velocity = 0f;
		_acceleration = 0f;
	}
}
