using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceTraceVisualizer : MonoBehaviour
{
	public GameObject sender;
	[HideInInspector]
	public Vector3 touchPointThisScreen;
	[HideInInspector]
	public Vector3 touchPointOtherScreen;
	private LineRenderer lineRenderer;
	void Start()
	{
		lineRenderer = GetComponent<LineRenderer>();
	}

	void Update()
	{
		
	}

	private bool checkVisible(Vector3 p) {
		float k = (touchPointThisScreen.y - touchPointOtherScreen.y) / (touchPointThisScreen.x - touchPointOtherScreen.x);
		return p.y - touchPointThisScreen.y > k * (p.x - touchPointThisScreen.x);
	}

	private Vector3 getIntersection(Vector3 s, Vector3 t) {
		float k1 = (touchPointThisScreen.y - touchPointOtherScreen.y) / (touchPointThisScreen.x - touchPointOtherScreen.x);
		//y = k1 * (x - touchPointThisScreen.x) + touchPointThisScreen.y
		float k2 = (s.y - t.y) / (s.x - t.x);
		//y = k2 * (x - s.x) + s.y
		float kz = (s.z - t.z) / (s.x - t.x);
		//y = kz * (x - s.x) + s.z

		//k1 * (x - touchPointThisScreen.x) + touchPointThisScreen.y = k2 * (x - s.x) + s.y
		//(k1 - k2)x = k1 * touchPointThisScreen.x - k2 * s.x - touchPointThisScreen.y + s.y
		//x = (k1 * touchPointThisScreen.x - k2 * s.x - touchPointThisScreen.y + s.y) / (k1 - k2)
		//y = k2 * (x - s.x) + s.y
		float x = (k1 * touchPointThisScreen.x - k2 * s.x - touchPointThisScreen.y + s.y) / (k1 - k2);
		float y = k2 * (x - s.x) + s.y;
		float z = kz * (x - s.x) + s.z;

		return new Vector3(x, y, z);
	}

	public void updateCuttingPlane(Vector3[] vertices) {
		int count = vertices.Length;
		bool[] isVisible = new bool[count];
		for (int i=0;i<count;i++) {
			isVisible[i] = checkVisible(vertices[i]);
		}
		
		int visibleStart = -1;
		for (int i=0;i<count;i++) {
			if (isVisible[i] && !isVisible[(i + count - 1) % count]) {
				visibleStart = i;
			}
		}

		if (visibleStart == -1) {
			if (isVisible[0]) {
				lineRenderer.positionCount = count;
				for (int i=0;i<count;i++) {
					lineRenderer.SetPosition(i, vertices[i] + new Vector3(0.005f, 0, -0.005f));
				}
			}
			else {
				lineRenderer.positionCount = 0;
			}
		}
		else {
			List<Vector3> visibleList = new List<Vector3>();
			visibleList.Add(getIntersection(vertices[(visibleStart + count - 1) % count], vertices[visibleStart]) + new Vector3(0.005f, 0, -0.005f));
			int pos = visibleStart;
			while (isVisible[pos]) {
				visibleList.Add(vertices[pos] + new Vector3(0.005f, 0, -0.005f));
				pos++;
				pos %= count;
			}
			visibleList.Add(getIntersection(vertices[(pos + count - 1) % count], vertices[pos]) + new Vector3(0.005f, 0, -0.005f));
			lineRenderer.positionCount = visibleList.Count;
			for (int i=0;i<visibleList.Count;i++) {
				lineRenderer.SetPosition(i, visibleList[i]);
			}
		}

		string msg = "Slice\n";
		msg += touchPointThisScreen.x + "," + touchPointThisScreen.y + "," + touchPointThisScreen.z + "\n";
		msg += touchPointOtherScreen.x + "," + touchPointOtherScreen.y + "," + touchPointOtherScreen.z + "\n";
		msg += lineRenderer.positionCount + "\n";
		for (int i=0;i < lineRenderer.positionCount;i++) {
			Vector3 tv = lineRenderer.GetPosition(i);
			msg += tv.x + "," + tv.y + "," + tv.z + "\n";
		}
		sender.GetComponent<ServerController>().sendMessage(msg);
	}

	public void endVisualize() {
		lineRenderer.positionCount = 0;
	}
}
