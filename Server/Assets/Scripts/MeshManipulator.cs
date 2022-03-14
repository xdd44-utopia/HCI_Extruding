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
	private GameObject sliceTraceVisualizer;
	private GameObject sliderController;
	private ServerController sender;
	private GameObject extrudeHandle;
	private GameObject gridController;
	//interaction
	[HideInInspector]
	public Vector3 touchPosition;
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

	//extrude
	private Mesh extrudedMesh;
	private int originalVerticesCount = 0;
	private int extrudedVerticesCount = 0;
	private Vector3 extrudeDir = new Vector3(0, 0, 0);
	private Vector3 extrudeDirLocal = new Vector3(0, 0, 0);
	private Vector3 extrudeStartPos = new Vector3(0, 0, 0);
	private float extrudeDist = 0f;
	private Vector3[] extrudedVerticesOriginal;
	private float extrudeTimer = 0;

	//drill
	public float drillDist = 0f;

	//taper
	private Mesh taperedMesh;
	private Vector3[] taperedVertices;
	private int[] taperedTriangles;
	private Vector3 taperCenter;
	private float taperScale;
	private float taperTimer = 0;
	private bool taperStarted = false;

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

		sliceTraceVisualizer = GameObject.Find("Slice Trace");
		sliderController = GameObject.Find("SliderController");
		extrudeHandle = GameObject.Find("Extrude");
		gridController = GameObject.Find("RulerGrid");

		obj = gameObject.GetComponent<ObjectController>();

		touchPosition = INF;

		cuttingPlaneRenderer.enabled = false;

	
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
				extrudeHandle.GetComponent<RectTransform>().anchoredPosition = new Vector2(360 - extrudeDist / VectorCalculator.camWidth * 772, 0);
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
			castRay();
		}

	}
	/* #endregion */

	/* #region Construct highlight */
	public void castRay() {

		Vector3 mousePos = touchPosition;
		if (computerDebugging) {
			mousePos = Input.mousePosition;
			mousePos -= new Vector3(Screen.width / 2, Screen.height / 2, 0);
			mousePos *= Camera.main.orthographicSize / Screen.height * 2;
		}

		Vector3 rayStart;
		Vector3 rayDirection;
		RaycastHit hit;

		if (!Camera.main.orthographic) {
			rayStart = Camera.main.gameObject.transform.position;
			rayDirection = mousePos - rayStart;
		}
		else {
			rayStart = mousePos;
			if (Mathf.Abs(mousePos.z) < 0.01f) {
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

			obj.updateSelect(hit.triangleIndex);
			focusTriangleIndex = hit.triangleIndex;
			focusNormal = hit.normal;
			smode = SelectMode.selectFace;

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
		mesh.MarkModified();
		mesh.RecalculateNormals();
		gameObject.GetComponent<MeshFilter>().mesh = mesh;

		transform.position = undoPos;
		transform.localScale = undoScale;
		transform.rotation = undoRot;

		obj.updateTransform();
		obj.updateMesh(true);
	}

	/* #endregion */

	/* #region Extrude */
	
	private void prepareExtrude(bool isThisScreen) {

		if (!isEdgeAligned) {
			return;
		}

		prepareUndo();
		
		originalVerticesCount = vertices.Length;
		extrudedVerticesCount = 0;
		for (int i=0;i<selectBoundaries.Count;i++) {
			extrudedVerticesCount += selectBoundaries[i].Count;
		}
		
		extrudeDist = 0;
		extrudeStartPos = transform.position;
		extrudeDir = isThisScreen ? new Vector3(-1, 0, 0) : new Vector3(Mathf.Cos(-VectorCalculator.angle), 0, Mathf.Sin(-VectorCalculator.angle)).normalized;
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

		extrudedMesh.MarkModified();
		extrudedMesh.RecalculateNormals();

		extrudedVerticesOriginal = extrudedVerticesList.ToArray();
		state = Status.extrude;
		extrudeTimer = touchDelayTolerance;

	}

	public void updateExtrudeScale(float factor, bool isThisScreen) {
		factor /= transform.localScale.x;
		if (smode != SelectMode.selectFace) {
			return;
		}
		if (!isEdgeAligned) {
			return;
		}
		if (smode == SelectMode.selectFace && isEdgeAligned && state == Status.select) {
			if ((isThisScreenFocused && !isThisScreen) || (isOtherScreenFocused && isThisScreen)) {
				prepareExtrude(isThisScreen);
			}
		}
		if (state == Status.extrude) {
			extrudeDist += factor;
			extrudeDist = extrudeDist > 0 ? extrudeDist : 0;
			extrudeTimer = touchDelayTolerance;
		}
	}
	private void extrude() {

		if (smode != SelectMode.selectFace || !isEdgeAligned) {
			state = Status.select;
			return;
		}

		Vector3[] extrudedVertices = extrudedMesh.vertices;
		int[] extrudedTrianglesList = extrudedMesh.triangles;
		for (int i=originalVerticesCount;i<originalVerticesCount + extrudedVerticesCount;i++) {
			extrudedVertices[i] = extrudedVerticesOriginal[i] + extrudeDirLocal * extrudeDist;
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTrianglesList;
		extrudedMesh.MarkModified();
		extrudedMesh.RecalculateNormals();

		transform.position = extrudeStartPos + extrudeDir * transform.localScale.x * extrudeDist;

		if (extrudeDist > 0.01f) {
			gameObject.GetComponent<MeshFilter>().mesh = extrudedMesh;
			obj.updateTransform();
			obj.updateMesh(true);
		}

		extrudeTimer -= Time.deltaTime;
		if (extrudeTimer < 0) {
			state = Status.select;
			if (extrudeDist < 0.02f) {
				loadUndo();
			}
			extrudeDist = 0;
		}

	}
	/* #endregion */

	/* #region Taper */

	public void prepareTaper() {

		if (smode != SelectMode.selectFace) {
			return;
		}

		isEdgeAligned = false;
		closestVertex = -1;
		secondVertex = -1;

		taperStarted = false;

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

		state = Status.taper;

		taperTimer = touchDelayTolerance;
		
	}
	public void updateTaperScale(float factor) {

		if (state != Status.taper) {
			return;
		}
		
		taperScale += factor / 2.5f;
		taperScale = taperScale > 0 ? taperScale : 0;
		taperTimer = touchDelayTolerance;
		taperStarted = true;
	}
	private void taper() {

		int faceNum = selectTriangles.Count;
		int edgeLength = selectBoundaries.Count;
		
		taperedVertices = taperedMesh.vertices;
		taperedTriangles = taperedMesh.triangles;

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				taperedVertices[triangles[selectTriangles[i] * 3 + j]] = taperScale * (vertices[triangles[selectTriangles[i] * 3 + j]] - taperCenter) + taperCenter;
			}
		}

		taperedMesh.vertices = taperedVertices;
		taperedMesh.triangles = taperedTriangles;
		taperedMesh.MarkModified();
		taperedMesh.RecalculateNormals();

		gameObject.GetComponent<MeshFilter>().mesh = taperedMesh;
		gameObject.GetComponent<MeshCollider>().sharedMesh = taperedMesh;
		obj.updateMesh(false);

		// taperScale = 1;

		taperTimer -= Time.deltaTime;
		if ((taperTimer < 0 && taperStarted) || taperScale == 0) {
			state = Status.select;
		}

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
		if (state != Status.select) {
			return;
		}
		GameObject slicePlane = GameObject.Find("SlicePlane");
		Vector3 planePos = isMainScreen ? new Vector3(0, 0, 0) : slicePlane.transform.position;
		Vector3 planeNormal = isMainScreen ? new Vector3(0, 0, -1) : slicePlane.transform.rotation * new Vector3(0, 1, 0);

		prepareUndo();

		// x ⋅ planePos - VectorCalculator.dotProduct(planePos, planeNormal) = 0
		// <= 0 left, > 0 right
		planePos = transform.InverseTransformPoint(planePos);
		planeNormal = transform.InverseTransformPoint(planeNormal + transform.position).normalized;

		MeshCalculator.cutByPlane(ref vertices, ref triangles, planePos, planeNormal);

		//relocate mesh center
		float minx = 2147483647;
		float miny = 2147483647;
		float minz = 2147483647;
		float maxx = -2147483647;
		float maxy = -2147483647;
		float maxz = -2147483647;
		for (int i=0;i<vertices.Length;i++) {
			minx = Math.Min(minx, vertices[i].x);
			miny = Math.Min(miny, vertices[i].y);
			minz = Math.Min(minz, vertices[i].z);
			maxx = Math.Max(maxx, vertices[i].x);
			maxy = Math.Max(maxy, vertices[i].y);
			maxz = Math.Max(maxz, vertices[i].z);
		}
		Vector3 meshCenter = new Vector3((maxx + minx) / 2, (maxy + miny) / 2, (maxz + minz) / 2);
		for (int i=0;i<vertices.Length;i++) {
			vertices[i] -= meshCenter;
		}
		meshCenter = transform.TransformPoint(meshCenter);
		transform.position = meshCenter;

		//update mesh

		Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.MarkModified();
		mesh.RecalculateNormals();
		gameObject.GetComponent<MeshFilter>().mesh = mesh;

		obj.updateMesh(true);
		obj.updateTransform();

	}
	/* #endregion */

	/* #region Transform */

	public void startMoving(Vector3 panDelta, bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		if (!isThisScreenFocused && !isOtherScreenFocused) {
			transform.position += panDelta;
		}
		else {
			transform.position += new Vector3(0, panDelta.y, 0);
			if (isThisScreenFocused && isMainScreen) {
				if (isEdgeAligned) {
					if (panDelta.x < 0) {
						transform.position += new Vector3(panDelta.x, 0, 0);
						isEdgeAligned = false;
						closestVertex = -1;
						secondVertex = -1;
					}
				}
				else {
					transform.position += new Vector3(panDelta.x, 0, 0);
					adjustAlign(true);
				}
			}
			if (isOtherScreenFocused && !isMainScreen) {
				if (isEdgeAligned) {
					if (panDelta.x > 0) {
						transform.position += new Vector3(panDelta.x, 0, panDelta.z);
						isEdgeAligned = false;
						closestVertex = -1;
						secondVertex = -1;
					}
				}
				else {
					transform.position += new Vector3(panDelta.x, 0, panDelta.z);
					adjustAlign(false);
				}
			}
		}
		obj.updateTransform();
	}

	public void startScaling(float pinchDelta, bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		if (transform.localScale.x + pinchDelta > 0) {
			transform.localScale += new Vector3(pinchDelta, pinchDelta, pinchDelta);
			isEdgeAligned = false;
			closestVertex = -1;
			secondVertex = -1;
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
		
		List<int> sortedVerticesPointer = new List<int>();
		for (int i=0;i<selectBoundaries[0].Count;i++) {
			sortedVerticesPointer.Add(i);
		}
		sortedVerticesPointer.Sort((a, b) => {
			float ax = transform.TransformPoint(vertices[selectBoundaries[0][a]]).x;
			float bx = transform.TransformPoint(vertices[selectBoundaries[0][b]]).x;
			if (ax - bx == 0) {
				return 0;
			}
			else if (ax - bx > 0) {
				return isMainScreen ? -1 : 1;
			}
			else {
				return isMainScreen ? 1 : -1;
			}
		});
		closestVertex = selectBoundaries[0][sortedVerticesPointer[0]];
		secondVertex = selectBoundaries[0][sortedVerticesPointer[1]];
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
				float deltaAngle = Mathf.Acos(VectorCalculator.dotProduct(a, b) / a.magnitude / b.magnitude) * (closestVector.y > transform.position.y ? 1 : -1);
				Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, axis);
				transform.rotation = rot * transform.rotation;
			}
		}
		updateTwoVertices(ref closestVector, ref secondVector);
		if (Mathf.Abs(closestVector.x - VectorCalculator.camWidth / 2) < 0.01f && Mathf.Abs(secondVector.x - VectorCalculator.camWidth / 2) < 0.01f) {
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
			axisToFocus = VectorCalculator.crossProduct(focusNormal, (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(Mathf.Sin(-VectorCalculator.angle), 0, -Mathf.Cos(-VectorCalculator.angle))));
			angleToFocus = Vector3.Angle(focusNormal, (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(Mathf.Sin(-VectorCalculator.angle), 0, -Mathf.Cos(-VectorCalculator.angle))));
			centerToHitFace =
				transform.InverseTransformPoint(
						focusNormal + transform.position
					).normalized *
				VectorCalculator.dotProduct(
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

		transform.position = new Vector3(0, 0, 2f);
		transform.localScale = new Vector3(2, 2, 2);
		transform.rotation = Quaternion.Euler(30, 60, 45);

		gameObject.GetComponent<MeshFilter>().mesh = defaultMesh;
		gameObject.GetComponent<MeshCollider>().sharedMesh = defaultMesh;
		obj.updateTransform();
		obj.updateMesh(true);
	}

	public void cancel() {
		state = Status.select;
		isThisScreenFocused = false;
		isOtherScreenFocused = false;
		isEdgeAligned = false;
		obj.updateHighlight(-1, -1);
		closestVertex = -1;
		secondVertex = -1;
		touchPosition = INF;
		prevTouchPosition = INF;
		Camera.main.orthographic = false;
		gridController.GetComponent<GridController>().isFixed = false;
	}

	public void debug1() {

		// Transform debug
		startMoving(new Vector3(0.1f, 0, 0), true);

		// Extrude debug
		prepareExtrude(false);
	}
	public void debug2() {
		updateExtrudeScale(0.1f, true);
		extrude();
	}
	/* #endregion */
}