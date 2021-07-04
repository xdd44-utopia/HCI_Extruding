using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliceTraceVisualizer : MonoBehaviour
{
	public GameObject sender;
	[HideInInspector]
	public Vector3 touchPointThisScreen;
	[HideInInspector]
	public Vector3 touchPointOtherScreen;

	public Text debugText;
	private LineRenderer lineRenderer;
	void Start()
	{
		lineRenderer = GetComponent<LineRenderer>();
	}

	void Update()
	{
		// touchPointThisScreen = new Vector3(0, 0, 0);
		// touchPointOtherScreen = new Vector3(1, 0, 0);
	}

	private bool checkVisible(Vector3 p) {
		float k = (touchPointThisScreen.y - touchPointOtherScreen.y) / (touchPointThisScreen.x - touchPointOtherScreen.x);
		return p.y - touchPointThisScreen.y > k * (p.x - touchPointThisScreen.x);
	}

	private Vector3 getIntersection(Vector3 s, Vector3 t) {
		Vector3 p1 = touchPointThisScreen - touchPointOtherScreen;
		Vector3 p2 = new Vector3(-p1.z, 0, p1.x);
		Vector3 pn = crossProduct(p1, p2);
		return intersectLinePlane(s, t, (touchPointThisScreen+touchPointOtherScreen)/2, pn);
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
			Vector3 startVertex = getIntersection(vertices[(visibleStart + count - 1) % count], vertices[visibleStart]) + new Vector3(0.005f, 0, -0.005f);
			visibleList.Add(startVertex);
			int pos = visibleStart;
			while (isVisible[pos]) {
				visibleList.Add(vertices[pos] + new Vector3(0.005f, 0, -0.005f));
				pos++;
				pos %= count;
			}
			Vector3 endVertex = getIntersection(vertices[(pos + count - 1) % count], vertices[pos]) + new Vector3(0.005f, 0, -0.005f);
			visibleList.Add(endVertex);
			lineRenderer.positionCount = visibleList.Count;
			for (int i=0;i<visibleList.Count;i++) {
				lineRenderer.SetPosition(i, visibleList[i]);
			}

		}

		string msg = "Slice\n";
		msg += lineRenderer.positionCount + "\n";
		for (int i=0;i < lineRenderer.positionCount;i++) {
			Vector3 tv = lineRenderer.GetPosition(i);
			msg += tv.x + "," + tv.y + "," + tv.z + "\n";
		}
		sender.GetComponent<ServerController>().sendMessage(msg);
	}
	private Vector3 crossProduct(Vector3 a, Vector3 b) {
		return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}
	private float dotProduct(Vector3 a, Vector3 b) {
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}
	private Vector3 intersectLinePlane(Vector3 a, Vector3 b, Vector3 p, Vector3 n) { //line passes a and b, plane passes p with normal n
		float t = (dotProduct(p, n) - dotProduct(a, n)) / dotProduct(n, a - b);
		return a + t * (a - b);
	}

	public void endVisualize() {
		lineRenderer.positionCount = 0;
	}
}
