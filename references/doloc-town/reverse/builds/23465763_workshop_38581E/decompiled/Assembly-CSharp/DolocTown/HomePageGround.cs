using UnityEngine;

namespace DolocTown;

public class HomePageGround : MonoBehaviour
{
	[SerializeField]
	private float leftBorder;

	[SerializeField]
	private float moveSpeed;

	private float XPosition
	{
		get
		{
			return base.transform.position.x;
		}
		set
		{
			Vector3 position = base.transform.position;
			position.x = value;
			base.transform.position = position;
		}
	}

	private void Start()
	{
		XPosition = 0f;
	}

	private void Update()
	{
		if (XPosition <= leftBorder)
		{
			XPosition = 0f;
		}
		else
		{
			XPosition -= moveSpeed * Time.deltaTime;
		}
	}
}
