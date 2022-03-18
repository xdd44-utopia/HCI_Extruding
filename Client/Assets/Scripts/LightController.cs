using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LightController : MonoBehaviour
{

	public Text debugText;
	public Text angleText;
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		float angle = VectorCalculator.angle;
		this.transform.rotation = Quaternion.Euler(0, -angle / Mathf.PI * 180, 0);
		angleText.text = Math.Round((- angle * 180 / Mathf.PI), 1) + "°";
	}
}
