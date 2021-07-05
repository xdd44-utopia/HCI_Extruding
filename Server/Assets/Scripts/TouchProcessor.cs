using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{

	public GameObject meshManipulator;
	public GameObject sliderController;
	public GameObject sender;
	public GameObject slicePlane;
	public GameObject touchPointMark;
	public GameObject sliceTraceVisualizer;
	public LineRenderer crossScreenLine;
	public LineRenderer cutPlaneVisualizer;
	public Text debugText;

	public GameObject[] touchMarks;


	[HideInInspector]
	public bool isLocked;
	
	//standard
	private Vector3 INF = new Vector3(10000, 10000, 10000);
 
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

	private int touchCountThisScreen = 0;
	private Vector3[] touchPosThisScreen;
	private Vector3[] touchPrevPosThisScreen;
	private TouchPhase[] touchPhaseThisScreen;
	private int touchCountOtherScreen = 0;
	private Vector3[] touchPosOtherScreen;
	private Vector3[] touchPrevPosOtherScreen;
	private TouchPhase[] touchPhaseOtherScreen;
	private float endGestureLock = 0.1f;

	//double tap
	private float doubleTapTimer = 0;
	private float doubleTapTolerance = 0.15f;
	private float doubleTapInterval = 0.05f;

	//cross-screen slice
	private float crossScreenSliceTimer = 0;
	private float crossScreenSliceTolerance = 0.15f;
	private Vector3 startSliceThisScreen;
	private Vector3 startSliceOtherScreen;
	private Vector3 endSliceThisScreen;
	private Vector3 endSliceOtherScreen;
	private float sliceMinDist = 0.5f;
	private bool slicePrepared = false;

	private Status state = Status.none;
	private enum Status {
		none,
		singleScreen1This,
		singleScreen1Other,
		singleScreen2This,
		singleScreen2Other,
		crossScreen2,
		crossScreen3
	}

	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{

		touchCountThisScreen = Input.touchCount;
		if(touchCountThisScreen > 0) {
			touchPosThisScreen = new Vector3[touchCountThisScreen];
			touchPrevPosThisScreen = new Vector3[touchCountThisScreen];
			touchPhaseThisScreen = new TouchPhase[touchCountThisScreen];
			for (int i=0;i<touchCountThisScreen;i++) {
				Touch tch = Input.touches[i];
				touchPosThisScreen[i] = tch.position;
				touchPosThisScreen[i] -= new Vector3(360, 772, 0);
				touchPosThisScreen[i] *= Camera.main.orthographicSize / 772;
				touchPrevPosThisScreen[i] = tch.position - tch.deltaPosition;
				touchPrevPosThisScreen[i] -= new Vector3(360, 772, 0);
				touchPrevPosThisScreen[i] *= Camera.main.orthographicSize / 772;
				touchPhaseThisScreen[i] = tch.phase;
			}
		}

		visualize();
		calculate();

		if (touchTimer > 0) {
			switch (state) {
				case Status.singleScreen2This:
					meshManipulator.GetComponent<MeshManipulator>().startTransform(panThisScreen, 0, turnThisScreen, true);
					meshManipulator.GetComponent<MeshManipulator>().updateTaperScale(pinchDelta);
					break;
				case Status.singleScreen2Other:
					meshManipulator.GetComponent<MeshManipulator>().startTransform(panOtherScreen, 0, turnOtherScreen, false);
					break;
				case Status.crossScreen3:
					meshManipulator.GetComponent<MeshManipulator>().startTransform(Vector3.zero, pinchDelta, 0, false);
					break;
			}
		}
		if (touchTimerOtherScreen <= 0) {
			touchCountOtherScreen = 0;
		}
		cutPlaneVisualizer.enabled = (crossScreenSliceTimer >= 0 && slicePrepared);
		if (crossScreenSliceTimer <= 0 && slicePrepared) {
			meshManipulator.GetComponent<MeshManipulator>().executeSlice();
			startSliceThisScreen = Vector3.zero;
			startSliceOtherScreen = Vector3.zero;
			endSliceThisScreen = Vector3.zero;
			endSliceOtherScreen = Vector3.zero;
			slicePlane.GetComponent<SliceController>().locked = false;
			slicePrepared = false;
		}

		touchTimer -= Time.deltaTime;
		touchTimerOtherScreen -= Time.deltaTime;
		doubleTapTimer -= Time.deltaTime;
		crossScreenSliceTimer -= Time.deltaTime;
		endGestureLock -= Time.deltaTime;
	}
	private void visualize() {
		for (int i=0;i<touchCountThisScreen;i++) {
			touchMarks[i].transform.position = touchPosThisScreen[i];
		}
		for (int i=touchCountThisScreen;i<4;i++) {
			touchMarks[i].transform.position = INF;
		}
		if (touchCountThisScreen == 1 && touchCountOtherScreen == 1) {
			crossScreenLine.SetPosition(0, touchPosThisScreen[0]);
			crossScreenLine.SetPosition(1, touchPosOtherScreen[0]);
		}
		else {
			crossScreenLine.SetPosition(0, INF);
			crossScreenLine.SetPosition(1, INF);
		}
	}
	private void calculate () {
		dragDelta = 0;
		panThisScreen = new Vector3(0, 0, 0);
		panOtherScreen = new Vector3(0, 0, 0);
		pinchDelta = 0;
		// meshManipulator.GetComponent<MeshManipulator>().touchPosition = INF;

		if (touchCountThisScreen == 0 && touchCountOtherScreen == 0) {
			state = Status.none;
		}
		else if (touchCountThisScreen == 1 && touchCountOtherScreen == 0) {
			state = Status.singleScreen1This;
		}
		else if (touchCountThisScreen == 0 && touchCountOtherScreen == 1) {
			state = Status.singleScreen1Other;
		}
		else if (touchCountThisScreen == 2 && touchCountOtherScreen == 0) {
			state = Status.singleScreen2This;
		}
		else if (touchCountThisScreen == 0 && touchCountOtherScreen == 2) {
			state = Status.singleScreen2Other;
		}
		else if (touchCountThisScreen == 1 && touchCountOtherScreen == 1) {
			state = Status.crossScreen2;
		}
		else if ((touchCountThisScreen == 1 && touchCountOtherScreen == 2) || (touchCountThisScreen == 2 && touchCountOtherScreen == 1)) {
			state = Status.crossScreen3;
		}

		if (state != Status.none) {
			touchTimer = touchDelayTolerance;
		}

		switch (state) {
			case Status.crossScreen3: {
				
				float pinchStart = (touchPrevPosThisScreen[0] - touchPrevPosOtherScreen[0]).magnitude;
				float pinchEnd = (touchPosThisScreen[0] - touchPosOtherScreen[0]).magnitude;

				pinchDelta = (pinchEnd - pinchStart);

				if (Mathf.Abs(pinchDelta) > minPinchDistance) {
					pinchDelta *= 1;
				}
				else {
					pinchDelta = 0;
				}
				break;
			}
			case Status.singleScreen2This: {
				Vector3 panStart = (touchPrevPosThisScreen[0] + touchPrevPosThisScreen[1]) / 2;
				Vector3 panEnd = (touchPosThisScreen[0] + touchPosThisScreen[1]) / 2;

				panThisScreen = panEnd - panStart;

				if (panThisScreen.magnitude > minPanDistance) {
					panThisScreen *= 1;
				}
				else {
					panThisScreen = new Vector3(0, 0, 0);
				}

				Vector3 startVec = touchPrevPosThisScreen[0] - touchPrevPosThisScreen[1];
				Vector3 endVec = touchPosThisScreen[0] - touchPosThisScreen[1];
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
				
				float pinchStart = (touchPrevPosThisScreen[0] - touchPrevPosThisScreen[1]).magnitude;
				float pinchEnd = (touchPosThisScreen[0] - touchPosThisScreen[1]).magnitude;

				pinchDelta = (pinchEnd - pinchStart);

				if (Mathf.Abs(pinchDelta) > minPinchDistance) {
					pinchDelta *= 1;
				}
				else {
					pinchDelta = 0;
				}

				endGestureLock = 0.1f;
				break;
			}
			case Status.singleScreen2Other: {
				Vector3 panStart = (touchPrevPosOtherScreen[0] + touchPrevPosOtherScreen[1]) / 2;
				Vector3 panEnd = (touchPosOtherScreen[0] + touchPosOtherScreen[1]) / 2;

				panOtherScreen = panEnd - panStart;

				if (panOtherScreen.magnitude > minPanDistance) {
					panOtherScreen *= 1;
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
				endGestureLock = 0.1f;
				break;
			}
			case Status.crossScreen2: {
				if (crossScreenSliceTimer <= 0) {
					startSliceThisScreen = touchPosThisScreen[0];
					startSliceOtherScreen = touchPosOtherScreen[0];
				}
				else {
					endSliceThisScreen = touchPosThisScreen[0];
					endSliceOtherScreen = touchPosOtherScreen[0];
					sliceTraceVisualizer.GetComponent<SliceTraceVisualizer>().touchPointThisScreen = endSliceThisScreen;
					sliceTraceVisualizer.GetComponent<SliceTraceVisualizer>().touchPointOtherScreen = endSliceOtherScreen;
					processCrossScreenSlice();
					visualizeCrossScreenSlice();
				}
				crossScreenSliceTimer = crossScreenSliceTolerance;
				endGestureLock = 0.1f;
				break;
			}
			case Status.singleScreen1This: {
				if (touchPosThisScreen[0].y > -3.2 && touchPosThisScreen[0].y < 4.2 && touchPhaseThisScreen[0] == TouchPhase.Ended && endGestureLock < 0) {
					meshManipulator.GetComponent<MeshManipulator>().touchPosition = touchPosThisScreen[0];
					meshManipulator.GetComponent<MeshManipulator>().castRay();
					touchPointMark.transform.position = touchPosThisScreen[0];

					if (doubleTapTolerance - doubleTapTimer > doubleTapInterval && doubleTapTimer > 0) {
						meshManipulator.GetComponent<MeshManipulator>().startFocus();
					}
					doubleTapTimer = doubleTapTolerance;

					float currentDist = Vector2.Distance(touchPosThisScreen[0], dragStartPoint);
					float prevDist = Vector2.Distance(touchPrevPosThisScreen[0], dragStartPoint);
					dragDelta = currentDist - prevDist;

					if (Mathf.Abs(dragDelta) > minDragDist) {
						dragDelta *= dragRatio;
					} else {
						dragDelta = 0;
					}
				}
				break;
			}
			case Status.singleScreen1Other: {
				if (touchPosOtherScreen[0].y > -3.2 && touchPosOtherScreen[0].y < 4.2 && touchPhaseOtherScreen[0] == TouchPhase.Ended && endGestureLock < 0) {
					meshManipulator.GetComponent<MeshManipulator>().touchPosition = touchPosOtherScreen[0];
					meshManipulator.GetComponent<MeshManipulator>().castRay();
					touchPointMark.transform.position = touchPosOtherScreen[0];

					if (doubleTapTolerance - doubleTapTimer > doubleTapInterval && doubleTapTimer > 0) {
						meshManipulator.GetComponent<MeshManipulator>().startSecondaryFocus();
					}
					doubleTapTimer = doubleTapTolerance;
					
					float currentDist = Vector2.Distance(touchPosOtherScreen[0], dragStartPoint);
					float prevDist = Vector2.Distance(touchPrevPosOtherScreen[0], dragStartPoint);
					dragDelta = currentDist - prevDist;

					if (Mathf.Abs(dragDelta) > minDragDist) {
						dragDelta *= dragRatio;
					} else {
						dragDelta = 0;
					}
				}
				break;
			}
		}
	}

	private void processCrossScreenSlice() {

		if ((endSliceThisScreen - startSliceThisScreen).magnitude < sliceMinDist || (endSliceOtherScreen - startSliceOtherScreen).magnitude < sliceMinDist) {
			return;
		}

		Vector3 centerPos = (startSliceThisScreen + startSliceOtherScreen) / 2;
		Vector3 normal = crossProduct(centerPos - endSliceOtherScreen, centerPos - endSliceThisScreen);

		if (normal.z < 0) {
			normal = -normal;
		}

		Vector3 axisToFocus = crossProduct(normal, new Vector3(0, -1, 0));
		float angleToFocus = -Vector3.Angle(normal, new Vector3(0, -1, 0));

		Quaternion originRotation = Quaternion.identity;
		originRotation.eulerAngles = new Vector3(0, -1, 0);
		slicePlane.GetComponent<SliceController>().locked = true;
		slicePlane.transform.position = centerPos;
		slicePlane.transform.rotation = Quaternion.AngleAxis(angleToFocus, axisToFocus) * originRotation;
		slicePrepared = true;
		meshManipulator.GetComponent<MeshManipulator>().startSlice();

	}

	private void visualizeCrossScreenSlice() {
		float angle = sliderController.GetComponent<SliderController>().angle;
		Vector3 centerPos = (startSliceThisScreen + startSliceOtherScreen) / 2;
		Vector3 startThis = intersectLinePlane(centerPos, centerPos + endSliceThisScreen - endSliceOtherScreen, endSliceThisScreen, new Vector3(0, 0, 1));
		Vector3 startOther = intersectLinePlane(centerPos, centerPos + endSliceThisScreen - endSliceOtherScreen, endSliceOtherScreen, new Vector3(Mathf.Sin(-angle), 0, -Mathf.Cos(angle)));
		cutPlaneVisualizer.SetPosition(0, endSliceThisScreen);
		cutPlaneVisualizer.SetPosition(1, startThis);
		cutPlaneVisualizer.SetPosition(2, startOther);
		cutPlaneVisualizer.SetPosition(3, endSliceOtherScreen);

		string msg = "Cutting\n";
		msg += endSliceThisScreen.x + "," + endSliceThisScreen.y + "," + endSliceThisScreen.z + "\n";
		msg += endSliceOtherScreen.x + "," + endSliceOtherScreen.y + "," + endSliceOtherScreen.z + "\n";
		msg += startThis.x + "," + startThis.y + "," + startThis.z + "\n";
		msg += startOther.x + "," + startOther.y + "," + startOther.z + "\n";
		sender.GetComponent<ServerController>().sendMessage(msg);
	}
	private Vector3 crossProduct(Vector3 a, Vector3 b) {
		return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}
	private float dotProduct(Vector3 a, Vector3 b) {
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	private Vector3 intersectLinePlane(Vector3 a, Vector3 b, Vector3 p, Vector3 n) { //line passes a and b, plane passes p with normal n
		float t = (dotProduct(p, n) - dotProduct(a, n)) / dotProduct(n, a - b);
		return a + t * (a - b);
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
	
	public void updateTouchPoint(int touchCount, Vector3[] touchPos, Vector3[] touchPrevPos, TouchPhase[] phases) {
		touchTimerOtherScreen = touchDelayTolerance;
		touchCountOtherScreen = touchCount;
		touchPosOtherScreen = new Vector3[touchCount];
		touchPrevPosOtherScreen = new Vector3[touchCount];

		for (int i=0;i<touchCountOtherScreen;i++) {
			touchPosOtherScreen[i] = touchPos[i];
			touchPrevPosOtherScreen[i] = touchPrevPos[i];
			touchPhaseOtherScreen[i] = phases[i];
		}
	}
	
}
