using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FaceTracker : MonoBehaviour
{
	public GameObject renderCam;
	public GameObject sender;
	public Text debugText;

	private float camWidth;
	private float camHeight;
	private float angle = - Mathf.PI / 2;
	private float sendTimer = 0;

	[HideInInspector]
	public Vector3 observeOther = new Vector3(0, 0, -5f);

	private Vector3 currentObserve = new Vector3(0, 0, -5f);
	// Start is called before the first frame update
	void Start()
	{
		Camera cam = Camera.main;
		camHeight = 2f * cam.orthographicSize;
		camWidth = camHeight * cam.aspect;
	}

	void Update() {
		angle = GameObject.Find("Angles").GetComponent<SliderController>().angle;
		currentObserve = convertFromServer(observeOther);
		updateObservation();
		updateFov();
		if (sendTimer >= 0.1f) {
			sender.GetComponent<ClientController>().sendMessage("Camera\n" + currentObserve.x + "," + currentObserve.y + "," + currentObserve.z);
			sendTimer = 0;
		}
		else {
			sendTimer += Time.deltaTime;
		}
	}

	// Update is called once per frame
	void updateObservation()
	{
		renderCam.transform.position = currentObserve;
	}

	void updateFov() {
		Camera cam = renderCam.GetComponent<Camera>();
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
