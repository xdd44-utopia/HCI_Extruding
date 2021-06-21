using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
	public GameObject sender;
	public GameObject objectManager;
	public Text debugText;

	[HideInInspector]
	public Vector3 acceOther;

	[HideInInspector]
	public float angle;
	private float prevAngle = -1;

	private float defaultAngle = - Mathf.PI / 3;
	private const float minAngle = - Mathf.PI / 2;
	private const float maxAngle = 0;

	private bool isActivated = true;
	// Start is called before the first frame update
	void Start()
	{
		angle = defaultAngle;
	}

	// Update is called once per frame
	void Update()
	{

		Vector3 acceThis = Input.acceleration;
		acceOther.y = 0;
		acceThis.y = 0;
		float angleTemp = Vector3.Angle(acceThis, acceOther);

		angleTemp = - angleTemp * Mathf.PI / 180;

		if (isActivated) {
			angle = Mathf.Lerp(angle, angleTemp, Time.deltaTime * 3);
		}
		else {
			angle = - Mathf.PI / 2;
		}
		angle = angle > minAngle ? angle : minAngle;
		angle = angle < maxAngle ? angle : maxAngle;

		angle *= 180 / Mathf.PI;
		angle = Mathf.Round(angle);
		angle *= Mathf.PI / 180;

		//angle = - Mathf.PI / 2;

		if (angle != prevAngle) {
			sender.GetComponent<ServerController>().sendMessage("Angle\n" + angle + "\n");
			objectManager.GetComponent<ObjectManager>().angleChanged();
		}
		prevAngle = angle;
	}
}
