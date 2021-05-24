using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{

	public GameObject meshManipulator;
	public Text debugText;


	[HideInInspector]
	public bool isLocked;
	
	//standard
 
	private const float panRatio = 1;
	private const float minPanDistance = 0;

	private const float dragRatio = 0.01f;
	private const float minDragDist = 0;

	private Vector3 panThisScreen;
	private Vector3 panOtherScreen;
	private float pinchDelta;
	private float turnThisScreen;
	private float turnOtherScreen;
	private const float minPinchDistance = 0;
	private const float minTurnDistance = 0.05f;

	private float dragDelta;
	private Vector3 dragStartPoint;

	private float touchTimer = 0;
	private float touchTimerOtherScreen = 0;
	private float touchDelayTolerance = 0.1f;

	private int touchCountOtherScreen = 0;
	private Vector3[] touchPosOtherScreen;
	private Vector3[] touchPrevPosOtherScreen;

	private float doubleTapTimer = 0;
	private float doubleTapTolerance = 0.2f;


	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		calculate();

		if (touchTimer > 0) {
			if (Input.touchCount == 2) {
				meshManipulator.GetComponent<MeshManipulator>().startTransform(panThisScreen, 0, turnThisScreen, true);
			}
			else if (touchCountOtherScreen == 2) {
				meshManipulator.GetComponent<MeshManipulator>().startTransform(panOtherScreen, 0, turnOtherScreen, false);
			}
			else if (Input.touchCount + touchCountOtherScreen == 2) {
				meshManipulator.GetComponent<MeshManipulator>().startTransform(Vector3.zero, pinchDelta, 0, true);
			}
		}
		if (touchTimerOtherScreen <= 0) {
			touchCountOtherScreen = 0;
		}

		touchTimer -= Time.deltaTime;
		touchTimerOtherScreen -= Time.deltaTime;
		doubleTapTimer -= Time.deltaTime;

	}
	private void calculate () {
		dragDelta = 0;
		panThisScreen = new Vector3(0, 0, 0);
		panOtherScreen = new Vector3(0, 0, 0);
		pinchDelta = 0;
		meshManipulator.GetComponent<MeshManipulator>().touchPosition = new Vector3(10000, 10000, 10000);

		if (Input.touchCount == 2) {
			Touch touch1 = Input.touches[0];
			Touch touch2 = Input.touches[1];
 
			if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) {

				touchTimer = touchDelayTolerance;

				Vector3 panStart = (touch1.position - touch1.deltaPosition + touch2.position - touch2.deltaPosition) / 2;
				Vector3 panEnd = (touch1.position + touch2.position) / 2;

				panThisScreen = panEnd - panStart;

				if (panThisScreen.magnitude > minPanDistance) {
					panThisScreen *= Camera.main.orthographicSize / 772;
				}
				else {
					panThisScreen = new Vector3(0, 0, 0);
				}

				Vector3 startVec = (touch1.position - touch1.deltaPosition) - (touch2.position - touch2.deltaPosition);
				Vector3 endVec = touch1.position - touch2.position;
				float turnStart = Vector3.Angle(new Vector3(1, 0, 0), startVec);
				turnStart = (startVec.y >= 0 ? turnStart : -turnStart);
				float turnEnd = Vector3.Angle(new Vector3(1, 0, 0), endVec);
				turnEnd = (endVec.y >= 0 ? turnEnd : -turnEnd);

				turnThisScreen = turnEnd - turnStart;

				if (Mathf.Abs(turnThisScreen) > minTurnDistance) {
					turnThisScreen *= 1;
				}
				else {
					turnThisScreen = 0;
				}
			}
		}
		else if (touchCountOtherScreen == 2) {

			touchTimer = touchDelayTolerance;

			Vector3 panStart = (touchPrevPosOtherScreen[0] + touchPrevPosOtherScreen[1]) / 2;
			Vector3 panEnd = (touchPosOtherScreen[0] + touchPosOtherScreen[1]) / 2;

			panOtherScreen = panEnd - panStart;

			if (panOtherScreen.magnitude > minPanDistance) {
				panOtherScreen *= Camera.main.orthographicSize / 772;
			}
			else {
				panOtherScreen = new Vector3(0, 0, 0);
			}

			Vector3 startVec = touchPrevPosOtherScreen[0] - touchPrevPosOtherScreen[1];
			Vector3 endVec = touchPosOtherScreen[0] - touchPosOtherScreen[1];
			Vector3 norm = crossProduct(startVec, endVec);
			Vector3 para = new Vector3(-norm.z, 0, norm.x);
			if (norm.x < 0) {
				para = -para;
			}
			float turnStart = Vector3.Angle(para, startVec);
			turnStart = (startVec.y >= 0 ? turnStart : -turnStart);
			float turnEnd = Vector3.Angle(para, endVec);
			turnEnd = (endVec.y >= 0 ? turnEnd : -turnEnd);

			turnOtherScreen = turnEnd - turnStart;

			if (Mathf.Abs(turnOtherScreen) > minTurnDistance) {
				turnOtherScreen *= 1;
			}
			else {
				turnOtherScreen = 0;
			}

		}
		else if (Input.touchCount + touchCountOtherScreen == 2) {

			touchTimer = touchDelayTolerance;
			
			Touch touch1 = Input.touches[0];
			Vector3 touchPrevPosThisScreen = touch1.position - touch1.deltaPosition;
			Vector3 touchPosThisScreen = touch1.position;
			float pinchStart = (touchPrevPosThisScreen - touchPrevPosOtherScreen[0]).magnitude;
			float pinchEnd = (touchPosThisScreen - touchPosOtherScreen[0]).magnitude;

			pinchDelta = (pinchEnd - pinchStart);

			if (Mathf.Abs(pinchDelta) > minPinchDistance) {
				pinchDelta  *= Camera.main.orthographicSize / 772;
			}
			else {
				pinchDelta = 0;
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
	private Vector3 crossProduct(Vector3 a, Vector3 b) {
		return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
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
	
	public void updateTouchPoint(int touchCount, Vector3[] touchPos, Vector3[] touchPrevPos) {
		touchTimerOtherScreen = touchDelayTolerance;
		touchCountOtherScreen = touchCount;
		touchPosOtherScreen = new Vector3[touchCount];
		touchPrevPosOtherScreen = new Vector3[touchCount];

		for (int i=0;i<touchCountOtherScreen;i++) {
			touchPosOtherScreen[i] = touchPos[i];
			touchPrevPosOtherScreen[i] = touchPrevPos[i];
			touchPosOtherScreen[i] *= 772 / Camera.main.orthographicSize;
			touchPrevPosOtherScreen[i] *= 772 / Camera.main.orthographicSize;
		}
	}
	
}
