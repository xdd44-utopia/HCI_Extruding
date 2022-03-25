using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public static class VectorCalculator {

	private static float inf = 2147483647f;
	public static bool debugging = false;
	public static float camWidth;
	public static float camHeight;
	public static float angle;
	public static int debugInt = 0;

	static void Start()
	{

		Camera cam = Camera.main;
		camHeight = 2f * cam.orthographicSize;
		camWidth = camHeight * cam.aspect;
		debugging = false;
	}
	
	public static bool areLineSegmentIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
		bool onSegment(Vector2 p, Vector2 q, Vector2 r) {
			if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) &&
				q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y))
			return true;
		
			return false;
		}
		int orientation(Vector2 p1, Vector2 p2, Vector2 p3) {
			float ori = (p2.y - p1.y) * (p3.x - p2.x) - (p2.x - p1.x) * (p3.y - p2.y);
			if (Mathf.Approximately(0, ori)) return 0;
			return (ori > 0)? 1: 2;
		}
		int o1 = orientation(a1, a2, b1);
		int o2 = orientation(a1, a2, b2);
		int o3 = orientation(b1, b2, a1);
		int o4 = orientation(b1, b2, a2);
		if (o1 != o2 && o3 != o4) return true;
		if (o1 == 0 && onSegment(a1, b1, a2)) return true;
		if (o2 == 0 && onSegment(a1, b2, a2)) return true;
		if (o3 == 0 && onSegment(b1, a1, b2)) return true;
		if (o4 == 0 && onSegment(b1, a2, b2)) return true;
		return false;
	}

	public static Vector2 getLineIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
		float det = (a1.x - a2.x) * (b1.y - b2.y) - (a1.y - a2.y) * (b1.x - b2.x);
		return new Vector2(
			((a1.x * a2.y - a1.y * a2.x) * (b1.x - b2.x) - (a1.x - a2.x) * (b1.x * b2.y - b1.y * b2.x)) / det,
			((a1.x * a2.y - a1.y * a2.x) * (b1.y - b2.y) - (a1.y - a2.y) * (b1.x * b2.y - b1.y * b2.x)) / det
		);
	}

	public static Vector2 getLineSegmentIntersection(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2) {
		if (!areLineSegmentIntersect(a1, a2, b1, b2)) {
			return new Vector2(inf, inf);
		}
		else {
			return getLineIntersection(a1, a2, b1, b2);
		}
	}
	public static Vector3 getLinePlaneIntersection(Vector3 a, Vector3 b, Vector3 p, Vector3 n) { //line passes a and b, plane passes p with normal n
		float t = (Vector3.Dot(p, n) - Vector3.Dot(a, n)) / Vector3.Dot(n, a - b);
		return a + t * (a - b);
	}
	public static float multXZ(Vector3 from, Vector3 to) {
		return from.x * to.x + from.z * to.z;
	}
	public static Vector3 convertFromServer(Vector3 v) {
		Vector3 origin = new Vector3(camWidth / 2 + camWidth * Mathf.Cos(angle) / 2, 0, - camWidth * Mathf.Sin(angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(angle), 0, - Mathf.Sin(angle));
		Vector3 z = new Vector3(Mathf.Cos(Mathf.PI / 2 - angle), 0, Mathf.Sin(Mathf.PI / 2 - angle));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	public static Vector3 convertToServer(Vector3 v) {
		Vector3 origin = new Vector3(- camWidth / 2 - camWidth * Mathf.Cos(angle) / 2, 0, - camWidth * Mathf.Sin(angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
		Vector3 z = new Vector3(-Mathf.Cos(Mathf.PI / 2 - angle), 0, Mathf.Sin(Mathf.PI / 2 - angle));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	public static float vectorAngle(Vector3 a, Vector3 b) { //radians
		return Vector3.Angle(a, b) * Mathf.PI / 180;
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

	public static Vector2[] facePlaneFront(Vector3[] vertices, Vector3 normal) {
		
		Vector3 axis = Vector3.Cross(normal, new Vector3(0, 0, 1));
		float angle = Vector3.Angle(normal, new Vector3(0, 0, 1));
		Quaternion rotation = axis.magnitude != 0 ? Quaternion.AngleAxis(angle, axis) : Quaternion.identity;

		Vector2[] newVertices = new Vector2[vertices.Length];
		for (int i=0;i<vertices.Length;i++) {
			Vector3 vertice2D = rotation * vertices[i];
			newVertices[i] = new Vector2(vertice2D.x, vertice2D.y);
		}

		return newVertices;

	}

	public static bool isPointInsidePolygon(Vector2[] vertices, List<int> boundary, Vector2 p) {
		
		//If any ray starting from the point intersect with the polygon even times, then it is outside the polygon
		int upperCount = 0;
		int lowerCount = 0;
		for (int i=0;i<boundary.Count;i++) {
			Vector2 a1 = vertices[boundary[i]];
			Vector2 a2 = vertices[boundary[(i + 1) % boundary.Count]];
			if ((a1.x < p.x && a2.x > p.x) || (a1.x > p.x && a2.x < p.x)) {
				if (a1.y + (a2.y - a1.y) / (a2.x - a1.x) * (p.x - a1.x) < p.y) {
					upperCount++;
				}
				if (a1.y + (a2.y - a1.y) / (a2.x - a1.x) * (p.x - a1.x) > p.y) {
					lowerCount++;
				}
			}
		}
		return upperCount % 2 == 1 && lowerCount % 2 == 1;

	}

	public static bool isLineSegmentInsidePolygon(Vector2[] vertices, List<int> boundary, Vector2 p1, Vector2 p2) {
		
		Vector2 d = p2 - p1;
		p1 += 0.0075f * d;
		p2 -= 0.0075f * d;
		if (!isPointInsidePolygon(vertices, boundary, p1) || !isPointInsidePolygon(vertices, boundary, p2)) {
			return false;
		}

		for (int i=0;i<boundary.Count;i++) {
			if (areLineSegmentIntersect(vertices[boundary[i]], vertices[boundary[(i + 1) % boundary.Count]], p1, p2)) {
				return false;
			}
		}

		return true;

	}

	public static List<int> simplifyBoundary(Vector3[] vertices, List<int> orignalBoundary) {
		List<int> boundary = new List<int>();
		boundary.AddRange(orignalBoundary);
		while (true) {
			int prevCount = boundary.Count;
			for (int i=0;i < boundary.Count;i++) {
				int prev = (i + boundary.Count - 1) % boundary.Count;
				int next = (i + 1) % boundary.Count;
				if (Vector3.Cross(vertices[boundary[next]] - vertices[boundary[i]], vertices[boundary[i]] - vertices[boundary[prev]]).magnitude < 0.001f) {
				// if (Vector3.Angle(vertices[boundary[next]] - vertices[boundary[i]], vertices[boundary[i]] - vertices[boundary[prev]]) < 1f) {
					boundary.RemoveAt(i);
					break;
				}
			}
			if (boundary.Count == prevCount) {
				break;
			}
		}
		return boundary;
	}

	public static string VectorToString(Vector3[] vertices) {
		string str = "";
		for (int i=0;i<vertices.Length;i++) {
			str += vertices[i].x + ", " + vertices[i].y + ", " + vertices[i].z + "\n";
		}
		return str;
	}
	public static string VectorToString(List<Vector3> vertices) {
		string str = "";
		for (int i=0;i<vertices.Count;i++) {
			str += vertices[i].x + ", " + vertices[i].y + ", " + vertices[i].z + "\n";
		}
		return str;
	}

}