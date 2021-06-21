using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FaceTracker : MonoBehaviour
{
	public GameObject sender;
	public GameObject renderCam;
	public Text facePosText;
	public Image observeButton;
	public Image perspectiveButton;
	public Sprite observeFaceTrackingSprite;
	public Sprite observeFixedCameraSprite;
	public Sprite orthographicSprite;
	public Sprite perspectiveSprite;
	private bool useFaceTrack = false;

	private Camera cam;
	private float camWidth;
	private float camHeight;
	
	[HideInInspector]
	public bool increaseX = false;
	[HideInInspector]
	public bool decreaseX = false;
	[HideInInspector]
	public bool increaseY = false;
	[HideInInspector]
	public bool decreaseY = false;
	[HideInInspector]
	public bool increaseZ = false;
	[HideInInspector]
	public bool decreaseZ = false;

	private Vector3 currentObserve = new Vector3(6f, -1f, -10f);
	private Vector3 observe = new Vector3(6f, -1f, -10f);
	private Vector3 defaultObserve = new Vector3(6f, -1f, -10f);
	private float correction = 0.2f;
	private float smoothSpeed = 20f;
	private float smoothTolerance = 0.01f;
	private float observationScalePlaner = 10f;
	private float observationScaleVertical = 10f;
	private float observeMoveSensitive = 0.05f;
	// Start is called before the first frame update
	void Start()
	{
		cam = renderCam.GetComponent<Camera>();
		camHeight = 2f * Camera.main.orthographicSize;
		camWidth = camHeight * Camera.main.aspect;
	}

	void Update() {
		observeButton.sprite = (useFaceTrack ? observeFaceTrackingSprite : observeFixedCameraSprite);
		perspectiveButton.sprite = (cam.orthographic ? orthographicSprite : perspectiveSprite);
		updateObservation();
		updateFov();
	}

	// Update is called once per frame
	void updateObservation()
	{

		if (cam.orthographic) {
			renderCam.transform.position = new Vector3(0, 0, -5);
			string msg = "Face\nO\n";
			sender.GetComponent<ServerController>().sendMessage(msg);
		}
		else {
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
				string msg = "Face\n" + currentObserve.x + "," + currentObserve.y + "," + currentObserve.z + "\n";
				sender.GetComponent<ServerController>().sendMessage(msg);
			}
			renderCam.transform.position = currentObserve;
		}
		facePosText.text = "Face pos: " + currentObserve;
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

	public void switchObservationMode() {
		if (useFaceTrack) {
			useFaceTrack = false;
			observe = defaultObserve;
		} else {
			useFaceTrack = true;
		}
	}
	public void switchPerspectiveMode() {
		if (cam.orthographic) {
			cam.orthographic = false;
		} else {
			cam.orthographic = true;
		}
	}

	public void resetAll() {
		useFaceTrack = false;
		observe = defaultObserve;
	}
}
