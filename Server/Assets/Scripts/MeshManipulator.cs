using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class MeshManipulator : MonoBehaviour
{
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
	private float touchDelayTolerance = 0.1f;
	private Vector3 INF = new Vector3(10000, 10000, 10000);
	private float prevAngle = 0;

	//hit
	private ObjectController obj;
	public Mesh defaultMesh;
	private RaycastHit hit;
	private Vector3[] hitVertices;
	private int[] hitTriangles;
	private int hitVerticesNum;
	private int hitTrianglesNum;

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
	public List<int> selectEdgeVertices;
	private List<int> prevEdgeVertices;
	[HideInInspector]
	public List<int> selectTriangles;
	[HideInInspector]
	public Vector3 focusNormal;
	
	//focus
	private int focusTriangleIndex = -1;
	private int prevAngleFitTriangle = -1;
	private Vector3 axisToFocus;
	private float angleToFocus;
	private Vector3 posToFocus;
	private const float focusSpeed = 25;

	//extrude
	private Mesh extrudedMesh;
	private Vector3 extrudeDir = new Vector3(0, 0, 0);
	private Vector3 extrudeStartPos = new Vector3(0, 0, 0);
	private float extrudeDist = 0f;
	private Vector3[] extrudedVertices;
	private Vector3[] extrudedVerticesOriginal;
	private int[] extrudedTriangles;
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

	//slice
	private List<Vector3> leftVerticesList;
	private List<Vector3> rightVerticesList;
	private List<Vector3> edgeVerticesList;
	private List<Vector3> cuttingPlaneVerticesList;
	private Vector3 planeNormalWorld;
	public MeshRenderer cuttingPlaneRenderer;

	//Undo
	private Vector3[] undoVertices;
	private Vector2[] undoUV;
	private int[] undoTriangles;
	private bool undoAvailable = false;
	private Vector3 undoPos;

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
			focusText.text += "\nEdge snapped";
		}

		if (state == Status.select) {
			hitVertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
			hitTriangles = gameObject.GetComponent<MeshFilter>().mesh.triangles;
			hitVerticesNum = hitVertices.Length;
			hitTrianglesNum = hitTriangles.Length;
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

		// if (Input.GetMouseButtonDown(0)) {
		// 	castRay();
		// }

	}
	/* #endregion */

	/* #region Construct highlight */
	public void castRay() {

		Vector3 mousePos = touchPosition;
		// mousePos = Input.mousePosition;
		// mousePos -= new Vector3(Screen.width / 2, Screen.height / 2, 0);
		// mousePos *= Camera.main.orthographicSize / Screen.height * 2;

		Vector3 rayStart;
		Vector3 rayDirection;

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
			
			Debug.Log("Cast ray");

			prevEdgeVertices = new List<int>();
			obj.updateSelect(hit.triangleIndex);
			focusTriangleIndex = hit.triangleIndex;
			smode = SelectMode.selectFace;

		}
		
	}

	private void checkAngleFit() {
		int targetTriangle = -1;
		if (isEdgeAligned) {
			if (isThisScreenFocused) {
				for (int i=0;i<hitTriangles.Length / 3;i++) {
					int cnt = 0;
					for (int j=0;j<3;j++) {
						if (Mathf.Abs(VectorCalculator.convertFromServer(obj.transform.TransformPoint(hitVertices[hitTriangles[i * 3 + j]])).z) < 0.05f) {
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
				for (int i=0;i<hitTriangles.Length / 3;i++) {
					int cnt = 0;
					for (int j=0;j<3;j++) {
						if (Mathf.Abs(obj.transform.TransformPoint(hitVertices[hitTriangles[i * 3 + j]]).z) < 0.05f) {
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
		undoVertices = new Vector3[hitVertices.Length];
		undoTriangles = new int[hitTriangles.Length];
		for (int i=0;i<hitVertices.Length;i++) {
			undoVertices[i] = hitVertices[i];
		}
		for (int i=0;i<hitTriangles.Length;i++) {
			undoTriangles[i] = hitTriangles[i];
		}
		undoPos = obj.transform.position;
	}

	public void loadUndo() {
		if (!undoAvailable) {
			return;
		}
		else {
			undoAvailable = false;
		}
		hitVertices = gameObject.GetComponent<MeshFilter>().mesh.vertices;
		hitTriangles = gameObject.GetComponent<MeshFilter>().mesh.triangles;
		hitVertices = new Vector3[undoVertices.Length];
		hitTriangles = new int[undoTriangles.Length];
		for (int i=0;i<hitVertices.Length;i++) {
			hitVertices[i] = undoVertices[i];
		}
		for (int i=0;i<hitTriangles.Length;i++) {
			hitTriangles[i] = undoTriangles[i];
		}

		Mesh tempMesh = gameObject.GetComponent<MeshFilter>().mesh;

		tempMesh.triangles = hitTriangles;
		tempMesh.vertices = hitVertices;
		tempMesh.MarkModified();
		tempMesh.RecalculateNormals();
		gameObject.GetComponent<MeshFilter>().mesh = tempMesh;
		gameObject.GetComponent<MeshCollider>().sharedMesh = tempMesh;
		obj.updateTransform();
		obj.updateMesh(true);
		obj.transform.position = undoPos;
	}

	/* #endregion */

	/* #region Extrude */
	
	private void prepareExtrude(bool isThisScreen) {

		prepareUndo();

		int faceNum = selectTriangles.Count;
		int edgeLength = selectEdgeVertices.Count;
		
		extrudeDist = 0;
		extrudeStartPos = obj.transform.position;
		if (isThisScreen) {
			extrudeDir = new Vector3(-1, 0, 0);
		}
		else {
			extrudeDir = new Vector3(Mathf.Cos(-VectorCalculator.angle), 0, Mathf.Sin(-VectorCalculator.angle));
		}

		extrudedMesh = new Mesh();
		extrudedMesh.vertices = new Vector3[hitVerticesNum + edgeLength];
		extrudedMesh.uv = new Vector2[hitVerticesNum + edgeLength];
		extrudedMesh.triangles = new int[hitTrianglesNum + edgeLength * 6];
		
		extrudedVertices = extrudedMesh.vertices;
		extrudedTriangles = extrudedMesh.triangles;
		//Original mesh
		for (int i=0;i<hitVerticesNum;i++) {
			extrudedVertices[i] = hitVertices[i];
		}
		//Copy edge
		for (int i=0;i<edgeLength;i++) {
			extrudedVertices[hitVerticesNum + i] = hitVertices[selectEdgeVertices[i]];
		}
		//Original triangles
		for (int i=0;i<hitTrianglesNum;i++) {
			extrudedTriangles[i] = hitTriangles[i];
		}
		//Reassign triangles of extruded face
		for (int i=0;i<selectTriangles.Count;i++) {
			for (int j=0;j<3;j++) {
				for (int k=0;k<edgeLength;k++) {
					if (Vector3.Distance(extrudedVertices[hitVerticesNum + k], extrudedVertices[extrudedTriangles[selectTriangles[i] * 3 + j]]) < 0.0001f) {
						extrudedTriangles[selectTriangles[i] * 3 + j] = hitVerticesNum + k;
					}
				}
			}
		}
		//Assign triangles from extrusion
		for (int i=0;i<edgeLength;i++) {
			extrudedTriangles[hitTrianglesNum + i * 6 + 0] = selectEdgeVertices[i];
			extrudedTriangles[hitTrianglesNum + i * 6 + 1] = selectEdgeVertices[(i+1)%edgeLength];
			extrudedTriangles[hitTrianglesNum + i * 6 + 2] = hitVerticesNum + (i+1)%edgeLength;
			extrudedTriangles[hitTrianglesNum + i * 6 + 3] = selectEdgeVertices[i];
			extrudedTriangles[hitTrianglesNum + i * 6 + 4] = hitVerticesNum + (i+1)%edgeLength;
			extrudedTriangles[hitTrianglesNum + i * 6 + 5] = hitVerticesNum + i;
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTriangles;

		extrudedMesh.MarkModified();
		extrudedMesh.RecalculateNormals();

		extrudedVerticesOriginal = new Vector3[extrudedVertices.Length];
		for (int i=0;i<hitVerticesNum + edgeLength;i++) {
			extrudedVerticesOriginal[i] = extrudedVertices[i];
		}
		state = Status.extrude;
		extrudeTimer = touchDelayTolerance;

	}

	public void updateExtrudeScale(float factor, bool isThisScreen) {
		factor /= obj.transform.localScale.x;
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
			debugText2.text = factor + "";
		}
	}
	private void extrude() {

		if (smode != SelectMode.selectFace) {
			state = Status.select;
			return;
		}


		extrudedVertices = extrudedMesh.vertices;
		extrudedTriangles = extrudedMesh.triangles;
		int faceNum = selectTriangles.Count;
		int edgeLength = selectEdgeVertices.Count;
		Vector3 localNormal = obj.transform.InverseTransformPoint(obj.transform.position - extrudeDir);

		localNormal = localNormal.normalized;
		for (int i=0;i<edgeLength;i++) {
			extrudedVertices[hitVerticesNum + i] = extrudedVerticesOriginal[hitVerticesNum + i] + localNormal * extrudeDist;
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTriangles;
		extrudedMesh.MarkModified();
		extrudedMesh.RecalculateNormals();

		extrudeDir = extrudeDir.normalized * obj.transform.localScale.x;

		obj.transform.position = extrudeStartPos + extrudeDir * extrudeDist;

		debugText2.text += " " + extrudeDir * extrudeDist;


		if (extrudeDist > 0.02f) {
			Mesh tempMesh = new Mesh();

			tempMesh.vertices = new Vector3[extrudedVertices.Length];
			tempMesh.uv = new Vector2[extrudedVertices.Length];
			tempMesh.triangles = new int[extrudedTriangles.Length];

			Vector3[] tempVertices = tempMesh.vertices;
			int[] tempTriangles = tempMesh.triangles;
			
			for (int i=0;i<extrudedVertices.Length;i++) {
				tempVertices[i] = extrudedVertices[i];
			}
			for (int i=0;i<extrudedTriangles.Length;i++) {
				tempTriangles[i] = extrudedTriangles[i];
			}
			tempMesh.vertices = tempVertices;
			tempMesh.triangles = tempTriangles;
			tempMesh.MarkModified();
			tempMesh.RecalculateNormals();

			gameObject.GetComponent<MeshFilter>().mesh = tempMesh;
			gameObject.GetComponent<MeshCollider>().sharedMesh = tempMesh;
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

		int faceNum = selectTriangles.Count;
		int edgeLength = selectEdgeVertices.Count;

		taperedMesh = gameObject.GetComponent<MeshFilter>().mesh;

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				taperCenter += hitVertices[hitTriangles[selectTriangles[i] * 3 + j]];
			}
		}
		taperCenter /= faceNum * 3;

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
		int edgeLength = selectEdgeVertices.Count;
		
		taperedVertices = taperedMesh.vertices;
		taperedTriangles = taperedMesh.triangles;

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				taperedVertices[hitTriangles[selectTriangles[i] * 3 + j]] = taperScale * (hitVertices[hitTriangles[selectTriangles[i] * 3 + j]] - taperCenter) + taperCenter;
			}
		}

		debugText2.text = taperScale + "";

		for (int i=0;i<taperedVertices.Length;i++) {
			for (int j=0;j<edgeLength;j++) {
				if (hitVertices[selectEdgeVertices[j]] == hitVertices[i]) {
					taperedVertices[i] = taperScale * (hitVertices[selectEdgeVertices[j]] - taperCenter) + taperCenter;
				}
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
		startSlice(true, false);
		executeSlice();
		isOtherScreenPenetrate = false;
	}
	private void enableCuttingPlaneThisScreen() {
		isThisScreenCuttingPlane = true;
		cuttingPlaneRenderer.enabled = true;
		isEdgeAligned = false;
	}
	private void executeCuttingPlaneThisScreen() {
		startSlice(true, true);
		executeSlice();
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
	public void startSlice(bool isScreenCut, bool isMainScreen) {
		GameObject slicePlane = GameObject.Find("SlicePlane");
		if (state == Status.select) {
			if (isScreenCut && isMainScreen) {
				prepareSlice(new Vector3(0, 0, 0.025f), new Vector3(0, 0, -1), isScreenCut);
			}
			else {
				Vector3 normalTemp = slicePlane.transform.rotation * new Vector3(0, 1, 0);
				prepareSlice(slicePlane.transform.position, normalTemp, isScreenCut);
			}
		}
	}
	private void prepareSlice(Vector3 planePos, Vector3 planeNormal, bool isScreenCut) {

		prepareUndo();

		// x ⋅ planePos - VectorCalculator.dotProduct(planePos, planeNormal) = 0
		// <= 0 left, > 0 right
		planeNormalWorld = new Vector3(planeNormal.x, planeNormal.y, planeNormal.z);
		planePos = obj.transform.InverseTransformPoint(planePos);
		planeNormal = obj.transform.InverseTransformPoint(planeNormal + obj.transform.position);
		Vector3 avoidZeroVector = new Vector3(UnityEngine.Random.Range(0.01f, 0.02f), UnityEngine.Random.Range(0.01f, 0.02f), UnityEngine.Random.Range(0.01f, 0.02f));
		if (planeNormal.x != 0) {
			avoidZeroVector.x = - (avoidZeroVector.y * planeNormal.y + avoidZeroVector.z * planeNormal.z) / planeNormal.x;
		}
		else if (planeNormal.y != 0) {
			avoidZeroVector.y = - (avoidZeroVector.x * planeNormal.x + avoidZeroVector.z * planeNormal.z) / planeNormal.y;
		}
		else if (planeNormal.z != 0) {
			avoidZeroVector.z = - (avoidZeroVector.x * planeNormal.x + avoidZeroVector.y * planeNormal.y) / planeNormal.z;
		}
		planePos += avoidZeroVector;

		leftVerticesList = new List<Vector3>();
		rightVerticesList = new List<Vector3>();
		edgeVerticesList = new List<Vector3>();

		//Calculate edge of cutting plane & reconstruct faces along the edge
		int[] triangleSide = new int[hitTrianglesNum / 3]; // -1 left, 0 cross, 1 right
		float pn = VectorCalculator.dotProduct(planePos, planeNormal);
		for (int i=0;i<hitTrianglesNum / 3;i++) {
			float[] verticesPos = new float[3]{
				VectorCalculator.dotProduct(hitVertices[hitTriangles[i * 3 + 0]], planeNormal) - pn,
				VectorCalculator.dotProduct(hitVertices[hitTriangles[i * 3 + 1]], planeNormal) - pn,
				VectorCalculator.dotProduct(hitVertices[hitTriangles[i * 3 + 2]], planeNormal) - pn
			};
			if (verticesPos[0] <= 0 && verticesPos[1] <= 0 && verticesPos[2] <= 0) {
				triangleSide[i] = -1;
				for (int j=0;j<3;j++) {
					leftVerticesList.Add(hitVertices[hitTriangles[i * 3 + j]]);
				}
			}
			else if (verticesPos[0] > 0 && verticesPos[1] > 0 && verticesPos[2] > 0) {
				triangleSide[i] = 1;
				for (int j=0;j<3;j++) {
					rightVerticesList.Add(hitVertices[hitTriangles[i * 3 + j]]);
				}
			}
			else {
				triangleSide[i] = 0;
				for (int j=0;j<3;j++) {
					Vector3[] curVec = new Vector3[3]{
						hitVertices[hitTriangles[i * 3 + j]],
						hitVertices[hitTriangles[i * 3 + ((j+1)%3)]],
						hitVertices[hitTriangles[i * 3 + ((j+2)%3)]]
					};
					if (verticesPos[j] <= 0 && verticesPos[(j+1)%3] <= 0) {
						Vector3 newVec1 = VectorCalculator.getLinePlaneIntersection(curVec[0], curVec[2], planePos, planeNormal);
						Vector3 newVec2 = VectorCalculator.getLinePlaneIntersection(curVec[1], curVec[2], planePos, planeNormal);
						leftVerticesList.Add(curVec[0]);
						leftVerticesList.Add(curVec[1]);
						leftVerticesList.Add(newVec1);
						leftVerticesList.Add(curVec[1]);
						leftVerticesList.Add(newVec2);
						leftVerticesList.Add(newVec1);
						rightVerticesList.Add(curVec[2]);
						rightVerticesList.Add(newVec1);
						rightVerticesList.Add(newVec2);
						edgeVerticesList.Add(newVec1);
						edgeVerticesList.Add(newVec2);
						break;
					}
					else if (verticesPos[j] > 0 && verticesPos[(j+1)%3] > 0) {
						Vector3 newVec1 = VectorCalculator.getLinePlaneIntersection(curVec[0], curVec[2], planePos, planeNormal);
						Vector3 newVec2 = VectorCalculator.getLinePlaneIntersection(curVec[1], curVec[2], planePos, planeNormal);
						rightVerticesList.Add(curVec[0]);
						rightVerticesList.Add(curVec[1]);
						rightVerticesList.Add(newVec1);
						rightVerticesList.Add(curVec[1]);
						rightVerticesList.Add(newVec2);
						rightVerticesList.Add(newVec1);
						leftVerticesList.Add(curVec[2]);
						leftVerticesList.Add(newVec1);
						leftVerticesList.Add(newVec2);
						edgeVerticesList.Add(newVec1);
						edgeVerticesList.Add(newVec2);
						break;
					}
				}
			}
		}

		if (edgeVerticesList.Count == 0) {
			return;
		}

		//Extract edge as successive vertices
		List<Vector3> sortedEdgeVerticesList = new List<Vector3>();
		int curEdge = 0;
		Vector3 curVect = edgeVerticesList[1];
		bool done = false;
		sortedEdgeVerticesList.Clear();

		for (int i=0;i<edgeVerticesList.Count / 2;i++) {
			for (int j=0;j<edgeVerticesList.Count / 2;j++) {
				if (curEdge != j) {
					if (edgeVerticesList[j * 2] == curVect) {
						sortedEdgeVerticesList.Add(edgeVerticesList[j * 2]);
						curVect = edgeVerticesList[j * 2 + 1];
						curEdge = j;
						break;
					}
					else if (edgeVerticesList[j * 2 + 1] == curVect) {
						sortedEdgeVerticesList.Add(edgeVerticesList[j * 2 + 1]);
						curVect = edgeVerticesList[j * 2];
						curEdge = j;
						break;
					}
				}
			}
		}

		//Simplify edge
		while (true) {
			int prevCount = sortedEdgeVerticesList.Count;
			for (int i=0;i<sortedEdgeVerticesList.Count;i++) {
				int prev = (i + sortedEdgeVerticesList.Count - 1) % sortedEdgeVerticesList.Count;
				int next = (i + 1) % sortedEdgeVerticesList.Count;
				if (VectorCalculator.crossProduct(sortedEdgeVerticesList[next] - sortedEdgeVerticesList[i], sortedEdgeVerticesList[i] - sortedEdgeVerticesList[prev]).magnitude < 0.001f) {
					sortedEdgeVerticesList.RemoveAt(i);
					break;
				}
			}
			if (sortedEdgeVerticesList.Count == prevCount) {
				break;
			}
		}

		Vector3[] sortedEdgeVertices = new Vector3[sortedEdgeVerticesList.Count];
		for (int i=0;i<sortedEdgeVerticesList.Count;i++){
			sortedEdgeVertices[i] = obj.transform.TransformPoint(sortedEdgeVerticesList[i]);
		}

		//Construct cutting plane
		cuttingPlaneVerticesList = new List<Vector3>();
		while (sortedEdgeVerticesList.Count > 3) {
			int prevCount = sortedEdgeVerticesList.Count;
			int loopCount = 0;
			int i = UnityEngine.Random.Range(0, sortedEdgeVerticesList.Count);
			while (loopCount < prevCount) {
				int prev1 = (i + sortedEdgeVerticesList.Count - 1) % sortedEdgeVerticesList.Count;
				int prev2 = (i + sortedEdgeVerticesList.Count - 2) % sortedEdgeVerticesList.Count;
				int next1 = (i + 1) % sortedEdgeVerticesList.Count;
				int next2 = (i + 2) % sortedEdgeVerticesList.Count;
				bool isIntersect = false;
				if (Vector3.Angle(
						sortedEdgeVerticesList[next1] - sortedEdgeVerticesList[i],
						sortedEdgeVerticesList[next2] - sortedEdgeVerticesList[next1]
					) <
					Vector3.Angle(
						sortedEdgeVerticesList[next1] - sortedEdgeVerticesList[i],
						sortedEdgeVerticesList[prev1] - sortedEdgeVerticesList[next1]
					)
				) {
					for (int j=0;j<sortedEdgeVerticesList.Count;j++) {
						if (j != i && j != prev1 && j != prev2 && j != next1 &&
							VectorCalculator.areLineSegmentIntersect(
								sortedEdgeVerticesList[prev1],
								sortedEdgeVerticesList[next1],
								sortedEdgeVerticesList[j],
								sortedEdgeVerticesList[(j+1)%sortedEdgeVerticesList.Count]
							)
						) {
							isIntersect = true;
							break;
						}
					}
					if (!isIntersect) {
						cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[prev1]);
						cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[i]);
						cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[next1]);
						sortedEdgeVerticesList.RemoveAt(i);
					}
				}
				if (!isIntersect) {
					break;
				}
				i++;
				loopCount++;
				if (i == sortedEdgeVerticesList.Count) {
					i = 0;
				}
			}
			if (prevCount == sortedEdgeVerticesList.Count) {
				Debug.Log(prevCount);
				break;
			}
		}
		cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[0]);
		cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[1]);
		cuttingPlaneVerticesList.Add(sortedEdgeVerticesList[2]);

		//Verify direction
		for (int i=0;i<cuttingPlaneVerticesList.Count / 3;i++) {
			Vector3 normalFace = VectorCalculator.crossProduct(
				cuttingPlaneVerticesList[i * 3 + 1] - cuttingPlaneVerticesList[i * 3 + 0],
				cuttingPlaneVerticesList[i * 3 + 2] - cuttingPlaneVerticesList[i * 3 + 1]
			);
			if (
				(normalFace.x != 0 && planeNormal.x != 0 && planeNormal.x / normalFace.x < 0) ||
				(normalFace.y != 0 && planeNormal.y != 0 && planeNormal.y / normalFace.y < 0) ||
				(normalFace.z != 0 && planeNormal.z != 0 && planeNormal.z / normalFace.z < 0)
			) {
				Vector3 tempVertex = cuttingPlaneVerticesList[i * 3 + 1];
				cuttingPlaneVerticesList[i * 3 + 1] = new Vector3(cuttingPlaneVerticesList[i * 3 + 0].x, cuttingPlaneVerticesList[i * 3 + 0].y, cuttingPlaneVerticesList[i * 3 + 0].z);
				cuttingPlaneVerticesList[i * 3 + 0] = tempVertex;
			}
		}
	}
	
	public void executeSlice(){

		Mesh leftMesh = gameObject.GetComponent<MeshFilter>().mesh;
		leftMesh.Clear();
		leftMesh.vertices = new Vector3[leftVerticesList.Count + cuttingPlaneVerticesList.Count];
		leftMesh.triangles = new int[leftVerticesList.Count + cuttingPlaneVerticesList.Count];
		Vector3[] leftVertices = leftMesh.vertices;
		int[] leftTriangles = leftMesh.triangles;
		for (int i=0;i<leftVerticesList.Count;i++) {
			leftVertices[i] = leftVerticesList[i];
			leftTriangles[i] = i;
		}

		for (int i=0;i<cuttingPlaneVerticesList.Count / 3;i++) {
			for (int j=0;j<3;j++) {
				leftVertices[i * 3 + j + leftVerticesList.Count] = cuttingPlaneVerticesList[i * 3 + j];
				leftTriangles[i * 3 + j + leftVerticesList.Count] = i * 3 + j + leftVerticesList.Count;
			}
		}

		//relocate mesh center
		float minx = 2147483647;
		float miny = 2147483647;
		float minz = 2147483647;
		float maxx = -2147483647;
		float maxy = -2147483647;
		float maxz = -2147483647;
		for (int i=0;i<leftVertices.Length;i++) {
			minx = Math.Min(minx, leftVertices[i].x);
			miny = Math.Min(miny, leftVertices[i].y);
			minz = Math.Min(minz, leftVertices[i].z);
			maxx = Math.Max(maxx, leftVertices[i].x);
			maxy = Math.Max(maxy, leftVertices[i].y);
			maxz = Math.Max(maxz, leftVertices[i].z);
		}
		Vector3 meshCenter = new Vector3((maxx + minx) / 2, (maxy + miny) / 2, (maxz + minz) / 2);
		for (int i=0;i<leftVertices.Length;i++) {
			leftVertices[i] -= meshCenter;
		}
		meshCenter = transform.TransformPoint(meshCenter);
		transform.position = meshCenter;

		//update mesh

		leftMesh.vertices = leftVertices;
		leftMesh.triangles = leftTriangles;
		leftMesh.MarkModified();
		leftMesh.RecalculateNormals();
		gameObject.GetComponent<MeshFilter>().mesh = leftMesh;
		gameObject.GetComponent<MeshCollider>().sharedMesh = leftMesh;

		obj.updateMesh(true);
		obj.updateTransform();

	}
	/* #endregion */

	/* #region Drill */

	// public void enableDrillSimulation() {

	// 	// if (smode != SelectMode.selectFace) {
	// 	// 	return;
	// 	// }
		
	// 	isOtherScreenPenetrate = true;
	// 	isEdgeAligned = false;
	// 	drillDist = 0;

	// 	obj.GetComponent<MeshRenderer>().enabled = false;
	// 	GameObject.Find("Inside").GetComponent<MeshRenderer>().enabled = false;
	// 	GameObject[] faces = GameObject.FindGameObjectsWithTag("FaceObj");
	// 	foreach (GameObject face in faces) {
	// 		face.GetComponent<MeshRenderer>().enabled = false;
	// 	}
	// 	drillObj.SetActive(true);

	// 	state = Status.drill;
	// }
	// public void exitDrillSimulation() {
	// 	isOtherScreenPenetrate = false;
	// 	state = Status.select;
	// }
	// public void disableDrillSimulation() {

	// 	obj.GetComponent<MeshRenderer>().enabled = true;
	// 	GameObject.Find("Inside").GetComponent<MeshRenderer>().enabled = true;
	// 	GameObject[] faces = GameObject.FindGameObjectsWithTag("FaceObj");
	// 	foreach (GameObject face in faces) {
	// 		face.GetComponent<MeshRenderer>().enabled = true;
	// 	}
	// 	drillObj.SetActive(false);
	// 	sender.GetComponent<ServerController>().sendMessage("Drill\nX");

	// 	state = Status.select;
	// }

	// public void updateDrillScale(float factor) {
	// 	if (smode != SelectMode.selectFace) {
	// 		return;
	// 	}
	// 	if (state == Status.drill) {
	// 		drillDist += factor;
	// 		drillDist = drillDist > 0 ? drillDist : 0;
	// 		Vector3 dir = drillObj.transform.InverseTransformPoint(new Vector3(Mathf.Cos(-VectorCalculator.angle), 0, Mathf.Sin(-VectorCalculator.angle)) * drillDist + drillObj.transform.position);
	// 		drillObj.GetComponent<DrilledObjectController>().dir = dir;
	// 	}
	// }

	/* #endregion */

	/* #region Transform */

	public void startMoving(Vector3 panDelta, bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		if (!isThisScreenFocused && !isOtherScreenFocused) {
			obj.transform.position += panDelta;
		}
		else {
			obj.transform.position += new Vector3(0, panDelta.y, 0);
			if (isThisScreenFocused && isMainScreen) {
				if (isEdgeAligned) {
					if (panDelta.x < 0) {
						obj.transform.position += new Vector3(panDelta.x, 0, 0);
						isEdgeAligned = false;
						closestVertex = -1;
						secondVertex = -1;
					}
				}
				else {
					obj.transform.position += new Vector3(panDelta.x, 0, 0);
					adjustAlign(true);
				}
			}
			if (isOtherScreenFocused && !isMainScreen) {
				if (isEdgeAligned) {
					if (panDelta.x > 0) {
						obj.transform.position += new Vector3(panDelta.x, 0, panDelta.z);
						isEdgeAligned = false;
						closestVertex = -1;
						secondVertex = -1;
					}
				}
				else {
					obj.transform.position += new Vector3(panDelta.x, 0, panDelta.z);
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
		if (obj.transform.localScale.x + pinchDelta > 0) {
			obj.transform.localScale += new Vector3(pinchDelta, pinchDelta, pinchDelta);
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
		obj.transform.rotation = rot * obj.transform.rotation;
		obj.updateTransform();
	}

	private void adjustAlign(bool isMainScreen) {
		return;
		if (isMainScreen && !isOtherScreenPenetrate) {
			int faceNum = selectEdgeVertices.Count;
			closestVertex = -1;
			secondVertex = -1;
			for (int i=0;i<faceNum;i++) {
				if (closestVertex == -1 || obj.transform.TransformPoint(hitVertices[closestVertex]).x < obj.transform.TransformPoint(hitVertices[selectEdgeVertices[i]]).x) {
					closestVertex = selectEdgeVertices[i];
				}
			}
			for (int i=0;i<faceNum;i++) {
				if ((secondVertex == -1 || obj.transform.TransformPoint(hitVertices[secondVertex]).x < obj.transform.TransformPoint(hitVertices[selectEdgeVertices[i]]).x) && selectEdgeVertices[i] != closestVertex) {
					secondVertex = selectEdgeVertices[i];
				}
			}
			Vector3 closestVector = obj.transform.TransformPoint(hitVertices[closestVertex]);
			Vector3 secondVector = obj.transform.TransformPoint(hitVertices[secondVertex]);
			if (closestVector.x > VectorCalculator.camWidth / 2 && secondVector.x > VectorCalculator.camWidth / 2) {
				if (closestVector.x != secondVector.x) {
					float k = (closestVector.y - secondVector.y) / (closestVector.x - secondVector.x);
					k = 1/k;
					float deltaAngle = Mathf.Atan(k);
					Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(0, 0, 1));
					obj.transform.rotation = rot * obj.transform.rotation;
				}
				closestVector = obj.transform.TransformPoint(hitVertices[closestVertex]);
				obj.transform.position -= new Vector3(closestVector.x - VectorCalculator.camWidth / 2, 0, 0);
				isEdgeAligned = true;
			}
			else if (closestVector.x > VectorCalculator.camWidth / 2) {
				float dist = (closestVector - new Vector3(obj.transform.position.x, obj.transform.position.y, 0)).magnitude;
				Vector3 targetVector = new Vector3(VectorCalculator.camWidth / 2, Mathf.Sqrt(dist * dist - (VectorCalculator.camWidth / 2 - obj.transform.position.x) * (VectorCalculator.camWidth / 2 - obj.transform.position.x)) * (closestVector.y > obj.transform.position.y ? 1 : -1), 0);
				Vector3 a = targetVector - new Vector3(obj.transform.position.x, obj.transform.position.y, 0);
				Vector3 b = closestVector - new Vector3(obj.transform.position.x, obj.transform.position.y, 0);
				float deltaAngle = Mathf.Acos(VectorCalculator.dotProduct(a, b) / a.magnitude / b.magnitude) * (closestVector.y > obj.transform.position.y ? 1 : -1);
				Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(0, 0, 1));
				obj.transform.rotation = rot * obj.transform.rotation;
			}
		}
		else if (!isMainScreen && !isThisScreenCuttingPlane) {
			int faceNum = selectEdgeVertices.Count;
			closestVertex = -1;
			secondVertex = -1;
			for (int i=0;i<faceNum;i++) {
				if (closestVertex == -1 || VectorCalculator.convertFromServer(obj.transform.TransformPoint(hitVertices[closestVertex])).x > VectorCalculator.convertFromServer(obj.transform.TransformPoint(hitVertices[selectEdgeVertices[i]])).x) {
					closestVertex = selectEdgeVertices[i];
				}
			}
			for (int i=0;i<faceNum;i++) {
				if ((secondVertex == -1 || VectorCalculator.convertFromServer(obj.transform.TransformPoint(hitVertices[secondVertex])).x > VectorCalculator.convertFromServer(obj.transform.TransformPoint(hitVertices[selectEdgeVertices[i]])).x) && selectEdgeVertices[i] != closestVertex) {
					secondVertex = selectEdgeVertices[i];
				}
			}
			Vector3 closestVector = VectorCalculator.convertFromServer(obj.transform.TransformPoint(hitVertices[closestVertex]));
			Vector3 secondVector = VectorCalculator.convertFromServer(obj.transform.TransformPoint(hitVertices[secondVertex]));
			if (closestVector.x < - VectorCalculator.camWidth / 2 && secondVector.x < - VectorCalculator.camWidth / 2) {
				if (closestVector.x != secondVector.x) {
					float k = (closestVector.y - secondVector.y) / (closestVector.x - secondVector.x);
					k = 1/k;
					float deltaAngle = Mathf.Atan(k);
					Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(-Mathf.Sin(-VectorCalculator.angle), 0, Mathf.Cos(-VectorCalculator.angle)));
					obj.transform.rotation = rot * obj.transform.rotation;
				}
				closestVector = VectorCalculator.convertFromServer(obj.transform.TransformPoint(hitVertices[closestVertex]));
				obj.transform.position += new Vector3(Mathf.Cos(-VectorCalculator.angle) * (- closestVector.x - VectorCalculator.camWidth / 2), 0, Mathf.Sin(-VectorCalculator.angle) * (- closestVector.x - VectorCalculator.camWidth / 2));
				isEdgeAligned = true;
			}
			else if (closestVector.x < - VectorCalculator.camWidth / 2) {
				float dist = (closestVector - new Vector3(VectorCalculator.convertFromServer(obj.transform.position).x, VectorCalculator.convertFromServer(obj.transform.position).y, 0)).magnitude;
				Vector3 targetVector = new Vector3(- VectorCalculator.camWidth / 2, Mathf.Sqrt(dist * dist - (VectorCalculator.camWidth / 2 + VectorCalculator.convertFromServer(obj.transform.position).x) * (VectorCalculator.camWidth / 2 + VectorCalculator.convertFromServer(obj.transform.position).x)) * (closestVector.y > obj.transform.position.y ? 1 : -1), 0);
				Vector3 a = targetVector - new Vector3(VectorCalculator.convertFromServer(obj.transform.position).x, VectorCalculator.convertFromServer(obj.transform.position).y, 0);
				Vector3 b = closestVector - new Vector3(VectorCalculator.convertFromServer(obj.transform.position).x, VectorCalculator.convertFromServer(obj.transform.position).y, 0);
				float deltaAngle = Mathf.Acos(VectorCalculator.dotProduct(a, b) / a.magnitude / b.magnitude) * (closestVector.y > obj.transform.position.y ? 1 : -1);
				Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(-Mathf.Sin(-VectorCalculator.angle), 0, Mathf.Cos(-VectorCalculator.angle)));
				obj.transform.rotation = rot * obj.transform.rotation;
			}
		}
	}

	private void focus() {
		float deltaAngle = angleToFocus - Mathf.Lerp(angleToFocus, 0, focusSpeed * Time.deltaTime);
		angleToFocus -= deltaAngle;
		obj.transform.rotation = Quaternion.AngleAxis(deltaAngle, axisToFocus) * obj.transform.rotation;
		if (Mathf.Abs(angleToFocus) < 0.01f) {
			angleToFocus = 0;
			obj.transform.position = posToFocus;
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
		obj.transform.position = Vector3.Lerp(obj.transform.position, posToFocus, focusSpeed * Time.deltaTime);
		obj.updateTransform();
	}

	private void startFocus(bool isThisScreen, bool isNewFocus) {

		if (isNewFocus) {
			for (int i=0;i<selectEdgeVertices.Count;i++) {
				prevEdgeVertices.Add(selectEdgeVertices[i]);
			}
			obj.updateHighlight(focusTriangleIndex, -2);
			focusNormal = hit.normal;
		}

		if (state == Status.select && (smode == SelectMode.selectFace || !isNewFocus)) {
			axisToFocus = VectorCalculator.crossProduct(focusNormal, (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(Mathf.Sin(-VectorCalculator.angle), 0, -Mathf.Cos(-VectorCalculator.angle))));
			angleToFocus = Vector3.Angle(focusNormal, (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(Mathf.Sin(-VectorCalculator.angle), 0, -Mathf.Cos(-VectorCalculator.angle))));
			centerToHitFace =
				obj.transform.InverseTransformPoint(
						focusNormal + obj.transform.position
					).normalized *
				VectorCalculator.dotProduct(
					obj.transform.InverseTransformPoint(
						focusNormal + obj.transform.position
					).normalized,
					hitVertices[hitTriangles[focusTriangleIndex * 3 + 0]]
				);

			float depth = (obj.transform.TransformPoint(centerToHitFace) - obj.transform.position).magnitude;
			depth += 0.025f;
			float offset = VectorCalculator.camWidth / 2;
			if (!isNewFocus) {
				if (isThisScreen) {
					offset = VectorCalculator.camWidth / 2 - obj.transform.position.x;
				}
				else {
					offset = VectorCalculator.camWidth / 2 + VectorCalculator.convertFromServer(obj.transform.position).x;
				}
			}

			if (isThisScreen) {
				posToFocus = new Vector3(VectorCalculator.camWidth / 2 - offset, isNewFocus ? 0 : obj.transform.position.y, depth);
				focusingThisScreen = true;
			}
			else {
				posToFocus = new Vector3(VectorCalculator.camWidth / 2 + Mathf.Cos(-VectorCalculator.angle) * offset - Mathf.Sin(-VectorCalculator.angle) * depth, isNewFocus ? 0 : obj.transform.position.y, Mathf.Sin(-VectorCalculator.angle) * offset + Mathf.Cos(-VectorCalculator.angle) * depth);
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
		obj.realMeasure = obj.transform.localScale;
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

		obj.transform.position = new Vector3(0, 0, 2f);
		obj.transform.localScale = new Vector3(2, 2, 2);
		obj.transform.rotation = Quaternion.Euler(30, 60, 45);

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
	/* #endregion */
}