using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchProcessor : MonoBehaviour
{
	public GameObject sender;
	public GameObject obj;

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
	// Start is called before the first frame update
	void Start()
	{
		phase = Phase.scale;
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
		bool isStartingTouching = false;
		bool isTouching = false;
		Vector2 tp = new Vector2(2000, 2000);

		if (Input.touchCount > 0) {
			tp = processTouchPoint(new Vector2(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y));
			isTouching = true;
			isStartingTouching = (Input.GetTouch(0).phase == TouchPhase.Began);
		}
		if (Input.GetMouseButton(0)) {
			tp = processTouchPoint(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
			isTouching = true;
			isStartingTouching = Input.GetMouseButtonDown(0);
		}

		if (isStartingTouching) {
			if (!isScaleDone) {
				pos = tp;
				startTouch = tp;
				planarScale = 0;
				isScaleDone = true;
			}
			else {
				phase = Phase.freemove;
			}
		}

		if (isTouching && phase == Phase.scale) {
			planarScale = scaleSensitive * (new Vector3(tp.x, tp.y, 0) - new Vector3(startTouch.x, startTouch.y, 0)).magnitude;
			prevTouch = tp;
			sender.GetComponent<ServerController>().sendMessage();
		}
	}

	private void freemoving() {
		bool isStartingTouching = false;
		bool isTouching = false;
		Vector2 tp1 = new Vector2(2000, 2000);
		Vector2 tp2 = new Vector2(2000, 2000);

		if (Input.touchCount > 0) {
			tp1 = processTouchPoint(new Vector2(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y));
			if (Input.touchCount > 1) {
				tp2 = processTouchPoint(new Vector2(Input.GetTouch(1).position.x, Input.GetTouch(1).position.y));
			}
			isTouching = true;
			isStartingTouching = (Input.GetTouch(0).phase == TouchPhase.Began) || (!lastFrameTouching);
			lastFrameTouching = true;
		}
		if (Input.GetMouseButton(0)) {
			tp1 = processTouchPoint(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
			isTouching = true;
			isStartingTouching = Input.GetMouseButtonDown(0) || !lastFrameTouching;
			lastFrameTouching = true;
		}

		if (isStartingTouching) {
			prevTouch = tp1;
			if (Input.touchCount > 1) {
				prevAngle = processAngle(tp1, tp2);
			}
		}

		if (isTouching) {
			if (Input.touchCount > 1) {
				rotate(rotateSensitive * (processAngle(tp1, tp2) - prevAngle));
				prevAngle = processAngle(tp1, tp2);
				lastFrameTwoPointTouching = true;
			}
			else {
				if (lastFrameTwoPointTouching) {
					lastFrameTwoPointTouching = false;
				}
				else {
					pos += moveSensitive * (new Vector3(tp1.x, tp1.y, 0) - new Vector3(prevTouch.x, prevTouch.y, 0));
				}
				prevTouch = tp1;
			}
			sender.GetComponent<ServerController>().sendMessage();
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

	public void resetAll() {
		pos = defaultPos;
		rot = defaultRot;
		phase = Phase.scale;
		isScaleDone = false;
		lastFrameTouching = false;
		verticalScale = 0.01f;
		planarScale = 0f;
		sender.GetComponent<ServerController>().sendMessage();
	}

	private enum Phase {
		scale,
		freemove
	}
}
