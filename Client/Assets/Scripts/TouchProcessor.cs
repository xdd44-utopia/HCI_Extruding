using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{

	public GameObject sender;
	public Slider extrudeSlider;

	[HideInInspector]
	public bool isLocked;
	
	//standard
 
	private const float panRatio = 1;
	private const float minPanDistance = 0;

	private const float dragRatio = 0.01f;
	private const float minDragDist = 0;

	private Vector3 panDelta;
	private float pinchDelta;
	private float turnDelta;
	private const float minPinchDistance = 0;
	private const float minTurnDistance = 0.05f;

	private float dragDelta;
	private Vector3 dragStartPoint;

	private float touchTimer = 0;
	private float touchDelayTolerance = 0.1f;

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

	// Update is called once per frame
	void Update()
	{
		angle = GameObject.Find("Angles").GetComponent<SliderController>().angle;
		calculate();

		freemoving();

		touchTimer -= Time.deltaTime;

	}

	private void freemoving() {
		
		if (touchTimer > 0) {
			Vector3 panConverted = convertToServer(panDelta);
			string msg = "Transform\n" + panDelta.x + "," + panDelta.y + "," + panDelta.z + "\n";
			msg += pinchDelta + "\n";
			msg += (-turnDelta) + "\n";
			sender.GetComponent<ClientController>().sendMessage(msg);
		}
		if (isExtruding) {
			verticalScale = maxVerticalScale * extrudeSlider.value;
			string msg = "Extruding\n" + verticalScale;
			sender.GetComponent<ClientController>().sendMessage(msg);
		}
		// if (Mathf.Abs(dragDelta) > minDragDist) {

		// }

	}
 
	private void calculate () {
		dragDelta = 0;
		panDelta = new Vector3(0, 0, 0);
		pinchDelta = 0;
 
		if (Input.touchCount == 2) {
			Touch touch1 = Input.touches[0];
			Touch touch2 = Input.touches[1];
 
			if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) {

				touchTimer = touchDelayTolerance;

				Vector3 panStart = (touch1.position - touch1.deltaPosition + touch2.position - touch2.deltaPosition) / 2;
				Vector3 panEnd = (touch1.position + touch2.position) / 2;

				panDelta = panEnd - panStart;

				if (panDelta.magnitude > minPanDistance) {
					panDelta *= Camera.main.orthographicSize / 772;
				}
				else {
					panDelta = new Vector3(0, 0, 0);
				}

				float pinchStart = ((touch1.position - touch1.deltaPosition) - (touch2.position - touch2.deltaPosition)).magnitude;
				float pinchEnd = (touch1.position - touch2.position).magnitude;

				pinchDelta = pinchEnd - pinchStart;

				if (Mathf.Abs(pinchDelta) > minPinchDistance) {
					pinchDelta  *= Camera.main.orthographicSize / 772;
				}
				else {
					pinchDelta = 0;
				}

				Vector3 startVec = (touch1.position - touch1.deltaPosition) - (touch2.position - touch2.deltaPosition);
				Vector3 endVec = touch1.position - touch2.position;
				float turnStart = Vector3.Angle(new Vector3(1, 0, 0), startVec);
				turnStart = (startVec.y >= 0 ? turnStart : -turnStart);
				float turnEnd = Vector3.Angle(new Vector3(1, 0, 0), endVec);
				turnEnd = (endVec.y >= 0 ? turnEnd : -turnEnd);

				turnDelta = turnEnd - turnStart;

				if (Mathf.Abs(turnDelta) > minTurnDistance) {
					turnDelta *= 1;
				}
				else {
					turnDelta = 0;
				}
			}
		}
		else if (Input.touchCount == 1) {
			Touch touch1 = Input.touches[0];
			
			if (touch1.position.y > 200 && touch1.position.y < 1400) {
				if (touch1.phase == TouchPhase.Moved) {
					float currentDist = Vector2.Distance(touch1.position, dragStartPoint);
					float prevDist = Vector2.Distance(touch1.position - touch1.deltaPosition, dragStartPoint);
					dragDelta = currentDist - prevDist;

					if (Mathf.Abs(dragDelta) > minDragDist) {
						dragDelta *= dragRatio;
					} else {
						dragDelta = 0;
					}
				}
			}

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

	public void resetAll() {
		Debug.Log("233");
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
