﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
	private GameObject sender;
	private Text debugText;
	private LineRenderer screenSide;

	[HideInInspector]
	public Vector3 acceOther;

	[HideInInspector]
	public float angle;
	private float prevAngle = -1;

	private float defaultAngle = - Mathf.PI / 3;
	private const float minAngle = - 2 * Mathf.PI / 3;
	private const float maxAngle = Mathf.PI / 6;

	private bool isActivated = true;
	// Start is called before the first frame update
	void Start()
	{

		Camera cam = Camera.main;
		VectorCalculator.camHeight = 2f * cam.orthographicSize;
		VectorCalculator.camWidth = VectorCalculator.camHeight * cam.aspect;

		sender = GameObject.Find("Server");
		debugText = GameObject.Find("Debug").GetComponent<Text>();
		screenSide = GameObject.Find("SideViewSceen").GetComponent<LineRenderer>();

		angle = defaultAngle;
	}

	// Update is called once per frame
	void Update()
	{

		Vector3 acceThis = Input.acceleration;
		acceOther.y = 0;
		acceThis.y = 0;
		float angleTemp = Vector3.Angle(acceThis, acceOther);

		angleTemp = - angleTemp * Mathf.PI / 180;

		if (isActivated) {
			angle = Mathf.Lerp(angle, angleTemp, Time.deltaTime * 3);
		}
		else {
			angle = - Mathf.PI / 2;
		}
		angle = angle > minAngle ? angle : minAngle;
		angle = angle < maxAngle ? angle : maxAngle;

		angle *= 125;
		angle = Mathf.Round(angle);
		angle /= 125;

		angle = angle > - Mathf.PI / 2 ? angle : - Mathf.PI / 2;
		angle = angle < 0 ? angle : 0;

		angle = - Mathf.PI / 3;

		screenSide.SetPosition(2, new Vector3(2.5f + 5 * Mathf.Cos(-angle), -5.361f, 5 * Mathf.Sin(-angle)));

		if (Mathf.Abs(angle - prevAngle) > 0f) {
			sender.GetComponent<ServerController>().sendMessage("Angle\n" + angle + "\n");
		}
		prevAngle = angle;
		VectorCalculator.angle = angle;
		
	}
}
