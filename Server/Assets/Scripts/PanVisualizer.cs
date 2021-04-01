using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanVisualizer : MonoBehaviour
{
	public GameObject touchProcessor;
	public GameObject startPoint;
	public GameObject point;
	public GameObject[] points = new GameObject[10];
	private GameObject[] objs = new GameObject[6];
	private LineRenderer[] lrs = new LineRenderer[6];

	private const float lineWidth = 0.05f;

	[HideInInspector]
	public bool isPanning;
	private bool prevState;
	private Vector3 pos;
	private Vector3 startPos;

	private float timer = -1;
	private const float tolerance = 0.1f;

	private float camWidth;
	private float camHeight;

	// Start is called before the first frame update
	void Start()
	{
		Camera cam = Camera.main;
		camHeight = cam.orthographicSize;
		camWidth = camHeight * cam.aspect;

		for (int i=0;i<6;i++) {
			objs[i] = new GameObject("Axis");

			lrs[i] = objs[i].AddComponent<LineRenderer>();
			lrs[i].startWidth = lineWidth;
			lrs[i].useWorldSpace = false;
			lrs[i].material = new Material(Shader.Find("Sprites/Default"));
			lrs[i].material.color = Color.white; 
			lrs[i].positionCount = 2;
			lrs[i].startColor = new Color(0, 0, 0, (i < 3 ? 0.25f : 1f));
			lrs[i].endColor = new Color(0, 0, 0, (i < 3 ? 0.25f : 1f));
		}

		for (int i=0;i<10;i++) {
			points[i] = Instantiate((i < 5 ? startPoint : point), new Vector3(0, 0, 0), Quaternion.identity);
		}
		
		isPanning = false;
		prevState = false;
	}

	// Update is called once per frame
	void Update()
	{

		for (int i=0;i<6;i++) {
			lrs[i].enabled = isPanning;
		}

		for (int i=0;i<10;i++) {
			points[i].SetActive(isPanning);
		}

		lrs[0].SetPosition(0, new Vector3(startPos.x, startPos.y, 0));
		lrs[0].SetPosition(1, new Vector3(startPos.x, startPos.y, 5));
		lrs[1].SetPosition(0, new Vector3(startPos.x, -5f, startPos.z));
		lrs[1].SetPosition(1, new Vector3(startPos.x, 5f, startPos.z));
		lrs[2].SetPosition(0, new Vector3(2.5f, startPos.y, startPos.z));
		lrs[2].SetPosition(1, new Vector3(-2.5f, startPos.y, startPos.z));

		lrs[3].SetPosition(0, new Vector3(pos.x, pos.y, 0));
		lrs[3].SetPosition(1, new Vector3(pos.x, pos.y, 5));
		lrs[4].SetPosition(0, new Vector3(pos.x, -5f, pos.z));
		lrs[4].SetPosition(1, new Vector3(pos.x, 5f, pos.z));
		lrs[5].SetPosition(0, new Vector3(2.5f, pos.y, pos.z));
		lrs[5].SetPosition(1, new Vector3(-2.5f, pos.y, pos.z));

		points[0].transform.position = new Vector3(startPos.x, startPos.y, 5);
		points[1].transform.position = new Vector3(startPos.x, -5f, startPos.z);
		points[2].transform.position = new Vector3(startPos.x, 5f, startPos.z);
		points[3].transform.position = new Vector3(2.5f, startPos.y, startPos.z);
		points[4].transform.position = new Vector3(-2.5f, startPos.y, startPos.z);
		points[5].transform.position = new Vector3(pos.x, pos.y, 5);
		points[6].transform.position = new Vector3(pos.x, -5f, pos.z);
		points[7].transform.position = new Vector3(pos.x, 5f, pos.z);
		points[8].transform.position = new Vector3(2.5f, pos.y, pos.z);
		points[9].transform.position = new Vector3(-2.5f, pos.y, pos.z);

		isPanning = (timer > 0);
		timer -= Time.deltaTime;

	}

	public void pan(Vector3 currentPos) {
		if (!prevState && isPanning) {
			startPos = currentPos;
		}
		prevState = isPanning;
		pos = currentPos;
		isPanning = true;
		timer = tolerance;
	}
	
}
