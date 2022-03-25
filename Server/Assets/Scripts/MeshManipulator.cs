using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MeshManipulator : MonoBehaviour
{
	public bool computerDebugging;
	public Text modeText;
	public Text focusText;
	public Text debugText;
	public Text debugText2;
	public Image measureButton;
	public Sprite measureRecoverSprite;
	public Sprite measureRecoveredSprite;
	public LineRenderer holeLine;
	private GameObject sliderController;
	private ServerController sender;
	private GameObject extrudeHandle;
	private GameObject gridController;

	//Initial status
	private Vector3 defaultPosition;
	private Vector3 defaultScale;
	private Quaternion defaultRotation;

	//interaction
	private Vector3 prevTouchPosition;
	private float touchDelayTolerance = 0.25f;
	private Vector3 INF = new Vector3(10000, 10000, 10000);
	private float prevAngle = 0;

	//hit
	private ObjectController obj;
	public Mesh defaultMesh;
	private Vector3[] vertices;
	private int[] triangles;

	//Focus
	[HideInInspector]
	private Vector3 centerToHitFace;
	private bool focusingThisScreen = false;
	private bool focusingOtherScreen = false;
	private bool isThisScreenCuttingPlane = false;
	private bool isOtherScreenPenetrate = false;
	private Vector3 footDrop;


	//selection
	[HideInInspector]
	public List<List<int>> selectBoundaries;
	[HideInInspector]
	public List<int> selectTriangles;
	[HideInInspector]
	private Vector3 focusNormal;
	
	//focus
	private int focusTriangleIndex = -1;
	private int prevAngleFitTriangle = -1;
	private Vector3 axisToFocus;
	private float angleToFocus;
	private Vector3 posToFocus;
	private const float focusSpeed = 25;
	private int newSelectTriangle = -2;

	//extrude
	private bool drilling = false;
	private bool extrudeStarted = false;
	private Mesh extrudedMesh;
	private int originalVerticesCount = 0;
	private int extrudedVerticesCount = 0;
	private Vector3 extrudeDir = new Vector3(0, 0, 0);
	private Vector3 extrudeDirLocal = new Vector3(0, 0, 0);
	private Vector3 extrudeStartPos = new Vector3(0, 0, 0);
	private float extrudeDist = 0f;
	private Vector3[] extrudedVerticesOriginal;
	private float extrudeTimer = 0;

	//taper
	private Mesh taperedMesh;
	private Vector3[] taperedVerticesOriginal;
	private Vector3[] taperedVertices;
	private int[] taperedTriangles;
	private Vector3 taperCenter;
	private float taperScale;

	public MeshRenderer cuttingPlaneRenderer;

	//Undo
	private Vector3[] undoVertices;
	private Vector2[] undoUV;
	private int[] undoTriangles;
	private bool undoAvailable = false;
	private Vector3 undoPos;
	private Vector3 undoScale;
	private Quaternion undoRot;

	//Object status
	private bool isThisScreenFocused = false;
	private bool isOtherScreenFocused = false;
	private bool isEdgeAligned = false;

	//Edge align
	private int closestVertex = -1;
	private int secondVertex = -1;

	private Status state = Status.select;
	private enum Status {
		select,
		focus,
		extrude,
		taper,
		drill
	}

	private SelectMode smode = SelectMode.selectObject;
	private enum SelectMode {
		selectFace,
		selectObject
	}

	/* #region Main */
	void Start()
	{
		GameObject findObject;
		findObject = GameObject.Find("Server");
		if (findObject != null) {
			sender = findObject.GetComponent<ServerController>();
		}

		sliderController = GameObject.Find("SliderController");
		extrudeHandle = GameObject.Find("Extrude");
		gridController = GameObject.Find("RulerGrid");

		obj = gameObject.GetComponent<ObjectController>();

		cuttingPlaneRenderer.enabled = false;

		defaultPosition = transform.position;
		defaultRotation = transform.rotation;
		defaultScale = transform.localScale;
	
	}

	void Update() {

		measureButton.sprite = (obj.isRealMeasure ? measureRecoveredSprite : measureRecoverSprite);
		
		vertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
		triangles = gameObject.GetComponent<MeshFilter>().mesh.triangles;
		
		if (isEdgeAligned) {
			if (isThisScreenFocused) {
				string msg = "Extrude\n" + extrudeDist;
				sender.GetComponent<ServerController>().sendMessage(msg);
			}
			else {
				extrudeHandle.SetActive(isOtherScreenFocused);
				extrudeHandle.GetComponent<RectTransform>().anchoredPosition = new Vector2(Screen.width / 2 - extrudeDist / VectorCalculator.camWidth * (Screen.height / 2), 0);
			}
		}
		else {
			extrudeHandle.SetActive(false);
		}

		if (isThisScreenFocused) {
			focusText.text = "Snapped on this screen";
		}
		else if (isOtherScreenFocused) {
			focusText.text = "Snapped on the other screen";
		}
		else {
			focusText.text = "No snapping";
		}
		if (isEdgeAligned) {
			focusText.text = "Edge snapped";
		}

		if (Mathf.Abs(VectorCalculator.angle - prevAngle) > 0.005f) {
			obj.updateTransform();
			if (isOtherScreenFocused) {
				startFocus(false, false);
			}
		}

		modeText.text = state + "";
		if (state == Status.extrude && drilling) {
			modeText.text = "drilling";
			holeLine.enabled = true;
			string msg = "ExtrudeNegative\n" + extrudeDist;
			sender.GetComponent<ServerController>().sendMessage(msg);
		}
		else {
			holeLine.enabled = false;
		}

		switch(state) {
			case Status.select:
				checkAngleFit();
				break;
			case Status.focus:
				focus();
				break;
			case Status.extrude:
				try{
					extrude();
				}
				catch (Exception e) {
					loadUndo();
					state = Status.select;
				}
				break;
			case Status.taper:
				taper();
				break;
		}

		prevAngle = VectorCalculator.angle;

		if (computerDebugging && Input.GetMouseButtonDown(0)) {
			Vector3 mousePos = Input.mousePosition;
			if (mousePos.y > 475) {
				mousePos -= new Vector3(Screen.width / 2, Screen.height / 2, 0);
				mousePos *= Camera.main.orthographicSize / Screen.height * 2;
				castRay(mousePos);
			}
		}

	}
	/* #endregion */

	/* #region Construct highlight */
	public void castRay(Vector3 touchPosition) {

		Vector3 rayStart;
		Vector3 rayDirection;
		RaycastHit hit;

		if (!Camera.main.orthographic) {
			rayStart = Camera.main.gameObject.transform.position;
			rayDirection = touchPosition - rayStart;
		}
		else {
			rayStart = touchPosition;
			if (Mathf.Abs(touchPosition.z) < 0.01f) {
				rayDirection = new Vector3(0, 0, 5);
			}
			else {
				rayDirection = new Vector3(- 5 * Mathf.Cos(Mathf.PI / 2 + VectorCalculator.angle), 0, 5 * Mathf.Sin(Mathf.PI / 2 + VectorCalculator.angle));
			}
		}

		Debug.DrawLine(
			rayStart,
			rayStart + rayDirection * 10f,
			Color.yellow,
			100f,
			true
		);

		if (Physics.Raycast(rayStart, rayDirection, out hit)) {
			focusTriangleIndex = hit.triangleIndex;
			focusNormal = hit.normal;
			smode = SelectMode.selectFace;
			obj.updateSelect(hit.triangleIndex);
		}
		else {
			smode = SelectMode.selectObject;
			obj.updateSelect(-1);
		}
		
	}

	private void checkAngleFit() {
		int targetTriangle = -1;
		if (isEdgeAligned) {
			if (isThisScreenFocused) {
				for (int i=0;i<triangles.Length / 3;i++) {
					int cnt = 0;
					for (int j=0;j<3;j++) {
						if (Mathf.Abs(VectorCalculator.convertFromServer(transform.TransformPoint(vertices[triangles[i * 3 + j]])).z) < 0.05f) {
							cnt++;
						}
					}
					if (cnt == 3) {
						targetTriangle = i;
						break;
					}
				}
			}
			else if (isOtherScreenFocused) {
				for (int i=0;i<triangles.Length / 3;i++) {
					int cnt = 0;
					for (int j=0;j<3;j++) {
						if (Mathf.Abs(transform.TransformPoint(vertices[triangles[i * 3 + j]]).z) < 0.05f) {
							cnt++;
						}
					}
					if (cnt == 3) {
						targetTriangle = i;
						break;
					}
				}
			}
		}
		if (targetTriangle != prevAngleFitTriangle) {
			obj.updateHighlight(-2, targetTriangle);
			prevAngleFitTriangle = targetTriangle;
		}
	}
	/* #endregion */

	/* #region Undo */

	private void prepareUndo() {
		undoAvailable = true;
		undoVertices = new Vector3[vertices.Length];
		undoTriangles = new int[triangles.Length];
		for (int i=0;i<vertices.Length;i++) {
			undoVertices[i] = vertices[i];
		}
		for (int i=0;i<triangles.Length;i++) {
			undoTriangles[i] = triangles[i];
		}
		undoPos = transform.position;
		undoScale = transform.localScale;
		undoRot = transform.rotation;
	}

	public void loadUndo() {
		if (!undoAvailable) {
			return;
		}
		else {
			undoAvailable = false;
		}

		Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
		mesh.Clear();
		mesh.vertices = undoVertices;
		mesh.triangles = undoTriangles;
		mesh.uv = new Vector2[undoVertices.Length];
		gameObject.GetComponent<MeshFilter>().mesh = mesh;

		transform.position = undoPos;
		transform.localScale = undoScale;
		transform.rotation = undoRot;

		obj.updateMesh(true);
		obj.updateTransform();
	}

	/* #endregion */

	/* #region Extrude */
	public void prepareNegativeExtrude() {

		prepareUndo();

		if (!isThisScreenFocused) {
			Debug.Log("Refused");
			return;
		}

		try {

			Vector3 center = new Vector3(0, 0, 0);

			bool isInsideFace(Vector3 p) {
				Vector2 p2D = new Vector2(p.x, p.y);
				Vector2[] vertices2D = new Vector2[vertices.Length];
				for (int i=0;i<vertices.Length;i++) {
					vertices2D[i] = new Vector2(transform.TransformPoint(vertices[i]).x, transform.TransformPoint(vertices[i]).y);
				}
				if (!VectorCalculator.isPointInsidePolygon(vertices2D, selectBoundaries[0], p2D)) {
					Debug.Log("Refused");
					return false;
				}
				for (int i=1;i<selectBoundaries.Count;i++) {
					if (VectorCalculator.isPointInsidePolygon(vertices2D, selectBoundaries[i], p2D)) {
						Debug.Log("Refused");
						return false;
					}
				}
				return true;
			}

			Vector3[] holeVertices = new Vector3[4];
			holeVertices[0] = new Vector3(-0.25f, -0.25f, transform.TransformPoint(vertices[selectBoundaries[0][0]]).z) + center;
			holeVertices[1] = new Vector3(0.25f, -0.25f, transform.TransformPoint(vertices[selectBoundaries[0][0]]).z) + center;
			holeVertices[2] = new Vector3(0.25f, 0.25f, transform.TransformPoint(vertices[selectBoundaries[0][0]]).z) + center;
			holeVertices[3] = new Vector3(-0.25f, 0.25f, transform.TransformPoint(vertices[selectBoundaries[0][0]]).z) + center;
			for (int i=0;i<4;i++) {
				if (!isInsideFace(holeVertices[i])) {
					return;
				}
				holeVertices[i] = transform.InverseTransformPoint(holeVertices[i]);
			}

			List<Vector3> newVerticesList = vertices.ToList();
			newVerticesList.AddRange(holeVertices);
			List<int> newTrianglesList = new List<int>();
			for (int i=0;i<triangles.Length / 3;i++) {
				if (!selectTriangles.Contains(i)) {
					for (int j=0;j<3;j++) {
						newTrianglesList.Add(triangles[i * 3 + j]);
					}
				}
			}

			List<int> holeBoundary = new List<int>{
				newVerticesList.Count - 4,
				newVerticesList.Count - 3,
				newVerticesList.Count - 2,
				newVerticesList.Count - 1
			};
			selectBoundaries.Add(holeBoundary);
			Vector3 localNormal = - transform.InverseTransformPoint(transform.position - new Vector3(0, 0, -1)).normalized;
			for (int i=0;i<selectBoundaries.Count;i++) {
				selectBoundaries[i] = MeshCalculator.clockwiseBoundary(newVerticesList.ToArray(), selectBoundaries[i], localNormal);
			}
			int[] newFaceTriangles = MeshCalculator.triangulation(newVerticesList.ToArray(), selectBoundaries, localNormal);
			newTrianglesList.AddRange(newFaceTriangles);
			newSelectTriangle = newTrianglesList.Count / 3;
			selectBoundaries = new List<List<int>>(){holeBoundary};
			int[] newHoleTriangles = MeshCalculator.triangulation(newVerticesList.ToArray(), selectBoundaries, localNormal);
			newTrianglesList.AddRange(newHoleTriangles);
			selectTriangles = new List<int>(){newTrianglesList.Count / 3 - 1, newTrianglesList.Count / 3 - 2};
			vertices = newVerticesList.ToArray();
			triangles = newTrianglesList.ToArray();

			prepareExtrude(false);

		}
		catch (Exception e) {
			loadUndo();
			cancel();
		}

	}
	
	private void prepareExtrude(bool isThisScreen) {

		if ((!isThisScreen && !isThisScreenFocused) || (isThisScreen && !isOtherScreenFocused)) {
			Debug.Log("Refused");
			return;
		}

		drilling = !isEdgeAligned;

		if (!drilling) {
			prepareUndo();
			newSelectTriangle = triangles.Length / 3;
		}
		
		originalVerticesCount = vertices.Length;
		extrudedVerticesCount = 0;
		for (int i=0;i<selectBoundaries.Count;i++) {
			extrudedVerticesCount += selectBoundaries[i].Count;
		}
		
		extrudeDist = 0;
		extrudeStartPos = transform.position;
		float angle = Math.Max(- VectorCalculator.angle, Mathf.PI / 20f);
		extrudeDir = isThisScreen ? new Vector3(-1, 0, 0) : new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized;
		extrudeDirLocal = transform.InverseTransformPoint(transform.position - extrudeDir).normalized;
		
		List<Vector3> extrudedVerticesList = new List<Vector3>();
		List<int> extrudedTrianglesList = new List<int>();
		//Original mesh
		extrudedVerticesList.AddRange(vertices);
		extrudedTrianglesList.AddRange(triangles);
		//Copy edge
		List<int> pointerToNewVerticeIndex = new List<int>();
		for (int i=0;i<vertices.Length;i++) {
			pointerToNewVerticeIndex.Add(i);
		}
		for (int i=0;i<selectBoundaries.Count;i++) {
			for (int j=0;j<selectBoundaries[i].Count;j++) {
				extrudedVerticesList.Add(vertices[selectBoundaries[i][j]]);
				pointerToNewVerticeIndex[selectBoundaries[i][j]] = extrudedVerticesList.Count - 1;
			}
		}
		//Reassign triangles of extruded face
		for (int i=0;i<selectTriangles.Count;i++) {
			for (int j=0;j<3;j++) {
				extrudedTrianglesList[selectTriangles[i] * 3 + j] = pointerToNewVerticeIndex[extrudedTrianglesList[selectTriangles[i] * 3 + j]];
			}
		}
		//Assign triangles from extrusion
		for (int i=0;i<selectBoundaries.Count;i++) {
			for (int j=0;j<selectBoundaries[i].Count;j++) {
				extrudedTrianglesList.Add(selectBoundaries[i][j]);
				extrudedTrianglesList.Add(selectBoundaries[i][(j + 1) % selectBoundaries[i].Count]);
				extrudedTrianglesList.Add(pointerToNewVerticeIndex[selectBoundaries[i][(j + 1) % selectBoundaries[i].Count]]);
				extrudedTrianglesList.Add(selectBoundaries[i][j]);
				extrudedTrianglesList.Add(pointerToNewVerticeIndex[selectBoundaries[i][(j + 1) % selectBoundaries[i].Count]]);
				extrudedTrianglesList.Add(pointerToNewVerticeIndex[selectBoundaries[i][j]]);
				if (i != 0){
					int t = extrudedTrianglesList[extrudedTrianglesList.Count - 5];
					extrudedTrianglesList[extrudedTrianglesList.Count - 5] = extrudedTrianglesList[extrudedTrianglesList.Count - 4];
					extrudedTrianglesList[extrudedTrianglesList.Count - 4] = t;
					t = extrudedTrianglesList[extrudedTrianglesList.Count - 2];
					extrudedTrianglesList[extrudedTrianglesList.Count - 2] = extrudedTrianglesList[extrudedTrianglesList.Count - 1];
					extrudedTrianglesList[extrudedTrianglesList.Count - 1] = t;
				}
			}
		}

		extrudedMesh = new Mesh();
		extrudedMesh.vertices = extrudedVerticesList.ToArray();
		extrudedMesh.uv = new Vector2[extrudedVerticesList.Count];
		extrudedMesh.triangles = extrudedTrianglesList.ToArray();

		extrudeStarted = false;
		extrudedVerticesOriginal = extrudedVerticesList.ToArray();
		state = Status.extrude;
		extrudeTimer = touchDelayTolerance;

	}

	public void updateExtrudeScale(float factor, bool isThisScreen) {
		factor /= transform.localScale.x;
		if (!isEdgeAligned && !drilling) {
			return;
		}
		if (isEdgeAligned && state == Status.select) {
			if ((isThisScreenFocused && !isThisScreen) || (isOtherScreenFocused && isThisScreen)) {
				prepareExtrude(isThisScreen);
			}
		}
		if (state == Status.extrude) {
			extrudeDist += factor;
			extrudeDist = extrudeDist > 0 ? extrudeDist : 0;
			extrudeDist = (extrudeDist > 3 && drilling) ? 3 : extrudeDist;
			extrudeTimer = touchDelayTolerance;
			extrudeStarted = true;
		}
	}
	private void extrude() {

		if (!isEdgeAligned && !drilling) {
			state = Status.select;
			return;
		}

		Vector3[] extrudedVertices = extrudedMesh.vertices;
		int[] extrudedTrianglesList = extrudedMesh.triangles;
		for (int i=originalVerticesCount;i<originalVerticesCount + extrudedVerticesCount;i++) {
			extrudedVertices[i] = extrudedVerticesOriginal[i] + extrudeDirLocal * extrudeDist * Mathf.Sin(- VectorCalculator.angle) * (drilling ? -1 : 1);
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTrianglesList;

		if (!drilling) {
			transform.position = extrudeStartPos + extrudeDir * transform.localScale.x * Mathf.Sin(- VectorCalculator.angle) * extrudeDist;
		}
		else {
			transform.position = extrudeStartPos;
		}

		if (extrudeDist > 0.01f) {
			gameObject.GetComponent<MeshFilter>().mesh = extrudedMesh;
			obj.updateMesh(true);
			obj.updateTransform();
			obj.updateHighlight(newSelectTriangle, -2);
			obj.updateSelect(newSelectTriangle);
		}

		extrudeTimer -= Time.deltaTime;
		if (extrudeTimer < 0 && extrudeDist > 0.05f) {
			state = Status.select;
			extrudeDist = 0;
			drilling = false;
			obj.updateMesh(true);
			obj.updateTransform();
			cancel();
		}

	}
	/* #endregion */

	/* #region Taper */

	public void switchTaperStatus() {
		if (state != Status.taper) {
			prepareTaper();
		}
		else {
			state = Status.select;
		}
	}

	private void prepareTaper() {

		if (!isThisScreenFocused) {
			return;
		}

		isEdgeAligned = false;

		prepareUndo();

		taperScale = 1;
		taperCenter = new Vector3(0, 0, 0);

		taperedMesh = gameObject.GetComponent<MeshFilter>().mesh;

		for (int i=0;i<selectTriangles.Count;i++) {
			for (int j=0;j<3;j++) {
				taperCenter += vertices[triangles[selectTriangles[i] * 3 + j]];
			}
		}
		taperCenter /= selectTriangles.Count * 3;

		taperedVerticesOriginal = vertices.ToArray();

		state = Status.taper;
		
	}
	public void updateTaperScale(float factor) {
		if (state != Status.taper) {
			return;
		}
		taperScale += factor / 2.5f;
		taperScale = taperScale > 0 ? taperScale : 0;
	}
	private void taper() {

		int faceNum = selectTriangles.Count;
		int edgeLength = selectBoundaries.Count;
		
		taperedVertices = taperedMesh.vertices;
		taperedTriangles = taperedMesh.triangles;

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				taperedVertices[triangles[selectTriangles[i] * 3 + j]] = taperScale * (taperedVerticesOriginal[triangles[selectTriangles[i] * 3 + j]] - taperCenter) + taperCenter;
			}
		}

		taperedMesh.vertices = taperedVertices;
		taperedMesh.triangles = taperedTriangles;

		gameObject.GetComponent<MeshFilter>().mesh = taperedMesh;
		gameObject.GetComponent<MeshCollider>().sharedMesh = taperedMesh;
		obj.updateMesh(false);

	}
	/* #endregion */

	/* #region Split */
	public void enableCuttingPlaneOtherScreen() {
		isOtherScreenPenetrate = true;
		isEdgeAligned = false;
	}
	public void executeCuttingPlaneOtherScreen() {
		cut(false);
		isOtherScreenPenetrate = false;
	}
	private void enableCuttingPlaneThisScreen() {
		isThisScreenCuttingPlane = true;
		cuttingPlaneRenderer.enabled = true;
		isEdgeAligned = false;
	}
	private void executeCuttingPlaneThisScreen() {
		cut(true);
		isThisScreenCuttingPlane = false;
		cuttingPlaneRenderer.enabled = false;
	}
	public void cuttingPlaneSwitcher() {
		if (!isThisScreenCuttingPlane) {
			enableCuttingPlaneThisScreen();
		}
		else {
			executeCuttingPlaneThisScreen();
		}
	}
	public void cut(bool isMainScreen) {

		obj.updateSelect(-1);

		if (state != Status.select) {
			return;
		}
		GameObject slicePlane = GameObject.Find("SlicePlane");
		Vector3 planePos = isMainScreen ? new Vector3(0, 0, 0) : slicePlane.transform.position;
		Vector3 planeNormal = isMainScreen ? new Vector3(0, 0, -1) : slicePlane.transform.rotation * new Vector3(0, 1, 0);

		prepareUndo();

		// x ⋅ planePos - Vector3.Dot(planePos, planeNormal) = 0
		// <= 0 left, > 0 right
		planePos = transform.InverseTransformPoint(planePos);
		planeNormal = transform.InverseTransformPoint(planeNormal + transform.position).normalized;

		MeshCalculator.cutByPlane(ref vertices, ref triangles, planePos, planeNormal);


		//update mesh
		Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		gameObject.GetComponent<MeshFilter>().mesh = mesh;

		obj.updateMesh(true);
		obj.updateTransform();
		cancel();

	}
	/* #endregion */

	/* #region Transform */

	public void startMoving(Vector3 panDelta, bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		if ((isThisScreenFocused && !isMainScreen) || (isOtherScreenFocused && isMainScreen)) {
			panDelta = new Vector3(0, panDelta.y, 0);
		}
		transform.position += panDelta;
		if (isThisScreenFocused) {
			adjustAlign(true);
		}
		if (isOtherScreenFocused) {
			adjustAlign(false);
		}
		transform.position = new Vector3(
			Mathf.Clamp(transform.position.x, - VectorCalculator.camWidth / 2, 3 * VectorCalculator.camWidth / 2),
			Mathf.Clamp(transform.position.y, - VectorCalculator.camHeight / 2, VectorCalculator.camHeight / 2),
			Mathf.Clamp(transform.position.z, 0, VectorCalculator.camWidth)
		);
		obj.updateTransform();
	}

	public void startScaling(float pinchDelta) {
		if (state != Status.select) {
			return;
		}
		if (transform.localScale.x + pinchDelta > 0) {
			transform.localScale += new Vector3(pinchDelta, pinchDelta, pinchDelta);
			isEdgeAligned = false;
			closestVertex = -1;
			secondVertex = -1;
			if (transform.localScale != obj.realMeasure) {
				obj.isRealMeasure = false;
			}
		}
		if (isThisScreenFocused) {
			startFocus(true, false);
		}
		else if (isOtherScreenFocused) {
			startFocus(false, false);
		}
	}

	public void startRotating(float turnDelta, bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		if ((isThisScreenFocused && !isMainScreen) || (isOtherScreenFocused && isMainScreen)) {
			return;
		}
		Quaternion rot = Quaternion.AngleAxis(turnDelta, (isMainScreen ? new Vector3(0, 0, 1) : new Vector3(-Mathf.Sin(-VectorCalculator.angle), 0, Mathf.Cos(-VectorCalculator.angle))));
		transform.rotation = rot * transform.rotation;
		obj.updateTransform();
	}

	private void adjustAlign(bool isMainScreen) {

		if ((isMainScreen && isOtherScreenPenetrate) || (!isMainScreen && isOtherScreenPenetrate)) {
			return;
		}
		
		float closestX = 2147483647;
		int closestI = -1;
		for (int i=0;i<selectBoundaries[0].Count;i++) {
			if (Mathf.Abs(transform.TransformPoint(vertices[selectBoundaries[0][i]]).x - VectorCalculator.camWidth / 2) < closestX) {
				closestX = Mathf.Abs(transform.TransformPoint(vertices[selectBoundaries[0][i]]).x - VectorCalculator.camWidth / 2);
				closestI = i;
			}
		}
		closestVertex = selectBoundaries[0][closestI];
		int leftVertex = selectBoundaries[0][(closestI + 1) % selectBoundaries[0].Count];
		int rightVertex = selectBoundaries[0][(closestI + selectBoundaries[0].Count - 1) % selectBoundaries[0].Count];
		secondVertex =
			Mathf.Abs((transform.TransformPoint(vertices[closestVertex]) + (transform.TransformPoint(vertices[leftVertex]) - transform.TransformPoint(vertices[closestVertex])).normalized).x - VectorCalculator.camWidth / 2) <
			Mathf.Abs((transform.TransformPoint(vertices[closestVertex]) + (transform.TransformPoint(vertices[rightVertex]) - transform.TransformPoint(vertices[closestVertex])).normalized).x - VectorCalculator.camWidth / 2) ?
			leftVertex : rightVertex;

		void updateTwoVertices(ref Vector3 v1, ref Vector3 v2) {
			v1 = transform.TransformPoint(vertices[closestVertex]);
			v2 = transform.TransformPoint(vertices[secondVertex]);
			if (!isMainScreen) {
				v1 = (- VectorCalculator.convertFromServer(v1));
				v2 = (- VectorCalculator.convertFromServer(v2));
			}
		}
		Vector3 closestVector = new Vector3(0, 0, 0);
		Vector3 secondVector = new Vector3(0, 0, 0);
		updateTwoVertices(ref closestVector, ref secondVector);
		Vector3 axis = isMainScreen ? new Vector3(0, 0, 1) : new Vector3(-Mathf.Sin(-VectorCalculator.angle), 0, Mathf.Cos(-VectorCalculator.angle));
		Vector3 dir = isMainScreen ? new Vector3(1, 0, 0) : new Vector3(Mathf.Cos(-VectorCalculator.angle), 0, Mathf.Sin(-VectorCalculator.angle));
		if (closestVector.x > VectorCalculator.camWidth / 2) {
			if (secondVector.x > VectorCalculator.camWidth / 2) {
				if (closestVector.x != secondVector.x) {
					float k = (closestVector.y - secondVector.y) / (closestVector.x - secondVector.x);
					k = 1/k;
					float deltaAngle = Mathf.Atan(k);
					Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, axis);
					transform.rotation = rot * transform.rotation;
				}
				updateTwoVertices(ref closestVector, ref secondVector);
				transform.position -= (closestVector.x - VectorCalculator.camWidth / 2) * dir;
				Vector3 d = (closestVector.x - VectorCalculator.camWidth / 2) * dir;
			}
			else {
				float dist = (closestVector - new Vector3(transform.position.x, transform.position.y, 0)).magnitude;
				Vector3 targetVector = new Vector3(VectorCalculator.camWidth / 2, Mathf.Sqrt(dist * dist - (VectorCalculator.camWidth / 2 - transform.position.x) * (VectorCalculator.camWidth / 2 - transform.position.x)) * (closestVector.y > transform.position.y ? 1 : -1), 0);
				Vector3 a = targetVector - new Vector3(transform.position.x, transform.position.y, 0);
				Vector3 b = closestVector - new Vector3(transform.position.x, transform.position.y, 0);
				float deltaAngle = Mathf.Acos(Vector3.Dot(a, b) / a.magnitude / b.magnitude) * (closestVector.y > transform.position.y ? 1 : -1);
				Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, axis);
				transform.rotation = rot * transform.rotation;
			}
		}
		updateTwoVertices(ref closestVector, ref secondVector);
		if (Mathf.Abs(closestVector.x - VectorCalculator.camWidth / 2) < 0.125f && Mathf.Abs(secondVector.x - VectorCalculator.camWidth / 2) < 0.125f) {
			isEdgeAligned = true;
		}
		
	}

	private void focus() {
		float deltaAngle = angleToFocus - Mathf.Lerp(angleToFocus, 0, focusSpeed * Time.deltaTime);
		angleToFocus -= deltaAngle;
		transform.rotation = Quaternion.AngleAxis(deltaAngle, axisToFocus) * transform.rotation;
		if (Mathf.Abs(angleToFocus) < 0.01f) {
			angleToFocus = 0;
			transform.position = posToFocus;
			obj.updateTransform();
			if (focusingThisScreen) {
				state = Status.select;
				focusingThisScreen = false;
				isThisScreenFocused = true;
				isOtherScreenFocused = false;
			}
			else if (focusingOtherScreen) {
				state = Status.select;
				focusingOtherScreen = false;
				isOtherScreenFocused = true;
				isThisScreenFocused = false;
			}
			Camera.main.orthographic = true;
			gridController.GetComponent<GridController>().isFixed = true;
		}
		transform.position = Vector3.Lerp(transform.position, posToFocus, focusSpeed * Time.deltaTime);
		obj.updateTransform();
	}

	private void startFocus(bool isThisScreen, bool isNewFocus) {

		if (isNewFocus) {
			obj.updateHighlight(focusTriangleIndex, -2);
		}

		if (state == Status.select && (smode == SelectMode.selectFace || !isNewFocus)) {
			axisToFocus = Vector3.Cross(focusNormal, (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(Mathf.Sin(-VectorCalculator.angle), 0, -Mathf.Cos(-VectorCalculator.angle))));
			angleToFocus = Vector3.Angle(focusNormal, (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(Mathf.Sin(-VectorCalculator.angle), 0, -Mathf.Cos(-VectorCalculator.angle))));
			centerToHitFace =
				transform.InverseTransformPoint(
						focusNormal + transform.position
					).normalized *
				Vector3.Dot(
					transform.InverseTransformPoint(
						focusNormal + transform.position
					).normalized,
					vertices[triangles[focusTriangleIndex * 3 + 0]]
				);

			float depth = (transform.TransformPoint(centerToHitFace) - transform.position).magnitude;
			depth += 0.025f;
			float offset = VectorCalculator.camWidth / 2;
			if (!isNewFocus) {
				if (isThisScreen) {
					offset = VectorCalculator.camWidth / 2 - transform.position.x;
				}
				else {
					offset = VectorCalculator.camWidth / 2 + VectorCalculator.convertFromServer(transform.position).x;
				}
			}

			if (isThisScreen) {
				posToFocus = new Vector3(VectorCalculator.camWidth / 2 - offset, isNewFocus ? 0 : transform.position.y, depth);
				focusingThisScreen = true;
			}
			else {
				posToFocus = new Vector3(VectorCalculator.camWidth / 2 + Mathf.Cos(-VectorCalculator.angle) * offset - Mathf.Sin(-VectorCalculator.angle) * depth, isNewFocus ? 0 : transform.position.y, Mathf.Sin(-VectorCalculator.angle) * offset + Mathf.Cos(-VectorCalculator.angle) * depth);
				focusingOtherScreen = true;
			}
			state = Status.focus;
			focusNormal = (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(-Mathf.Sin(VectorCalculator.angle), 0, -Mathf.Cos(-VectorCalculator.angle)));
		}
	}

	public void startNewFocusThisScreen() {
		startFocus(true, true);
	}

	public void startNewFocusOtherScreen() {
		startFocus(false, true);
	}

	/* #endregion */

	/* #region Public */

	public void recordRealMeasure() {
		obj.isRealMeasure = true;
		obj.realMeasure = transform.localScale;
	}

	public void recoverRealMeasure() {
		if (!obj.isRealMeasure) {
			obj.isRealMeasure = true;
			transform.localScale = obj.realMeasure;
			if (isThisScreenFocused) {
				startFocus(true, false);
			}
			else if (isOtherScreenFocused) {
				startFocus(false, false);
			}
		}
	}

	public void restart() {
		cancel();

		transform.position = defaultPosition;
		transform.localScale = defaultScale;
		transform.rotation = defaultRotation;

		gameObject.GetComponent<MeshFilter>().mesh = defaultMesh;
		gameObject.GetComponent<MeshCollider>().sharedMesh = defaultMesh;
		obj.updateMesh(true);
		obj.updateTransform();
	}

	public void cancel() {
		state = Status.select;
		drilling = false;
		isThisScreenFocused = false;
		isOtherScreenFocused = false;
		isEdgeAligned = false;
		obj.updateHighlight(-1, -1);
		obj.updateSelect(-1);
		closestVertex = -1;
		secondVertex = -1;
		prevTouchPosition = INF;
		Camera.main.orthographic = false;
		gridController.GetComponent<GridController>().isFixed = false;
	}

	public void debug1() {

		// Transform debug
		startMoving(new Vector3(0.1f, 0, 0), true);

		// Extrude debug
		isOtherScreenPenetrate = false;
		isThisScreenFocused = true;
		smode = SelectMode.selectFace;
		prepareNegativeExtrude();
		updateExtrudeScale(0.5f, false);
		extrude();
	}
	public void debug2() {
		// Extrude debug
		isThisScreenFocused = true;
		smode = SelectMode.selectFace;
		isEdgeAligned = true;
		prepareExtrude(false);
		updateExtrudeScale(0.5f, false);
		extrude();
	}
	/* #endregion */
}