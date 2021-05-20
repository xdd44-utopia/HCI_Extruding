using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RotateVisualizer : MonoBehaviour
{
	public GameObject touchProcessor;
	public GameObject bkg;
	public GameObject slider;
	private LineRenderer bkgLR;
	private LineRenderer sliderLR;

	public Text dbgText;

	private const float lineWidth = 0.05f;

	[HideInInspector]
	public bool isRotating;
	private bool prevState;
	private float angle;
	private float startAngle;

	private float camWidth;
	private float camHeight;

	// Start is called before the first frame update
	void Start()
	{
		Camera cam = Camera.main;
		camHeight = 10;
		camWidth = camHeight * cam.aspect;

		bkgLR = bkg.AddComponent<LineRenderer>();
		bkgLR.alignment = LineAlignment.TransformZ;
		bkgLR.startWidth = lineWidth;
		bkgLR.material = new Material(Shader.Find("Sprites/Default"));
		bkgLR.positionCount = 101;
		bkgLR.startColor = new Color(1, 1, 1, 0.25f);
		bkgLR.endColor = new Color(1, 1, 1, 0.25f);

		sliderLR = slider.AddComponent<LineRenderer>();
		sliderLR.alignment = LineAlignment.TransformZ;
		sliderLR.startWidth = lineWidth;
		sliderLR.material = new Material(Shader.Find("Sprites/Default"));
		sliderLR.positionCount = 100;
		sliderLR.startColor = new Color(1, 1, 1, 1);
		sliderLR.endColor = new Color(1, 1, 1, 1);
		
		isRotating = false;
		prevState = false;
	}

	// Update is called once per frame
	void Update()
	{

		// if (!prevState && isRotating) {
		// 	startAngle = touchProcessor.GetComponent<TouchProcessor>().rot;
		// }
		// prevState = isRotating;
		// angle = touchProcessor.GetComponent<TouchProcessor>().rot;

		bkgLR.enabled = isRotating;
		sliderLR.enabled = isRotating;

		Vector3 pos = new Vector3(0, 0, 0);//touchProcessor.GetComponent<TouchProcessor>().pos;
		for (int i=0;i<=100;i++) {
			bkgLR.SetPosition(i, new Vector3(-camWidth, pos.y + camWidth * Mathf.Cos(Mathf.PI * i / 50) / 2, pos.z + camWidth * Mathf.Sin(Mathf.PI * i / 50) / 2));
		}
		int n = (int)(angle - startAngle) * 100 / 360;
		if (n != 0) {
			int sign = 1;
			if (n < 0) {
				sign = -sign;
				n = -n;
			}
			sliderLR.positionCount = n + 2;
			sliderLR.SetPosition(0, new Vector3(-camWidth, pos.y, pos.z));
			for (int i=0;i<n;i++) {
				sliderLR.SetPosition(i + 1, new Vector3(-camWidth, pos.y + camWidth * Mathf.Cos(Mathf.PI * i * sign / 50) / 2, pos.z + camWidth * Mathf.Sin(Mathf.PI * i * sign/ 50) / 2));
			}
			sliderLR.SetPosition(n + 1, new Vector3(-camWidth, pos.y, pos.z));
		}
	}
	
}
