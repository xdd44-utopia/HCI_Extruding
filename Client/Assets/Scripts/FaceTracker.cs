using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FaceTracker : MonoBehaviour
{
	public GameObject renderCam;
	public GameObject sender;
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
	// Start is called before the first frame update
	void Start()
	{
		cam = renderCam.GetComponent<Camera>();
		camHeight = 2f * Camera.main.orthographicSize;
		camWidth = camHeight * Camera.main.aspect;
	}

	void Update() {
		angle = GameObject.Find("Angles").GetComponent<SliderController>().angle;
		currentObserve = convertFromServer(observeOther);
		updateObservation();
		updateFov();
		sender.GetComponent<ClientController>().sendMessage("Camera\n" + currentObserve.x + "," + currentObserve.y + "," + currentObserve.z + "\n");
	}

	// Update is called once per frame
	void updateObservation()
	{
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
		//debugText.text = angle + " " + renderCam.transform.position;
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
