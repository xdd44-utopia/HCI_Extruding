using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModelController : MonoBehaviour
{
	public GameObject touchProcessor;
	
	void Start() {
		
	}

	void Update() {
		// Vector3 tp = touchProcessor.GetComponent<TouchProcessor>().pos;
		// transform.position = new Vector3(tp.x, tp.y, tp.z);
		// transform.rotation = Quaternion.Euler(touchProcessor.GetComponent<TouchProcessor>().rot, 0, 0);
		// transform.localScale = new Vector3(planarScale, planarScale, verticalScale);
	}

}
