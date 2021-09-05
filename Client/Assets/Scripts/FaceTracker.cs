using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FaceTracker : MonoBehaviour
{
	public GameObject renderCam;
	public GameObject sender;
	public GameObject sliderController;
	public Text debugText;

	private Camera cam;
	private float camWidth;
	private float camHeight;
	private float angle = - Mathf.PI / 2;

	[HideInInspector]
	public bool useOrtho = false;
	[HideInInspector]
	public Vector3 observeOther = new Vector3(0, 0, -5f);

	private Vector3 currentObserve = new Vector3(0, 0, -5f);
	private Vector3 faceDetected = new Vector3(6f, -1f, -10f);
	private Vector3 prevFaceDetected = new Vector3(6f, -1f, -10f);
	private float correction = 3f;
	private float observationScalePlaner = 75f;
	private float observationScaleVertical = 50f;
	private float observeMoveSensitive = 0.05f;
	// Start is called before the first frame update
	void Start()
	{
		cam = renderCam.GetComponent<Camera>();
		camHeight = 2f * Camera.main.orthographicSize;
		camWidth = camHeight * Camera.main.aspect;
	}

	void Update() {
		angle = sliderController.GetComponent<SliderController>().angle;
		currentObserve = convertFromServer(observeOther);
		updateObservation();
		updateFov();
	}

	// Update is called once per frame
	void updateObservation()
	{

		GameObject[] objects = GameObject.FindGameObjectsWithTag("Player");
		Vector3 faceDetectedForServer;
		if (objects.Length == 0) {
			Vector3 temp = new Vector3(0, 0, -10);
			temp = (temp + convertToServer(temp)) / 2;
			faceDetectedForServer = new Vector3(temp.x, 100, temp.z);
		}
		else {
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
			faceDetected = new Vector3(
				-testObj.transform.position.x,
				testObj.transform.position.y,
				-testObj.transform.position.z
			);
			faceDetected.x *= observationScalePlaner;
			faceDetected.y *= observationScalePlaner;
			faceDetected.y += correction;
			faceDetected.z *= observationScaleVertical;
			Destroy(testObj, 0f);
			faceDetectedForServer = convertToServer(faceDetected);
		}
		if (Vector3.Distance(faceDetectedForServer, prevFaceDetected) > 0.05f) {
			string msg = "Face\n" + faceDetectedForServer.x + "," + faceDetectedForServer.y + "," + faceDetectedForServer.z + "\n";
			sender.GetComponent<ClientController>().sendMessage(msg);
			prevFaceDetected = faceDetectedForServer;
		}

		if (useOrtho) {
			cam.orthographic = true;
			renderCam.transform.position = new Vector3(0, 0, -5);
		}
		else {
			cam.orthographic = false;
			renderCam.transform.position = currentObserve;
		}
	}

	void updateFov() {
		cam.orthographicSize = camHeight / 2;
		float fovHorizontal = Mathf.Atan(-(Mathf.Abs(currentObserve.x) + camWidth / 2) / currentObserve.z) * 2;
		fovHorizontal = fovHorizontal * 180 / Mathf.PI;
		fovHorizontal = Camera.HorizontalToVerticalFieldOfView(fovHorizontal, cam.aspect);
		float fovVertical = Mathf.Atan(-(Mathf.Abs(currentObserve.y) + camHeight / 2) / currentObserve.z) * 2;
		fovVertical = fovVertical * 180 / Mathf.PI;
		cam.fieldOfView = (fovVertical > fovHorizontal ? fovVertical : fovHorizontal);
	}

	private Vector3 convertFromServer(Vector3 v) {
		Vector3 origin = new Vector3(camWidth / 2 + camWidth * Mathf.Cos(angle) / 2, 0, - camWidth * Mathf.Sin(angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(angle), 0, - Mathf.Sin(angle));
		Vector3 z = new Vector3(Mathf.Cos(Mathf.PI / 2 - angle), 0, Mathf.Sin(Mathf.PI / 2 - angle));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	private Vector3 convertToServer(Vector3 v) {
		Vector3 origin = new Vector3(- camWidth / 2 - camWidth * Mathf.Cos(angle) / 2, 0, - camWidth * Mathf.Sin(angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
		Vector3 z = new Vector3(-Mathf.Cos(Mathf.PI / 2 - angle), 0, Mathf.Sin(Mathf.PI / 2 - angle));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	private float multXZ(Vector3 from, Vector3 to) {
		return from.x * to.x + from.z * to.z;
	}
	
}
