using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
	// public GameObject slider;
	// public GameObject cam;

	public Text angleText;
	public GameObject protractor;

	[HideInInspector]
	public float angle;

	private float defaultAngle = - Mathf.PI / 2;
	private const float leftMost = -337.5f;
	private const float rightMost = 337.5f;

	// Start is called before the first frame update
	void Start()
	{
		angle = defaultAngle;
		VectorCalculator.angle = angle;
		Camera cam = Camera.main;
		VectorCalculator.camHeight = 2f * cam.orthographicSize;
		VectorCalculator.camWidth = VectorCalculator.camHeight * cam.aspect;
	}

	// Update is called once per frame
	void Update() {
		protractor.transform.rotation = Quaternion.Euler(-90, - angle * 180 / Mathf.PI, 0);
		angleText.text = Math.Round((- angle * 180 / Mathf.PI), 1) + "°";
		VectorCalculator.angle = angle;
	}
}
