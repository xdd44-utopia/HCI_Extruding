using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LightController : MonoBehaviour
{

	public GameObject sliderController;
	public Text debugText;
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		float angle = sliderController.GetComponent<SliderController>().angle;
		this.transform.rotation = Quaternion.Euler(0, -angle / Mathf.PI * 180, 0);
	}
}
