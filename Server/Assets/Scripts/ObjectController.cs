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
	// Start is called before the first frame update
	void Start()
	{
		isTransformUpdated = true;
		isMeshUpdated = true;
		index = 0;
	}

	// Update is called once per frame
	void Update()
	{
		if (isMeshUpdated) {
			inside.GetComponent<MeshFilter>().mesh = GetComponent<MeshFilter>().mesh;
		}
	}
}
