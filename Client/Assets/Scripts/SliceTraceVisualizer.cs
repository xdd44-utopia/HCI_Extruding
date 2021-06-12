using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceTraceVisualizer : MonoBehaviour
{
	public LineRenderer crossScreenLine;
	private LineRenderer lineRenderer;
	private float timer = 0;
	void Start()
	{
		lineRenderer = GetComponent<LineRenderer>();
	}

	void Update()
	{
		timer += Time.deltaTime;
		if (timer > 0.2f) {
			endVisualize();
		}
	}

	public void updateCuttingPlane(Vector3[] vertices, Vector3 tpThis, Vector3 tpOther) {
		timer = 0;
		crossScreenLine.SetPosition(0, tpThis);
		crossScreenLine.SetPosition(1, tpOther);
		lineRenderer.positionCount = vertices.Length;
		lineRenderer.SetPositions(vertices);
	}

	private void endVisualize() {
		crossScreenLine.SetPosition(0, new Vector3(10000, 10000, 10000));
		crossScreenLine.SetPosition(1, new Vector3(10000, 10000, 10000));
		lineRenderer.positionCount = 0;
	}
}
