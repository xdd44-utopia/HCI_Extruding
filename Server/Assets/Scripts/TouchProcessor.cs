using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{

	private MeshManipulator meshManipulator;
	public GameObject sliderController;
	private ServerController sender;
	public GameObject slicePlane;
	public GameObject sliceTraceVisualizer;
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
	private float pinchThreshold = 0.01f;

	private float touchTimer = 0;
	private float touchTimerOtherScreen = 0;
	private float touchDelayTolerance = 0.25f;

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
		GameObject findObject;
		findObject = GameObject.Find("OBJECT");
		if (findObject != null) {
			meshManipulator = findObject.GetComponent<MeshManipulator>();
		}
		findObject = GameObject.Find("Server");
		if (findObject != null) {
			sender = findObject.GetComponent<ServerController>();
		}
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
				touchPosThisScreen[i] -= new Vector3(Screen.width / 2, Screen.height / 2, 0);
				touchPosThisScreen[i] *= Camera.main.orthographicSize / (Screen.height / 2);
				touchPrevPosThisScreen[i] = tch.position - tch.deltaPosition;
				touchPrevPosThisScreen[i] -= new Vector3(Screen.width / 2, Screen.height / 2, 0);
				touchPrevPosThisScreen[i] *= Camera.main.orthographicSize / (Screen.height / 2);
				touchPhaseThisScreen[i] = tch.phase;
			}
		}

		visualize();
		calculate();

		if (touchTimer > 0) {
			switch (state) {
				case Status.singleScreen1This:
					meshManipulator.startMoving(panThisScreen, true);
					meshManipulator.updateExtrudeScale(dragDelta, true);
					break;
				case Status.singleScreen1Other:
					meshManipulator.startMoving(panOtherScreen, false);
					meshManipulator.updateExtrudeScale(dragDelta, false);
					// meshManipulator.updateDrillScale(dragDelta);
					break;
				case Status.singleScreen2This:
					meshManipulator.startRotating(turnThisScreen, true);
					meshManipulator.updateTaperScale(pinchDelta);
					break;
				case Status.singleScreen2Other:
					meshManipulator.startRotating(turnOtherScreen, false);
					meshManipulator.updateTaperScale(pinchDelta);
					break;
				case Status.crossScreen2:
					meshManipulator.startScaling(pinchDelta);
					break;
			}
		}
		if (touchTimerOtherScreen <= 0) {
			touchCountOtherScreen = 0;
		}


		touchTimer -= Time.deltaTime;
		touchTimerOtherScreen -= Time.deltaTime;
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
	}
	private void calculate () {
		dragDelta = 0;
		panThisScreen = new Vector3(0, 0, 0);
		panOtherScreen = new Vector3(0, 0, 0);
		turnThisScreen = 0;
		turnOtherScreen = 0;
		pinchDelta = 0;

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
		// Two screen cut disabled
		// else if (touchCountThisScreen == 1 && touchCountOtherScreen == 1) {
		// 	state = Status.crossScreen2;
		// }
		else if (touchCountThisScreen == 1 && touchCountOtherScreen == 2) {
			state = Status.crossScreen1This2Other;
		}
		else if (touchCountThisScreen == 2 && touchCountOtherScreen == 1) {
			state = Status.crossScreen2This1Other;
		}

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
				if (Mathf.Abs(pinchDelta) < pinchThreshold) {
					pinchDelta = 0;
				}
				
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
				if (Mathf.Abs(pinchDelta) < pinchThreshold) {
					pinchDelta = 0;
				}
				
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
				
				float pinchStart = (touchPrevPosThisScreen[0] - touchPrevPosOtherScreen[0]).magnitude;
				float pinchEnd = (touchPosThisScreen[0] - touchPosOtherScreen[0]).magnitude;

				pinchDelta = (pinchEnd - pinchStart);
				if (Mathf.Abs(pinchDelta) < pinchThreshold) {
					pinchDelta = 0;
				}

				endGestureLock = 0.25f;
				
				break;
			}
			case Status.singleScreen1This: {
				if (touchPosThisScreen[0].y > -3.2 && touchPosThisScreen[0].y < 4.2) {
					if (touchPhaseThisScreen[0] == TouchPhase.Began) {
						tapTimerThisScreen = tapDurationTolerance;
					}
					else if (touchPhaseThisScreen[0] == TouchPhase.Ended && tapTimerThisScreen >= 0) {
						meshManipulator.castRay(touchPosThisScreen[0]);
						tapTimerThisScreen = -1;
					}
					else if (touchPosThisScreen[0].y > -3.2 && touchPosThisScreen[0].y < 4.2) {
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
						meshManipulator.castRay(touchPosOtherScreen[0]);
						tapTimerOtherScreen = -1;
					}
					else {
						Vector3 touchDeltaPosOtherScreen = touchPosOtherScreen[0] - touchPrevPosOtherScreen[0];
						touchDeltaPosOtherScreen = new Vector3(touchDeltaPosOtherScreen.x, 0, touchDeltaPosOtherScreen.z);
						dragDelta = touchDeltaPosOtherScreen.magnitude * (touchDeltaPosOtherScreen.x > 0 || touchDeltaPosOtherScreen.z > 0 ? 1 : -1);
						Vector3 panStart = touchPrevPosOtherScreen[0];
						Vector3 panEnd = touchPosOtherScreen[0];
						panOtherScreen = panEnd - panStart;
					}
				}
				break;
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
