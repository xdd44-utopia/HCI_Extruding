using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectController : MonoBehaviour
{

	public GameObject inside;
	[HideInInspector]
	public bool isTransformUpdated;
	[HideInInspector]
	public bool isMeshUpdated;
	[HideInInspector]
	public int index;
	[HideInInspector]
	public bool isThisScreenFocused;
	[HideInInspector]
	public bool isOtherScreenFocused;
	[HideInInspector]
	public bool isRealMeasure;
	[HideInInspector]
	public Vector3 realMeasure = new Vector3(1, 1, 1);
	// Start is called before the first frame update
	void Start()
	{
		isTransformUpdated = true;
		isMeshUpdated = true;
		index = 0;
		isThisScreenFocused = false;
		isOtherScreenFocused = false;
		isRealMeasure = false;
	}

	// Update is called once per frame
	void Update()
	{
		if (isMeshUpdated) {
			inside.GetComponent<MeshFilter>().mesh = GetComponent<MeshFilter>().mesh;
		}
	}
}
