using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
	private GameObject sender;
	private Text debugText;
	private LineRenderer screenSide;

	[HideInInspector]
	public Vector3 acceOther;

	[HideInInspector]
	public float angle;
	private float prevAngle = - Mathf.PI / 3;

	private float defaultAngle = - Mathf.PI / 3;

	// Start is called before the first frame update
	void Start()
	{

		sender = GameObject.Find("Server");
		debugText = GameObject.Find("Debug").GetComponent<Text>();
		screenSide = GameObject.Find("SideViewSceen").GetComponent<LineRenderer>();

		angle = defaultAngle;
	}

	// Update is called once per frame
	void Update()
	{

		Vector3 acceThis = Input.acceleration;
		acceOther.y = 0;
		acceThis.y = 0;
		float angleTemp = (acceOther.x > acceThis.x ? Vector3.Angle(acceThis, acceOther) : 0);
		angleTemp = - angleTemp * Mathf.PI / 180;
		angle = Mathf.Clamp(Mathf.Round(Mathf.Lerp(angle, angleTemp, Time.deltaTime * 3) * 1000) / 1000, - Mathf.PI / 2, 0);
		angle = Mathf.Lerp(prevAngle, angle, 0.4f);

		// angle = - Mathf.PI / 2 + 0.05f;

		screenSide.SetPosition(2, new Vector3(VectorCalculator.camWidth / 2 + VectorCalculator.camWidth * Mathf.Cos(-angle), - VectorCalculator.camHeight / 2, VectorCalculator.camWidth * Mathf.Sin(-angle)));

		if (Mathf.Abs(angle - prevAngle) > 0.0025f) {
			sender.GetComponent<ServerController>().sendMessage("Angle\n" + angle + "\n");
			prevAngle = angle;
		}
		VectorCalculator.angle = angle;
		
	}
}
