using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{

	public GameObject sender;
	public Slider extrudeSlider;
	
	//standard

	private float angle = - Mathf.PI / 2;
	private float camWidth;
	private float camHeight;

	//extrude
	private bool isExtruding = false;
	private float verticalScale;
	private float maxVerticalScale = 1;


	void Start()
	{
		Camera cam = Camera.main;
		camHeight = 10;
		camWidth = camHeight * cam.aspect;
	}

	void Update()
	{

		angle = GameObject.Find("Angles").GetComponent<SliderController>().angle;

		if (Input.touchCount > 0) {
			string msg = "Touch\n" + Input.touchCount + "\n";
			for (int i=0;i<Input.touchCount;i++) {
				Touch tch = Input.touches[i];
				Vector3 currPos = convertToServer(tch.position * Camera.main.orthographicSize / 772);
				Vector3 prevPos = convertToServer((tch.position - tch.deltaPosition) * Camera.main.orthographicSize / 772);
				msg += currPos.x + "," + currPos.y + "," + currPos.z + "," + prevPos.x + "," + prevPos.y + "," + prevPos.z + "\n";
			}
			sender.GetComponent<ClientController>().sendMessage(msg);
		}
	}

	private Vector3 processTouchPoint(Vector3 v) {
		v.x -= 360;
		v.y -= 772;
		v *= Camera.main.orthographicSize / 772;
		return v;
	}
 
	private float Angle (Vector2 pos1, Vector2 pos2) {
		Vector2 from = pos2 - pos1;
		Vector2 to = new Vector2(1, 0);
 
		float result = Vector2.Angle( from, to );
		Vector3 cross = Vector3.Cross( from, to );
 
		if (cross.z > 0) {
			result = 360f - result;
		}
 
		return result;
	}

	private Vector3 convertToServer(Vector3 v) {
		Vector3 origin = new Vector3(- camWidth / 2 - camWidth * Mathf.Cos(angle) / 2, 0, - camWidth * Mathf.Sin(angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
		Vector3 z = new Vector3(-Mathf.Cos(Mathf.PI / 2 - angle), 0, Mathf.Sin(Mathf.PI / 2 - angle));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	private float multXZ(Vector3 from, Vector3 to) {
		return from.x * to.x + from.z * to.z;
	}
	
	public void startExtruding() {
		isExtruding = true;
		verticalScale = 0;
	}

	public void endExtruding() {
		isExtruding = false;
		extrudeSlider.value = 0;
	}
}
