using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchProcessor : MonoBehaviour
{

	public GameObject meshManipulator;
	public GameObject slicePlane;
	public GameObject touchPointMark;
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

	private int touchCountThisScreen = 0;
	private Vector3[] touchPosThisScreen;
	private Vector3[] touchPrevPosThisScreen;
	private int touchCountOtherScreen = 0;
	private Vector3[] touchPosOtherScreen;
	private Vector3[] touchPrevPosOtherScreen;

	//double tap
	private float doubleTapTimer = 0;
	private float doubleTapTolerance = 0.2f;
	private float doubleTapInterval = 0.05f;

	//cross-screen slice
	private float crossScreenSliceTimer = 0;
	private float crossScreenSliceTolerance = 0.15f;
	private Vector3 startSliceThisScreen;
	private Vector3 startSliceOtherScreen;
	private Vector3 endSliceThisScreen;
	private Vector3 endSliceOtherScreen;
	private float sliceMinDist = 1f;


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
			for (int i=0;i<touchCountThisScreen;i++) {
				Touch tch = Input.touches[i];
				touchPosThisScreen[i] = tch.position;
				touchPosThisScreen[i] -= new Vector3(360, 772, 0);
				touchPosThisScreen[i] *= Camera.main.orthographicSize / 772;
				touchPrevPosThisScreen[i] = tch.position - tch.deltaPosition;
				touchPrevPosThisScreen[i] -= new Vector3(360, 772, 0);
				touchPrevPosThisScreen[i] *= Camera.main.orthographicSize / 772; 
			}
		}

		calculate();

		if (touchTimer > 0) {
			if (touchCountThisScreen == 2) {
				meshManipulator.GetComponent<MeshManipulator>().startTransform(panThisScreen, 0, turnThisScreen, true);
			}
			else if (touchCountOtherScreen == 2) {
				meshManipulator.GetComponent<MeshManipulator>().startTransform(panOtherScreen, 0, turnOtherScreen, false);
			}
			else if (touchCountThisScreen + touchCountOtherScreen == 2) {
				meshManipulator.GetComponent<MeshManipulator>().startTransform(Vector3.zero, pinchDelta, 0, false);
			}
		}
		if (touchTimerOtherScreen <= 0) {
			touchCountOtherScreen = 0;
		}
		if (crossScreenSliceTimer <= 0) {
			processCrossScreenSlice();
			startSliceThisScreen = Vector3.zero;
			startSliceOtherScreen = Vector3.zero;
			endSliceThisScreen = Vector3.zero;
			endSliceOtherScreen = Vector3.zero;
		}

		touchTimer -= Time.deltaTime;
		touchTimerOtherScreen -= Time.deltaTime;
		doubleTapTimer -= Time.deltaTime;
		crossScreenSliceTimer -= Time.deltaTime;

	}
	private void calculate () {
		dragDelta = 0;
		panThisScreen = new Vector3(0, 0, 0);
		panOtherScreen = new Vector3(0, 0, 0);
		pinchDelta = 0;
		meshManipulator.GetComponent<MeshManipulator>().touchPosition = new Vector3(10000, 10000, 10000);

		if (touchCountThisScreen == 2) {

			touchTimer = touchDelayTolerance;

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
		}
		else if (touchCountOtherScreen == 2) {

			touchTimer = touchDelayTolerance;

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

		}
		else if (touchCountThisScreen + touchCountOtherScreen == 2) {

			touchTimer = touchDelayTolerance;
			
			float pinchStart = (touchPrevPosThisScreen[0] - touchPrevPosOtherScreen[0]).magnitude;
			float pinchEnd = (touchPosThisScreen[0] - touchPosOtherScreen[0]).magnitude;

			pinchDelta = (pinchEnd - pinchStart);

			if (Mathf.Abs(pinchDelta) > minPinchDistance) {
				//pinchDelta *= 1;
				pinchDelta *= 0;
			}
			else {
				pinchDelta = 0;
			}

			if (crossScreenSliceTimer <= 0) {
				startSliceThisScreen = touchPosThisScreen[0];
				startSliceOtherScreen = touchPosOtherScreen[0];
			}
			else {
				endSliceThisScreen = touchPosThisScreen[0];
				endSliceOtherScreen = touchPosOtherScreen[0];
			}
			crossScreenSliceTimer = crossScreenSliceTolerance;

		}
		else if (Input.touchCount == 1) {
			
			if (touchPosThisScreen[0].y > -3.2 && touchPosThisScreen[0].y < 4.2) {
				meshManipulator.GetComponent<MeshManipulator>().touchPosition = touchPosThisScreen[0];
				touchPointMark.transform.position = touchPosThisScreen[0];

				if (doubleTapTimer > doubleTapInterval) {
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
		}
		else if (touchCountOtherScreen == 1) {
			
			if (touchPosOtherScreen[0].y > -3.2 && touchPosOtherScreen[0].y < 4.2) {
				meshManipulator.GetComponent<MeshManipulator>().touchPosition = touchPosOtherScreen[0];
				touchPointMark.transform.position = touchPosOtherScreen[0];

				if (doubleTapTimer > doubleTapInterval) {
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
		}
	}

	private void processCrossScreenSlice() {

		if ((endSliceThisScreen - startSliceThisScreen).magnitude < sliceMinDist || (endSliceOtherScreen - startSliceOtherScreen).magnitude < sliceMinDist) {
			return;
		}

		Vector3 centerPos = (startSliceThisScreen + startSliceOtherScreen + endSliceThisScreen + endSliceOtherScreen) / 4;
		Vector3 normal = crossProduct(startSliceThisScreen - endSliceOtherScreen, startSliceOtherScreen - endSliceThisScreen);

		if (normal.z > 0) {
			normal = -normal;
		}

		debugText.text = centerPos + " " + normal;

		Vector3 axisToFocus = crossProduct(normal, new Vector3(0, -1, 0));
		float angleToFocus = Vector3.Angle(normal, new Vector3(0, -1, 0));

		Quaternion originRotation = Quaternion.identity;
		originRotation.eulerAngles = new Vector3(0, -1, 0);
		slicePlane.transform.position = centerPos;
		slicePlane.transform.rotation = Quaternion.AngleAxis(angleToFocus, axisToFocus) * originRotation;
		meshManipulator.GetComponent<MeshManipulator>().startSlice();

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
		}
	}
	
}
