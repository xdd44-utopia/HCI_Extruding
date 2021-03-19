using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{
	public GameObject sender;
	public GameObject obj;

	public Text panText;
	public Text pinchText;
	public Text angleText;

	private string address = "192.168.0.106";
	//Macbook local connecting to iPhone hotspot: 172.20.10.2
	//Samsung connecting to iPhone hotspot: 172.20.10.6
	//Samsung connecting to xdd44's wifi: 192.168.0.106
	//Macbook local connecting to xdd44's wifi: 192.168.0.101
	//iPhone connecting to iPhone hotspot: 10.150.153.190

	private Phase phase;
	private bool isScaleDone = false;


	[HideInInspector]
	public float verticalScale = 2.5f;
	[HideInInspector]
	public float planarScale = 2.5f;
	[HideInInspector]
	public Vector3 pos;
	[HideInInspector]
	public Quaternion rot;
	private Vector3 defaultPos;
	private Quaternion defaultRot;
	private Vector3 prevTouch;
	private Vector3 startTouch;
	private float prevAngle;
	private bool lastFrameTouching = false;
	private bool lastFrameTwoPointTouching = false;
	private float moveSensitive = 1f;
	private float scaleSensitive = 2f;
	private float rotateSensitive = Mathf.PI / 180;
	
	//standard
	private const float pinchTurnRatio = Mathf.PI / 2;
	private const float minTurnAngle = 0;
 
	private const float pinchRatio = 1;
	private const float minPinchDistance = 0;
 
	private const float panRatio = 1;
	private const float minPanDistance = 0;
 
	private float turnAngleDelta;
	private float turnAngle;
 
	private float pinchDistanceDelta;
	private float pinchDistance;

	private Vector3 panDelta;


	void Start()
	{
		phase = Phase.freemove;
		pos = new Vector3(100, 100, 0);
		rot = Quaternion.Euler(0, 0, 0);
		defaultPos = new Vector3(100, 100, 0);
		defaultRot = Quaternion.Euler(0, 0, 0);
		verticalScale = 0.01f;
		planarScale = 0f;
	}

	// Update is called once per frame
	void Update()
	{
		calculate();

		panText.text = "Pan: " + panDelta;
		pinchText.text = "Pinch: " + pinchDistanceDelta;
		angleText.text = "Angle: " + turnAngleDelta;
		

		if (isScaleDone && planarScale <= 0.01f) {
			isScaleDone = false;
		}
		switch (phase) {
			case Phase.scale:
				scaling();
				break;
			case Phase.freemove:
				freemoving();
				break;
		}
	}

	private void scaling() {
		
		sender.GetComponent<ClientController>().sendMessage();
	}

	private void freemoving() {
		bool refreshed = false;
		if (turnAngleDelta > minTurnAngle) {
			rotate(turnAngleDelta);
			refreshed = true;
		}
		if (panDelta.magnitude > minPanDistance) {
			pos += moveSensitive * panDelta;
			refreshed = true;
		}
		if (refreshed) {
			sender.GetComponent<ClientController>().sendMessage();
		}
	}

	private Vector2 processTouchPoint(Vector2 v) {
		v.x -= 360;
		v.y -= 772;
		v *= Camera.main.orthographicSize / 772;
		return v;
	}

	private float processAngle(Vector2 v1, Vector2 v2) {
		Vector2 t = v1 - v2;
		float angle = Vector2.Angle(new Vector2(1, 0), t);
		if (t.y < 0) {
			angle = -angle;
		}
		return angle;
	}

	private void rotate(float angle) {
		Vector3 axisWorld = new Vector3(0, 0, 1);
		axisWorld = Quaternion.Inverse(rot) * axisWorld;
		Quaternion rotChange = new Quaternion(
			axisWorld.x * Mathf.Sin(angle/2),
			axisWorld.y * Mathf.Sin(angle/2),
			axisWorld.z * Mathf.Sin(angle/2),
			Mathf.Cos(angle/2)
		);
		rot *= rotChange;
	}
 
	private void calculate () {
		pinchDistance = pinchDistanceDelta = 0;
		turnAngle = turnAngleDelta = 0;
		panDelta = new Vector3(0, 0, 0);
 
		// if two fingers are touching the screen at the same time ...
		if (Input.touchCount == 2) {
			Touch touch1 = Input.touches[0];
			Touch touch2 = Input.touches[1];
 
			// ... if at least one of them moved ...
			if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) {
				// ... check the delta distance between them ...
				pinchDistance = Vector2.Distance(touch1.position, touch2.position);
				float prevDistance = Vector2.Distance(touch1.position - touch1.deltaPosition,
				                                      touch2.position - touch2.deltaPosition);
				pinchDistanceDelta = pinchDistance - prevDistance;
 
				// ... if it's greater than a minimum threshold, it's a pinch!
				if (Mathf.Abs(pinchDistanceDelta) > minPinchDistance) {
					pinchDistanceDelta *= pinchRatio;
				} else {
					pinchDistance = pinchDistanceDelta = 0;
				}
 
				// ... or check the delta angle between them ...
				turnAngle = Angle(touch1.position, touch2.position);
				float prevTurn = Angle(touch1.position - touch1.deltaPosition,
				                       touch2.position - touch2.deltaPosition);
				turnAngleDelta = Mathf.DeltaAngle(prevTurn, turnAngle);
 
				// ... if it's greater than a minimum threshold, it's a turn!
				if (Mathf.Abs(turnAngleDelta) > minTurnAngle) {
					turnAngleDelta *= pinchTurnRatio;
				} else {
					turnAngle = turnAngleDelta = 0;
				}
			}
		}
		else if (Input.touchCount == 1) {
			Touch touch1 = Input.touches[0];

			if (touch1.phase == TouchPhase.Moved) {
				panDelta = touch1.deltaPosition;

				if (panDelta.magnitude > minPanDistance) {
					panDelta *= panRatio;
				}
				else {
					panDelta = new Vector3(0, 0, 0);
				}
			}

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

	public void switchMode() {
		if (phase == Phase.scale) {
			phase = Phase.freemove;
		}
		else {
			phase = Phase.scale;
			verticalScale = 0.01f;
			sender.GetComponent<ClientController>().sendMessage();
		}
	}

	public void resetAll() {
		pos = defaultPos;
		rot = defaultRot;
		phase = Phase.freemove;
		isScaleDone = false;
		lastFrameTouching = false;
		verticalScale = 0.01f;
		planarScale = 0f;
		sender.GetComponent<ClientController>().ConnectToTcpServer(address);
		sender.GetComponent<ClientController>().sendMessage();
	}

	private enum Phase {
		scale,
		freemove
	}
}
