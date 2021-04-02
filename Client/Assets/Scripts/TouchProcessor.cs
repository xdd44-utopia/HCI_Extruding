using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{
	public GameObject sender;
	public GameObject obj;
	public Slider extrudeSlider;

	[HideInInspector]
	public bool isExtruding;
	[HideInInspector]
	public bool isPanning;
	[HideInInspector]
	public bool isLocked;
	private float panTimer;
	private float panDelay = 0.1f;
	[HideInInspector]
	public bool isRotating;

	public Text panText;
	public Text pinchText;
	public Text angleText;

	private Phase phase;
	private float sendTimer = -1;

	[HideInInspector]
	public float verticalScale = 2.5f;
	[HideInInspector]
	public float planarScale = 2.5f;
	[HideInInspector]
	public Vector3 pos;
	[HideInInspector]
	public float rot;
	private Vector3 defaultPos;
	private float defaultRot;

	private const float maxPlanarScale = 5;
	
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

	[HideInInspector]
	public float extrudeDelta;

	void Start()
	{
		phase = Phase.freemove;
		pos = new Vector3(100, 100, 0);
		rot = 0;
		defaultPos = new Vector3(100, 100, 0);
		defaultRot = 0;
		verticalScale = 0.01f;
		planarScale = 0f;
	}

	// Update is called once per frame
	void Update()
	{
		calculate();

		switch (phase) {
			case Phase.freemove:
				freemoving();
				break;
		}

		panText.text = "Rotating " + isRotating;
		pinchText.text = "Extruding " + isExtruding;

		isLocked = isPanning || isExtruding || isRotating;

		if (sendTimer < 0) {
			//sender.GetComponent<ClientController>().sendMessage();
			sendTimer = 0.05f;
		}
		else {
			sendTimer -= Time.deltaTime;
		}

	}

	private void freemoving() {
		if (Mathf.Abs(turnAngleDelta) > minTurnAngle) {
			rot += turnAngleDelta;
		}
		if (panDelta.magnitude > minPanDistance) {
			pos += panDelta;
			panTimer = panDelay;
		}
		if (isExtruding) {
			verticalScale = maxPlanarScale * extrudeSlider.value;
		}

		isPanning = (panTimer > 0);
		panTimer -= Time.deltaTime;
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

				Vector3 panStart = (touch1.position - touch1.deltaPosition + touch2.position - touch2.deltaPosition) / 2;
				Vector3 panEnd = (touch1.position + touch2.position) / 2;

				panDelta = panEnd - panStart;

				if (panDelta.magnitude > minPanDistance) {
					panDelta *= Camera.main.orthographicSize / 772;
				}
				else {
					panDelta = new Vector3(0, 0, 0);
				}
			}
		}
		else if (Input.touchCount == 1 && !isExtruding) {
			Touch touch1 = Input.touches[0];
			if (touch1.phase == TouchPhase.Moved) {
				turnAngle = Angle(touch1.position, new Vector2(360, 772));
				float prevTurn = Angle(touch1.position - touch1.deltaPosition, new Vector2(360, 772));
				turnAngleDelta = Mathf.DeltaAngle(prevTurn, turnAngle);
				if (Mathf.Abs(turnAngleDelta) > minTurnAngle) {
					turnAngleDelta *= pinchTurnRatio;
				} else {
					turnAngle = turnAngleDelta = 0;
				}
			}
			else if (touch1.phase == TouchPhase.Began) {
				isRotating = true;
			}
			else if (touch1.phase == TouchPhase.Ended) {
				isRotating = false;
			}
		}

		if (isExtruding || isPanning) {
			isRotating = false;
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

	public void startExtruding() {
		isExtruding = true;
		planarScale = 0;
	}

	public void endExtruding() {
		isExtruding = false;
		extrudeSlider.value = 0;
	}

	public void resetAll() {
		pos = defaultPos;
		rot = defaultRot;
		verticalScale = 0.01f;
		planarScale = 0f;
	}

	private enum Phase {
		scale,
		freemove
	}
}
