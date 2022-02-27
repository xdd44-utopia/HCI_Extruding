using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{

	public GameObject sender;
	public GameObject sliderController;
	public GameObject[] touchMarks;
	
	//standard

	private float angle = - Mathf.PI / 2;
	private float camWidth;
	private float camHeight;
	private int touchCountThisScreen = 0;
	private Vector3[] touchPosThisScreen;
	private Vector3[] touchPrevPosThisScreen;


	void Start()
	{
		Camera cam = Camera.main;
		camHeight = 10;
		camWidth = camHeight * cam.aspect;
	}

	void Update()
	{

		angle = sliderController.GetComponent<SliderController>().angle;
		
		touchCountThisScreen = Input.touchCount;
		if(touchCountThisScreen > 0) {
			touchPosThisScreen = new Vector3[touchCountThisScreen];
			touchPrevPosThisScreen = new Vector3[touchCountThisScreen];
			for (int i=0;i<touchCountThisScreen;i++) {
				Touch tch = Input.touches[i];
				touchPosThisScreen[i] = tch.position;
				touchPosThisScreen[i] -= new Vector3(Screen.width / 2, Screen.height / 2, 0);
				touchPosThisScreen[i] *= Camera.main.orthographicSize / (Screen.height / 2);
				touchPrevPosThisScreen[i] = tch.position - tch.deltaPosition;
				touchPrevPosThisScreen[i] -= new Vector3(Screen.width / 2, Screen.height / 2, 0);
				touchPrevPosThisScreen[i] *= Camera.main.orthographicSize / (Screen.height / 2);
			}
		}

		for (int i=0;i<touchCountThisScreen;i++) {
			touchMarks[i].transform.position = touchPosThisScreen[i];
		}
		for (int i=touchCountThisScreen;i<4;i++) {
			touchMarks[i].transform.position = new Vector3(100, 100, 100);
		}

		if (touchCountThisScreen > 0) {
			string msg = "Touch\n" + touchCountThisScreen + "\n";
			for (int i=0;i<touchCountThisScreen;i++) {
				Vector3 currPos = convertToServer(touchPosThisScreen[i]);
				Vector3 prevPos = convertToServer(touchPrevPosThisScreen[i]);
				msg += currPos.x + "," + currPos.y + "," + currPos.z + "," + prevPos.x + "," + prevPos.y + "," + prevPos.z + "\n";
			}
			for (int i=0;i<touchCountThisScreen;i++) {
				switch (Input.touches[i].phase) {
					case TouchPhase.Began:
						msg += "B\n";
						break;
					case TouchPhase.Moved:
						msg += "M\n";
						break;
					case TouchPhase.Stationary:
						msg += "S\n";
						break;
					case TouchPhase.Ended:
						msg += "E\n";
						break;
					case TouchPhase.Canceled:
						msg += "C\n";
						break;
				}
			}
			sender.GetComponent<ClientController>().sendMessage(msg);
		}
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
}
