using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{

	public GameObject meshManipulator;

	private Phase phase;

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

	private float doubleTapTimer = 0;
	private float doubleTapTolerance = 0.2f;


	void Start()
	{
		phase = Phase.freemove;
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

		touchTimer -= Time.deltaTime;
		doubleTapTimer -= Time.deltaTime;

	}

	private void freemoving() {
		if (touchTimer > 0) {
			meshManipulator.GetComponent<MeshManipulator>().startTransform(panDelta, pinchDelta, turnDelta, true);
		}
		// if (Mathf.Abs(dragDelta) > minDragDist) {

		// }

	}
 
	private void calculate () {
		dragDelta = 0;
		panDelta = new Vector3(0, 0, 0);
		pinchDelta = 0;
		meshManipulator.GetComponent<MeshManipulator>().touchPosition = new Vector3(10000, 10000, 10000);
 
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
				meshManipulator.GetComponent<MeshManipulator>().touchPosition = touch1.position;
				if (touch1.phase == TouchPhase.Began) {
					if (doubleTapTimer > 0) {
						meshManipulator.GetComponent<MeshManipulator>().startFocus();
					}
					doubleTapTimer = doubleTapTolerance;
				}
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
		
	}

	private enum Phase {
		scale,
		freemove
	}
}
