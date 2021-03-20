using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameVisualizer : MonoBehaviour
{
	private LineRenderer lr;

	private const float lineWidth = 0.05f;

	private float camWidth;
	private float camHeight;

	// Start is called before the first frame update
	void Start()
	{
		Camera cam = Camera.main;
		camHeight = cam.orthographicSize;
		camWidth = camHeight * cam.aspect;

		lr = GetComponent<LineRenderer>();
		lr.startWidth = lineWidth;
		lr.useWorldSpace = false;
		lr.material = new Material(Shader.Find("Sprites/Default"));
		lr.material.color = Color.white; 
		lr.positionCount = 4;
		lr.startColor = new Color(1, 1, 1, 1f);
		lr.endColor = new Color(1, 1, 1, 1f);
		lr.SetPosition(0, new Vector3(-camWidth, camHeight, 0));
		lr.SetPosition(1, new Vector3(-camWidth, camHeight, camWidth * 2));
		lr.SetPosition(2, new Vector3(-camWidth, -camHeight, camWidth * 2));
		lr.SetPosition(3, new Vector3(-camWidth, -camHeight, 0));

		lr.enabled = true;
		
	}

	// Update is called once per frame
	void Update(){

	}
	
}
