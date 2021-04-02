using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FaceTracker : MonoBehaviour
{
	public GameObject sender;
	public GameObject renderCam;
	public Text facePosText;
	private bool useFaceTrack = false;

	private float camWidth;
	private float camHeight;
	
	public bool increaseX = false;
	public bool decreaseX = false;
	public bool increaseY = false;
	public bool decreaseY = false;
	public bool increaseZ = false;
	public bool decreaseZ = false;

	private Vector3 currentObserve = new Vector3(0, 0, -10f);
	private Vector3 observe = new Vector3(0, 0, -5f);
	private Vector3 defaultObserve = new Vector3(0, 0, -5f);
	private float correction = 0.2f;
	private float smoothSpeed = 20f;
	private float smoothTolerance = 0.01f;
	private float observationScalePlaner = 10f;
	private float observationScaleVertical = 10f;
	private float observeMoveSensitive = 0.05f;
	// Start is called before the first frame update
	void Start()
	{
		Camera cam = Camera.main;
		camHeight = 2f * cam.orthographicSize;
		camWidth = camHeight * cam.aspect;
	}

	void Update() {
		updateObservation();
		updateFov();
	}

	// Update is called once per frame
	void updateObservation()
	{
		if (useFaceTrack) {
			GameObject[] objects = GameObject.FindGameObjectsWithTag("Player");
			GameObject testObj = new GameObject();
			Instantiate(testObj, objects[0].transform.position, Quaternion.identity);
			testObj.transform.position = objects[0].transform.position;
			objects = GameObject.FindGameObjectsWithTag("FacePosition");
			testObj.transform.RotateAround(
				new Vector3(0f, 0f, 0f),
				new Vector3(0f, 1f, 0f),
				-objects[0].transform.rotation.eulerAngles.y
			);
			testObj.transform.RotateAround(
				new Vector3(0f, 0f, 0f),
				new Vector3(1f, 0f, 0f),
				-objects[0].transform.rotation.eulerAngles.x
			);
			testObj.transform.RotateAround(
				new Vector3(0f, 0f, 0f),
				new Vector3(0f, 0f, 1f),
				-objects[0].transform.rotation.eulerAngles.z
			);
			observe = new Vector3(
				-testObj.transform.position.x,
				testObj.transform.position.y,
				-testObj.transform.position.z
			);
			observe.x *= observationScalePlaner;
			observe.y *= observationScalePlaner;
			observe.y += correction;
			observe.z *= observationScaleVertical;
			Destroy(testObj, 0f);
		}
		else {
			if (increaseX) { observe.x += observeMoveSensitive; }
			if (decreaseX) { observe.x -= observeMoveSensitive; }
			if (increaseY) { observe.y += observeMoveSensitive; }
			if (decreaseY) { observe.y -= observeMoveSensitive; }
			if (increaseZ) { observe.z += observeMoveSensitive; }
			if (decreaseZ) { observe.z -= observeMoveSensitive; }
			facePosText.text = "Manual mode";
		}
		if (Vector3.Distance(currentObserve, observe) > smoothTolerance) {
			currentObserve = Vector3.Lerp(currentObserve, observe, smoothSpeed * Time.deltaTime);
			renderCam.transform.position = currentObserve;
			string msg = "Face\n" + currentObserve.x + "," + currentObserve.y + "," + currentObserve.z;
			sender.GetComponent<ServerController>().sendMessage(msg);
		}
		facePosText.text = "Face pos: " + currentObserve;
	}

	void updateFov() {
		Camera cam = renderCam.GetComponent<Camera>();
		float fovHorizontal = Mathf.Atan(-(Mathf.Abs(currentObserve.x) + camWidth / 2) / currentObserve.z) * 2;
		fovHorizontal = fovHorizontal * 180 / Mathf.PI;
		fovHorizontal = Camera.HorizontalToVerticalFieldOfView(fovHorizontal, cam.aspect);
		float fovVertical = Mathf.Atan(-(Mathf.Abs(currentObserve.y) + camHeight / 2) / currentObserve.z) * 2;
		fovVertical = fovVertical * 180 / Mathf.PI;
		cam.fieldOfView = (fovVertical > fovHorizontal ? fovVertical : fovHorizontal);
	}

	public void switchObservationMode() {
		if (useFaceTrack) {
			useFaceTrack = false;
			observe = defaultObserve;
		} else {
			useFaceTrack = true;
		}
	}

	public void resetAll() {
		useFaceTrack = false;
		observe = defaultObserve;
	}
}
