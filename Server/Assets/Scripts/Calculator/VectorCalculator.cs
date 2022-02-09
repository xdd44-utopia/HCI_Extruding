using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class VectorCalculator {

	private static float eps = 0.0000001f;

	public static bool areLinesIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2) {
		bool result = true;
		if (crossProduct(a2 - a1, b2 - a2).magnitude > 0 &&
			crossProduct(a2 - a1, b1 - a2).magnitude > 0 &&
			Vector3.Angle(crossProduct(a2 - a1, b2 - a2), crossProduct(a2 - a1, b1 - a2)) < eps
		) {
			result = false;
		}
		if (crossProduct(b2 - b1, a1 - b2).magnitude > 0 &&
			crossProduct(b2 - b1, a2 - b2).magnitude > 0 &&
			Vector3.Angle(crossProduct(b2 - b1, a1 - b2), crossProduct(b2 - b1, a2 - b2)) < eps
		) {
			result = false;
		}
		return result;
	}

	public static Vector3 crossProduct(Vector3 a, Vector3 b) {
		return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}

	public static float dotProduct(Vector3 a, Vector3 b) {
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	public static float vectorAngle(Vector3 a, Vector3 b) { //radians
		return Mathf.Acos(dotProduct(a, b) / a.magnitude / b.magnitude);
	}

	public static Vector3 vectorMean(Vector3[] vertices) {
		Vector3 result = new Vector3(0, 0, 0);
		if (vertices.Length == 0) {
			return result;
		}
		for (int i=0;i<vertices.Length;i++) {
			result += vertices[i];
		}
		result /= vertices.Length;
		return result;
	}

	public static Vector3 vectorMean(List<Vector3> vertices) {
		Vector3 result = new Vector3(0, 0, 0);
		if (vertices.Count == 0) {
			return result;
		}
		for (int i=0;i<vertices.Count;i++) {
			result += vertices[i];
		}
		result /= vertices.Count;
		return result;
	}

	public static float vectorVariance(Vector3[] vertices) {
		Vector3 mean = vectorMean(vertices);
		float result = 0;
		if (vertices.Length == 0) {
			return 0;
		}
		for (int i=0;i<vertices.Length;i++) {
			result += (vertices[i] - mean).magnitude * (vertices[i] - mean).magnitude;
		}
		result /= vertices.Length;
		return result;
	}

	public static float vectorVariance(List<Vector3> vertices) {
		Vector3 mean = vectorMean(vertices);
		float result = 0;
		if (vertices.Count == 0) {
			return 0;
		}
		for (int i=0;i<vertices.Count;i++) {
			result += (vertices[i] - mean).magnitude * (vertices[i] - mean).magnitude;
		}
		result /= vertices.Count;
		return result;
	}

}