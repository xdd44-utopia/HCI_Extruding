using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanVisualizer : MonoBehaviour
{
	public GameObject touchProcessor;
	public GameObject bkg;
	public GameObject slider;
	private LineRenderer bkgLR;
	private LineRenderer sliderLR;

	private const float lineWidth = 0.05f;

	[HideInInspector]
	public bool isPanning;
	private bool prevState;
	private Vector3 pos;
	private Vector3 startPos;

	private float camWidth;
	private float camHeight;

	// Start is called before the first frame update
	void Start()
	{
		Camera cam = Camera.main;
		camHeight = cam.orthographicSize;
		camWidth = camHeight * cam.aspect;

		bkgLR = bkg.AddComponent<LineRenderer>();
		bkgLR.startWidth = lineWidth;
		bkgLR.material = new Material(Shader.Find("Sprites/Default"));
		bkgLR.positionCount = 3;
		bkgLR.startColor = new Color(1, 1, 1, 0.5f);
		bkgLR.endColor = new Color(1, 1, 1, 0.5f);

		sliderLR = slider.AddComponent<LineRenderer>();
		sliderLR.startWidth = lineWidth;
		sliderLR.material = new Material(Shader.Find("Sprites/Default"));
		sliderLR.positionCount = 3;
		sliderLR.startColor = new Color(1, 1, 1, 1);
		sliderLR.endColor = new Color(1, 1, 1, 1);
		
		isPanning = false;
		prevState = false;
	}

	// Update is called once per frame
	void Update()
	{

		if (!prevState && isPanning) {
			startPos = touchProcessor.GetComponent<TouchProcessor>().pos;
		}
		prevState = isPanning;
		pos = touchProcessor.GetComponent<TouchProcessor>().pos;

		bkgLR.enabled = isPanning;
		sliderLR.enabled = isPanning;

		bkgLR.SetPosition(0, new Vector3(-camWidth, startPos.y, startPos.z));
		bkgLR.SetPosition(1, new Vector3(-camWidth, startPos.y, pos.z));
		bkgLR.SetPosition(2, new Vector3(-camWidth, pos.y, pos.z));

		sliderLR.SetPosition(0, new Vector3(-camWidth, startPos.y, startPos.z));
		sliderLR.SetPosition(1, new Vector3(-camWidth, pos.y, pos.z));
		sliderLR.SetPosition(2, new Vector3(pos.x, pos.y, pos.z));

	}
	
}
