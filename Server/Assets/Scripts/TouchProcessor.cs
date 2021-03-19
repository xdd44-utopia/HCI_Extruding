using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{
	public GameObject sender;
	public GameObject obj;

	private Phase phase;
	private float sendTimer = -1;

	[HideInInspector]
	public bool isScaling;
	[HideInInspector]
	public bool isPanning;
	[HideInInspector]
	public bool isLocked;
	private float panTimer;
	private float scaleTimer;
	private float panDelay = 0.1f;

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
	
	//standard
 
	private const float panRatio = 1;
	private const float minPanDistance = 0;

	private const float dragRatio = 0.01f;
	private const float minDragDist = 0;

	private Vector3 panDelta;

	private float dragDelta;
	private Vector3 dragStartPoint;


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

		if (sendTimer < 0) {
			sender.GetComponent<ServerController>().sendMessage();
			sendTimer = 0.05f;
		}
		else {
			sendTimer -= Time.deltaTime;
		}

		isLocked = isPanning || isScaling;
	}

	private void freemoving() {
		bool refreshed = false;
		if (panDelta.magnitude > minPanDistance) {
			pos += panDelta;
			isPanning = true;
			panTimer = panDelay;
		}
		if (Mathf.Abs(dragDelta) > minDragDist) {
			planarScale += dragDelta;
			isScaling = true;
			scaleTimer = panDelay;
		}

		isPanning = (panTimer > 0);
		panTimer -= Time.deltaTime;
		isScaling = (scaleTimer > 0);
		scaleTimer -= Time.deltaTime;
	}
 
	private void calculate () {
		dragDelta = 0;
		panDelta = new Vector3(0, 0, 0);
 
		if (Input.touchCount == 2) {
			Touch touch1 = Input.touches[0];
			Touch touch2 = Input.touches[1];
 
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
		else if (Input.touchCount == 1) {
			Touch touch1 = Input.touches[0];

			if (touch1.position.y < 1250) {
				if (touch1.phase == TouchPhase.Began) {
					Vector3 tp = processTouchPoint(touch1.position);
					pos = new Vector3(tp.x, tp.y, 0);
					pos.x = tp.x;
					pos.y = tp.y;
					dragStartPoint = touch1.position;
					rot = 0;
					planarScale = 0;
					verticalScale = 0.01f;
					isScaling = true;
					scaleTimer = panDelay;
				}
				else if (touch1.phase == TouchPhase.Moved) {
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
