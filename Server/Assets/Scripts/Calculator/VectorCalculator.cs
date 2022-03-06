using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public static class VectorCalculator {

	private static float eps = 0.0000001f;
	private static float inf = 2147483647f;
	private static bool debugging = false;

	public static bool areLineSegmentIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2) {
		bool result = true;
		if (crossProduct(a2 - a1, b2 - a2).magnitude > 0 &&
			crossProduct(a2 - a1, b1 - a2).magnitude > 0 &&
			Vector3.Angle(crossProduct(a2 - a1, b2 - a2), crossProduct(a2 - a1, b1 - a2)) < 0.05f
		) {
			result = false;
		}
		if (crossProduct(b2 - b1, a1 - b2).magnitude > 0 &&
			crossProduct(b2 - b1, a2 - b2).magnitude > 0 &&
			Vector3.Angle(crossProduct(b2 - b1, a1 - b2), crossProduct(b2 - b1, a2 - b2)) < 0.05f
		) {
			result = false;
		}
		return result;
	}

	public static Vector2 getLineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
		float det = (a1.x - a2.x) * (b1.y - b2.y) - (a1.y - a2.y) * (b1.x - b2.x);
		return new Vector2(
			((a1.x * a2.y - a1.y * a2.x) * (b1.x - b2.x) - (a1.x - a2.x) * (b1.x * b2.y - b1.y * b2.x)) / det,
			((a1.x * a2.y - a1.y * a2.x) * (b1.y - b2.y) - (a1.y - a2.y) * (b1.x * b2.y - b1.y * b2.x)) / det
		);
	}

	public static Vector2 getLineSegmentIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
		if (!areLineSegmentIntersect(new Vector3(a1.x, a1.y, 0), new Vector3(a2.x, a2.y, 0), new Vector3(b1.x, b1.y, 0), new Vector3(b2.x, b2.y, 0))) {
			return new Vector2(inf, inf);
		}
		else {
			return getLineIntersection(a1, a2, b1, b2);
		}
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

	public static Vector2[] facePlaneFront(Vector3[] vertices) {

		Vector3 normal = new Vector3(0, 0, 0);
		while (normal.magnitude == 0) {
			int p1 = (int)Random.Range(0, vertices.Length);
			int p2 = (int)Random.Range(0, vertices.Length);
			int p3 = (int)Random.Range(0, vertices.Length);
			normal = VectorCalculator.crossProduct(vertices[p2] - vertices[p1], vertices[p3] - vertices[p2]).normalized;
		}
		normal = normal.normalized;
		
		Vector3 axis = VectorCalculator.crossProduct(normal, new Vector3(0, 0, 1));
		float angle = Vector3.Angle(normal, new Vector3(0, 0, 0));
		Quaternion rotation = axis.magnitude != 0 ? Quaternion.AngleAxis(angle, axis) : Quaternion.identity;

		Vector2[] newVertices = new Vector2[vertices.Length];
		for (int i=0;i<vertices.Length;i++) {
			vertices[i] = rotation * vertices[i];
			newVertices[i] = new Vector2(vertices[i].x, vertices[i].y);
		}

		return newVertices;

	}

	public static bool isPointInsidePolygon(ref Vector2[] vertices, ref List<int> boundary, Vector2 p) {
		
		//If any ray starting from the point intersect with the polygon even times, then it is outside the polygon
		int upperCount = 0;
		int lowerCount = 0;
		for (int i=0;i<boundary.Count;i++) {
			Vector2 a1 = vertices[boundary[i]];
			Vector2 a2 = vertices[boundary[(i + 1) % boundary.Count]];
			if (getLineSegmentIntersection(a1, a2, p, new Vector2(p.x, inf)).x != inf) {
				upperCount++;
			}
			if (getLineSegmentIntersection(a1, a2, p, new Vector2(p.x, -inf)).x != inf) {
				lowerCount++;
			}
		}
		return upperCount % 2 == 1 || lowerCount % 2 == 1;

	}

	public static bool isLineSegmentInsidePolygon(ref Vector2[] vertices, ref List<int> boundary, Vector2 p1, Vector2 p2) {
		
		Vector2 d = p2 - p1;
		p1 += 0.0001f * d;
		p2 -= 0.0001f * d;
		if (!isPointInsidePolygon(ref vertices, ref boundary, p1)) {
			return false;
		}

		for (int i=0;i<boundary.Count;i++) {
			if (areLineSegmentIntersect(
				new Vector3(vertices[boundary[i]].x, vertices[boundary[i]].y, 0),
				new Vector3(vertices[boundary[(i + 1) % boundary.Count]].x, vertices[boundary[(i + 1) % boundary.Count]].y, 0),
				new Vector3(p1.x, p1.y, 0),
				new Vector3(p2.x, p2.y, 0)
			)) {
				return false;
			}
		}

		return true;

	}

}