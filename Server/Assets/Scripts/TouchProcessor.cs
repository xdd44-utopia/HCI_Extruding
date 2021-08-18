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
	public GameObject sliceTraceVisualizer;
	public LineRenderer crossScreenLine;
	public LineRenderer cutPlaneVisualizer;
	public Text debugText;
	public Text touchText;

	public GameObject[] touchMarks;


	[HideInInspector]
	public bool isLocked;
	
	//standard
	private static Vector3 INF {
		get {
			return new Vector3(10000, 10000, 10000);
		}
	}
 
	private const float panRatio = 1;

	private float dragDelta;
	private Vector3 dragStartPoint;
	private const float dragRatio = 0.01f;

	private Vector3 panThisScreen;
	private Vector3 panOtherScreen;
	private float pinchDelta;
	private float turnThisScreen;
	private float turnOtherScreen;

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
	private float tapTimerThisScreen = 0;
	private float tapTimerOtherScreen = 0;
	private float tapDurationTolerance = 0.2f;

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
		crossScreen1This2Other,
		crossScreen2This1Other
	}

	private float angle;

	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		angle = sliderController.GetComponent<SliderController>().angle;

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
				case Status.singleScreen1This:
					meshManipulator.GetComponent<MeshManipulator>().startMoving(panThisScreen, true);
					meshManipulator.GetComponent<MeshManipulator>().updateExtrudeScale(dragDelta, true);
					break;
				case Status.singleScreen1Other:
					meshManipulator.GetComponent<MeshManipulator>().startMoving(panOtherScreen, false);
					meshManipulator.GetComponent<MeshManipulator>().updateExtrudeScale(dragDelta, false);
					break;
				case Status.singleScreen2This:
					meshManipulator.GetComponent<MeshManipulator>().startRotating(turnThisScreen, true);
					meshManipulator.GetComponent<MeshManipulator>().startScaling(pinchDelta, true);
					// meshManipulator.GetComponent<MeshManipulator>().updateTaperScale(pinchDelta);
					break;
				case Status.singleScreen2Other:
					meshManipulator.GetComponent<MeshManipulator>().startRotating(turnOtherScreen, false);
					meshManipulator.GetComponent<MeshManipulator>().startScaling(pinchDelta, false);
					// meshManipulator.GetComponent<MeshManipulator>().updateTaperScale(pinchDelta);
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
		crossScreenSliceTimer -= Time.deltaTime;
		endGestureLock -= Time.deltaTime;
		tapTimerThisScreen -= Time.deltaTime;
		tapTimerOtherScreen -= Time.deltaTime;
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
		turnThisScreen = 0;
		turnOtherScreen = 0;
		pinchDelta = 0;
		meshManipulator.GetComponent<MeshManipulator>().touchPosition = INF;

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
		else if (touchCountThisScreen == 1 && touchCountOtherScreen == 2) {
			state = Status.crossScreen1This2Other;
		}
		else if (touchCountThisScreen == 2 && touchCountOtherScreen == 1) {
			state = Status.crossScreen2This1Other;
		}

		touchText.text = state + " " + Time.deltaTime;

		if (state != Status.none) {
			touchTimer = touchDelayTolerance;
		}

		switch (state) {
			case Status.crossScreen1This2Other: {
				
				Vector3 panStart = (touchPrevPosOtherScreen[0] + touchPrevPosOtherScreen[1]) / 2;
				Vector3 panEnd = (touchPosOtherScreen[0] + touchPosOtherScreen[1]) / 2;

				turnThisScreen = (panEnd - panStart).y * 5;
				
				break;
			}
			case Status.crossScreen2This1Other: {
				
				Vector3 panStart = (touchPrevPosThisScreen[0] + touchPrevPosThisScreen[1]) / 2;
				Vector3 panEnd = (touchPosThisScreen[0] + touchPosThisScreen[1]) / 2;

				turnOtherScreen = - (panEnd - panStart).y * 5;
				
				break;
			}
			case Status.singleScreen2This: {
				
				float pinchStart = (touchPrevPosThisScreen[0] - touchPrevPosThisScreen[1]).magnitude;
				float pinchEnd = (touchPosThisScreen[0] - touchPosThisScreen[1]).magnitude;

				pinchDelta = (pinchEnd - pinchStart);
				
				Vector3 startVec = touchPrevPosThisScreen[0] - touchPrevPosThisScreen[1];
				Vector3 endVec = touchPosThisScreen[0] - touchPosThisScreen[1];
				float turnStart = Vector3.Angle(new Vector3(1, 0, 0), startVec);
				turnStart = (startVec.y >= 0 ? turnStart : -turnStart);
				float turnEnd = Vector3.Angle(new Vector3(1, 0, 0), endVec);
				turnEnd = (endVec.y >= 0 ? turnEnd : -turnEnd);

				turnThisScreen = turnEnd - turnStart;

				endGestureLock = 0.25f;
				break;
			}
			case Status.singleScreen2Other: {
				
				float pinchStart = (touchPrevPosOtherScreen[0] - touchPrevPosOtherScreen[1]).magnitude;
				float pinchEnd = (touchPosOtherScreen[0] - touchPosOtherScreen[1]).magnitude;

				pinchDelta = (pinchEnd - pinchStart);
				
				Vector3 startVec = touchPrevPosOtherScreen[0] - touchPrevPosOtherScreen[1];
				Vector3 endVec = touchPosOtherScreen[0] - touchPosOtherScreen[1];
				float turnStart = Vector3.Angle(new Vector3(Mathf.Cos(-angle), 0, Mathf.Sin(-angle)), startVec);
				turnStart = (startVec.y >= 0 ? turnStart : -turnStart);
				float turnEnd = Vector3.Angle(new Vector3(Mathf.Cos(-angle), 0, Mathf.Sin(-angle)), endVec);
				turnEnd = (endVec.y >= 0 ? turnEnd : -turnEnd);

				turnOtherScreen = turnEnd - turnStart;

				endGestureLock = 0.25f;
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
				endGestureLock = 0.25f;
				break;
			}
			case Status.singleScreen1This: {
				if (touchPosThisScreen[0].y > -3.2 && touchPosThisScreen[0].y < 4.2) {
					if (touchPhaseThisScreen[0] == TouchPhase.Began) {
						tapTimerThisScreen = tapDurationTolerance;
					}
					else if (touchPhaseThisScreen[0] == TouchPhase.Ended && tapTimerThisScreen >= 0) {
						meshManipulator.GetComponent<MeshManipulator>().touchPosition = touchPosThisScreen[0];
						meshManipulator.GetComponent<MeshManipulator>().castRay();
						tapTimerThisScreen = -1;
					}
					else {
						dragDelta = (touchPrevPosThisScreen[0] - touchPosThisScreen[0]).x;
						Vector3 panStart = touchPrevPosThisScreen[0];
						Vector3 panEnd = touchPosThisScreen[0];
						panThisScreen = panEnd - panStart;
					}
				}
				break;
			}
			case Status.singleScreen1Other: {
				if (touchPosOtherScreen[0].y > -3.2 && touchPosOtherScreen[0].y < 4.2) {
					if (touchPhaseOtherScreen[0] == TouchPhase.Began) {
						tapTimerOtherScreen = tapDurationTolerance;
					}
					else if (touchPhaseOtherScreen[0] == TouchPhase.Ended && tapTimerOtherScreen >= 0) {
						meshManipulator.GetComponent<MeshManipulator>().touchPosition = touchPosOtherScreen[0];
						meshManipulator.GetComponent<MeshManipulator>().castRay();
						tapTimerOtherScreen = -1;
					}
					else {
						dragDelta = (touchPosOtherScreen[0] - touchPrevPosOtherScreen[0]).x;
						Vector3 panStart = touchPrevPosOtherScreen[0];
						Vector3 panEnd = touchPosOtherScreen[0];
						panOtherScreen = panEnd - panStart;
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

		Vector3 centerPos = (endSliceThisScreen + endSliceOtherScreen) / 2;
		Vector3 normal = crossProduct(endSliceThisScreen - endSliceOtherScreen, new Vector3(0, 1, 0));
		// Vector3 normal = crossProduct(centerPos - endSliceOtherScreen, centerPos - endSliceThisScreen);

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
		meshManipulator.GetComponent<MeshManipulator>().startSlice(false);
	}

	private void visualizeCrossScreenSlice() {
		Vector3 centerPos = (startSliceThisScreen + startSliceOtherScreen) / 2;
		// Vector3 startThis = intersectLinePlane(centerPos, centerPos + endSliceThisScreen - endSliceOtherScreen, endSliceThisScreen, new Vector3(0, 0, 1));
		// Vector3 startOther = intersectLinePlane(centerPos, centerPos + endSliceThisScreen - endSliceOtherScreen, endSliceOtherScreen, new Vector3(Mathf.Sin(-angle), 0, -Mathf.Cos(angle)));
		Vector3 startThis = new Vector3(endSliceThisScreen.x, 10, endSliceThisScreen.z);
		Vector3 startOther = new Vector3(endSliceOtherScreen.x, 10, endSliceOtherScreen.z);
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
		touchPhaseOtherScreen = new TouchPhase[touchCount];

		for (int i=0;i<touchCountOtherScreen;i++) {
			touchPosOtherScreen[i] = touchPos[i];
			touchPrevPosOtherScreen[i] = touchPrevPos[i];
			touchPhaseOtherScreen[i] = phases[i];
		}
	}
	
}
