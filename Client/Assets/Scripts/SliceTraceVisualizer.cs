using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliceTraceVisualizer : MonoBehaviour
{
	public LineRenderer crossScreenLine;
	public LineRenderer cuttingPlane;
	public Text debugText;
	private LineRenderer lineRenderer;
	private float traceTimer = 0;
	private float planeTimer = 0;
	private Vector3 INF = new Vector3(10000, 10000, 10000);
	void Start()
	{
		lineRenderer = GetComponent<LineRenderer>();
	}

	void Update()
	{
		traceTimer += Time.deltaTime;
		planeTimer += Time.deltaTime;
		if (traceTimer > 0.2f) {
			endTraceVisualize();
		}
		if (planeTimer > 0.2f) {
			endPlaneVisualize();
		}
	}

	public void updateTrace(Vector3[] vertices) {
		traceTimer = 0;
		lineRenderer.positionCount = vertices.Length;
		lineRenderer.SetPositions(vertices);
	}

	public void updateCuttingPlane(Vector3 tpThis, Vector3 tpOther, Vector3 tpStartThis, Vector3 tpStartOther) {
		planeTimer = 0;
		crossScreenLine.SetPosition(0, tpThis);
		crossScreenLine.SetPosition(1, tpOther);
		cuttingPlane.SetPosition(0, tpThis);
		cuttingPlane.SetPosition(1, tpStartThis);
		cuttingPlane.SetPosition(2, tpStartOther);
		cuttingPlane.SetPosition(3, tpOther);
	}

	private void endTraceVisualize() {
		lineRenderer.positionCount = 0;
	}
	private void endPlaneVisualize() {
		crossScreenLine.SetPosition(0, INF);
		crossScreenLine.SetPosition(1, INF);
		cuttingPlane.SetPosition(0, INF);
		cuttingPlane.SetPosition(1, INF);
		cuttingPlane.SetPosition(2, INF);
		cuttingPlane.SetPosition(3, INF);
	}
}
