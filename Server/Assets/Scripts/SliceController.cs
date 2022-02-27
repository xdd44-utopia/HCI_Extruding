using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceController : MonoBehaviour
{
	[HideInInspector]
	public bool locked;
	// Start is called before the first frame update
	void Start()
	{
		locked = false;
	}

	// Update is called once per frame
	void Update()
	{
		if (!locked) {
			transform.position = new Vector3(5f * Camera.main.aspect, 0, 0);
			float angle = GameObject.Find("SliderController").GetComponent<SliderController>().angle;
			transform.rotation = Quaternion.Euler(-90, angle * 180 / Mathf.PI, 0);
		}
		//transform.rotation = Quaternion.Euler(-90, -65, 0);
	}
}
