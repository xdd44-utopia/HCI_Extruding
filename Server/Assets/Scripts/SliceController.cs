using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceController : MonoBehaviour
{
	// Start is called before the first frame update
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		transform.position = new Vector3(2.2f, 0, 0.05f);
		float angle = GameObject.Find("SliderController").GetComponent<SliderController>().angle;
		transform.rotation = Quaternion.Euler(-90, angle * 180 / Mathf.PI, 0);
		//transform.rotation = Quaternion.Euler(-90, -65, 0);
	}
}
