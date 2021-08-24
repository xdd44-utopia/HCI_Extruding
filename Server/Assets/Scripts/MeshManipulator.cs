using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class MeshManipulator : MonoBehaviour
{
	public Camera cam;
	private float camWidth;
	private float camHeight;
	public Text modeText;
	public Text focusText;
	public Text debugText;
	public Text debugText2;
	public Image selectButton;
	public Sprite selectFaceSprite;
	public Sprite selectObjectSprite;
	public Image measureButton;
	public Sprite measureRecoverSprite;
	public Sprite measureRecoveredSprite;
	public GameObject sliceTraceVisualizer;
	public GameObject sliderController;
	public GameObject sender;
	public GameObject extrudeHandle;
	public GameObject gridController;
	//interaction
	[HideInInspector]
	public Vector3 touchPosition;
	private Vector3 prevTouchPosition;
	private float touchDelayTolerance = 0.1f;
	private Vector3 INF = new Vector3(10000, 10000, 10000);
	private float angle = 0;
	private float prevAngle = 0;

	//hit
	public GameObject hitObj;
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
	private bool isOtherScreenCuttingPlane = false;
	private Vector3 footDrop;


	//selection
	[HideInInspector]
	public List<int> selectEdgeVertices;
	private List<int> prevEdgeVertices;
	[HideInInspector]
	public List<int> selectTriangles;
	private int selectTriangleIndex;
	[HideInInspector]
	public int focusTriangleIndex;
	[HideInInspector]
	public Vector3 focusNormal;
	
	//focus
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
	private int[] extrudedTriangles;
	private float extrudeTimer = 0;

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
		taper
	}

	private SelectMode smode = SelectMode.selectObject;
	private enum SelectMode {
		selectFace,
		selectObject
	}

	/* #region Main */
	void Start()
	{

		touchPosition = INF;
		selectTriangleIndex = -1;
		focusTriangleIndex = -1;

		Camera cam = Camera.main;
		camHeight = 2f * cam.orthographicSize;
		camWidth = camHeight * cam.aspect;

		cuttingPlaneRenderer.enabled = false;
	
	}

	void Update() {

		selectButton.sprite = (smode == SelectMode.selectObject ? selectObjectSprite : selectFaceSprite);
		measureButton.sprite = (hitObj.GetComponent<ObjectController>().isRealMeasure ? measureRecoveredSprite : measureRecoverSprite);
		if (isEdgeAligned) {
			if (isThisScreenFocused) {
				string msg = "Extrude\n" + extrudeDist;
				sender.GetComponent<ServerController>().sendMessage(msg);
			}
			else {
				extrudeHandle.SetActive(isOtherScreenFocused);
				extrudeHandle.GetComponent<RectTransform>().anchoredPosition = new Vector2(360 - extrudeDist / camWidth * 772, 0);
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

		angle = sliderController.GetComponent<SliderController>().angle;
		if (state == Status.select) {
			updateMesh();
		}

		if (Mathf.Abs(angle - prevAngle) > 0.005f) {
			hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
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
				extrude();
				break;
			case Status.taper:
				taper();
				break;
		}

		prevAngle = angle;

		// if (Input.GetMouseButtonDown(0)) {
		// 	castRay();
		// }

	}
	/* #endregion */

	/* #region Construct highlight */
	public void castRay() {

		Vector3 mousePos = touchPosition;
		// mousePos = Input.mousePosition;
		// mousePos -= new Vector3(360, 772, 0);
		// mousePos *= Camera.main.orthographicSize / 772;

		Vector3 rayStart;
		Vector3 rayDirection;

		if (!cam.orthographic) {
			rayStart = cam.gameObject.transform.position;
			rayDirection = mousePos - rayStart;
		}
		else {
			rayStart = mousePos;
			if (Mathf.Abs(mousePos.z) < 0.01f) {
				rayDirection = new Vector3(0, 0, 5);
			}
			else {
				rayDirection = new Vector3(- 5 * Mathf.Cos(Mathf.PI / 2 + angle), 0, 5 * Mathf.Sin(Mathf.PI / 2 + angle));
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
			
			prevEdgeVertices = new List<int>();
			selectTriangleIndex = hitObj.GetComponent<ObjectController>().selectFace(hit.triangleIndex);
			smode = (selectTriangleIndex == -1 ? SelectMode.selectObject : SelectMode.selectFace);

		}
		
	}

	private void checkAngleFit() {
		int targetTriangle = -1;
		if (isEdgeAligned) {
			if (isThisScreenFocused) {
				for (int i=0;i<hitTriangles.Length / 3;i++) {
					int cnt = 0;
					for (int j=0;j<3;j++) {
						if (Mathf.Abs(convertFromServer(hitObj.transform.TransformPoint(hitVertices[hitTriangles[i * 3 + j]])).z) < 0.05f) {
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
						if (Mathf.Abs(hitObj.transform.TransformPoint(hitVertices[hitTriangles[i * 3 + j]]).z) < 0.05f) {
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
		hitObj.GetComponent<ObjectController>().updateAlighFace(targetTriangle);
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
		undoPos = hitObj.transform.position;
	}

	public void loadUndo() {
		if (!undoAvailable) {
			return;
		}
		else {
			undoAvailable = false;
		}
		hitVertices = hitObj.GetComponent<MeshFilter>().mesh.vertices;
		hitTriangles = hitObj.GetComponent<MeshFilter>().mesh.triangles;
		hitVertices = new Vector3[undoVertices.Length];
		hitTriangles = new int[undoTriangles.Length];
		for (int i=0;i<hitVertices.Length;i++) {
			hitVertices[i] = undoVertices[i];
		}
		for (int i=0;i<hitTriangles.Length;i++) {
			hitTriangles[i] = undoTriangles[i];
		}

		Mesh tempMesh = hitObj.GetComponent<MeshFilter>().mesh;

		tempMesh.triangles = hitTriangles;
		tempMesh.vertices = hitVertices;
		tempMesh.MarkModified();
		tempMesh.RecalculateNormals();
		hitObj.GetComponent<MeshFilter>().mesh = tempMesh;
		hitObj.GetComponent<MeshCollider>().sharedMesh = tempMesh;
		hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
		hitObj.GetComponent<ObjectController>().isMeshUpdated = true;
		hitObj.transform.position = undoPos;
	}

	/* #endregion */

	/* #region Extrude */
	private void extrude() {

		if (smode != SelectMode.selectFace) {
			state = Status.select;
			return;
		}


		extrudedVertices = extrudedMesh.vertices;
		extrudedTriangles = extrudedMesh.triangles;
		int faceNum = selectTriangles.Count;
		int edgeLength = selectEdgeVertices.Count;
		Vector3 localNormal = hitObj.transform.InverseTransformPoint(hitObj.transform.position - extrudeDir);

		localNormal = localNormal.normalized;
		for (int i=0;i<edgeLength;i++) {
			extrudedVertices[hitVerticesNum + i * 6 + 2] = hitVertices[selectEdgeVertices[(i+1)%edgeLength]] + localNormal * extrudeDist;
			extrudedVertices[hitVerticesNum + i * 6 + 4] = hitVertices[selectEdgeVertices[(i+1)%edgeLength]] + localNormal * extrudeDist;
			extrudedVertices[hitVerticesNum + i * 6 + 5] = hitVertices[selectEdgeVertices[i]] + localNormal * extrudeDist;
		}

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				extrudedVertices[hitTriangles[selectTriangles[i] * 3 + j]] = hitVertices[hitTriangles[selectTriangles[i] * 3 + j]] + localNormal * extrudeDist;
			}
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTriangles;
		extrudedMesh.MarkModified();
		extrudedMesh.RecalculateNormals();

		extrudeDir = extrudeDir.normalized * hitObj.transform.localScale.x;

		hitObj.transform.position = extrudeStartPos + extrudeDir * extrudeDist;

		hitObj.GetComponent<MeshFilter>().mesh = extrudedMesh;
		hitObj.GetComponent<MeshCollider>().sharedMesh = extrudedMesh;
		hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
		hitObj.GetComponent<ObjectController>().isMeshUpdated = true;

		extrudeTimer -= Time.deltaTime;
		if (extrudeTimer < 0) {
			state = Status.select;
			extrudeDist = 0;
		}

	}
	
	private void prepareExtrude(bool isThisScreen) {

		prepareUndo();

		int faceNum = selectTriangles.Count;
		int edgeLength = selectEdgeVertices.Count;
		
		extrudeDist = 0;
		extrudeStartPos = hitObj.transform.position;
		if (isThisScreen) {
			extrudeDir = new Vector3(-1, 0, 0);
		}
		else {
			extrudeDir = new Vector3(Mathf.Cos(-angle), 0, Mathf.Sin(-angle));
		}

		extrudedMesh = hitObj.GetComponent<MeshFilter>().mesh;
		extrudedMesh.vertices = new Vector3[hitVerticesNum + edgeLength * 6];
		extrudedMesh.triangles = new int[hitTrianglesNum + edgeLength * 6];
		
		extrudedVertices = extrudedMesh.vertices;
		extrudedTriangles = extrudedMesh.triangles;
		for (int i=0;i<hitVerticesNum;i++) {
			extrudedVertices[i] = hitVertices[i];
		}
		for (int i=0;i<hitTrianglesNum;i++) {
			extrudedTriangles[i] = hitTriangles[i];
		}
		for (int i=0;i<edgeLength;i++) {
			extrudedVertices[hitVerticesNum + i * 6 + 0] = hitVertices[selectEdgeVertices[i]];
			extrudedVertices[hitVerticesNum + i * 6 + 1] = hitVertices[selectEdgeVertices[(i+1)%edgeLength]];
			extrudedVertices[hitVerticesNum + i * 6 + 2] = hitVertices[selectEdgeVertices[(i+1)%edgeLength]];
			extrudedVertices[hitVerticesNum + i * 6 + 3] = hitVertices[selectEdgeVertices[i]];
			extrudedVertices[hitVerticesNum + i * 6 + 4] = hitVertices[selectEdgeVertices[(i+1)%edgeLength]];
			extrudedVertices[hitVerticesNum + i * 6 + 5] = hitVertices[selectEdgeVertices[i]];
		}
		for (int i=0;i<edgeLength;i++) {
			for (int j=0;j<6;j++) {
				extrudedTriangles[hitTrianglesNum + i * 6 + j] = hitVerticesNum + i * 6 + j;
			}
		}

		extrudedMesh.vertices = extrudedVertices;
		extrudedMesh.triangles = extrudedTriangles;

		state = Status.extrude;
		extrudeTimer = touchDelayTolerance;

	}
	/* #endregion */

	/* #region Taper */

	public void updateTaperScale(float factor) {

		if (state != Status.taper) {
			return;
		}
		
		taperScale += factor / 2.5f;
		taperTimer = touchDelayTolerance;
		taperStarted = true;
	}

	public void prepareTaper() {

		if (selectTriangleIndex != focusTriangleIndex) {
			return;
		}
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

		taperedMesh = hitObj.GetComponent<MeshFilter>().mesh;

		for (int i=0;i<faceNum;i++) {
			for (int j=0;j<3;j++) {
				taperCenter += hitVertices[hitTriangles[selectTriangles[i] * 3 + j]];
			}
		}
		taperCenter /= faceNum * 3;

		state = Status.taper;

		taperTimer = touchDelayTolerance;
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

		hitObj.GetComponent<MeshFilter>().mesh = taperedMesh;
		hitObj.GetComponent<MeshCollider>().sharedMesh = taperedMesh;
		hitObj.GetComponent<ObjectController>().isMeshUpdated = true;

		// taperScale = 1;

		taperTimer -= Time.deltaTime;
		if (taperTimer < 0 && taperStarted) {
			state = Status.select;
		}

	}
	/* #endregion */

	/* #region Split */
	public void enableCuttingPlaneOtherScreen() {
		isOtherScreenCuttingPlane = true;
		isEdgeAligned = false;
	}
	public void executeCuttingPlaneOtherScreen() {
		startSlice(true, false);
		executeSlice();
		isOtherScreenCuttingPlane = false;
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

		// x ⋅ planePos - dotProduct(planePos, planeNormal) = 0
		// <= 0 left, > 0 right
		planeNormalWorld = new Vector3(planeNormal.x, planeNormal.y, planeNormal.z);
		planePos = hitObj.transform.InverseTransformPoint(planePos);
		planeNormal = hitObj.transform.InverseTransformPoint(planeNormal + hitObj.transform.position);
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
		float pn = dotProduct(planePos, planeNormal);
		for (int i=0;i<hitTrianglesNum / 3;i++) {
			float[] verticesPos = new float[3]{
				dotProduct(hitVertices[hitTriangles[i * 3 + 0]], planeNormal) - pn,
				dotProduct(hitVertices[hitTriangles[i * 3 + 1]], planeNormal) - pn,
				dotProduct(hitVertices[hitTriangles[i * 3 + 2]], planeNormal) - pn
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
						Vector3 newVec1 = intersectLinePlane(curVec[0], curVec[2], planePos, planeNormal);
						Vector3 newVec2 = intersectLinePlane(curVec[1], curVec[2], planePos, planeNormal);
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
						Vector3 newVec1 = intersectLinePlane(curVec[0], curVec[2], planePos, planeNormal);
						Vector3 newVec2 = intersectLinePlane(curVec[1], curVec[2], planePos, planeNormal);
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
		bool done = false;
		sortedEdgeVerticesList.Add(edgeVerticesList[0]);
		sortedEdgeVerticesList.Add(edgeVerticesList[1]);
		while (!done) {
			for (int i=0;i<edgeVerticesList.Count / 2;i++) {
				for (int j=0;j<2;j++) {
					if (i != curEdge && edgeVerticesList[i * 2 + j] == sortedEdgeVerticesList[sortedEdgeVerticesList.Count - 1]) {
						if (edgeVerticesList[i * 2 + (1 - j)] == sortedEdgeVerticesList[0]) {
							done = true;
						}
						else {
							sortedEdgeVerticesList.Add(edgeVerticesList[i * 2 + (1 - j)]);
							curEdge = i;
						}
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
				if (crossProduct(sortedEdgeVerticesList[next] - sortedEdgeVerticesList[i], sortedEdgeVerticesList[i] - sortedEdgeVerticesList[prev]).magnitude < 0.001f) {
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
			sortedEdgeVertices[i] = hitObj.transform.TransformPoint(sortedEdgeVerticesList[i]);
		}
		if (!isScreenCut) {
			sliceTraceVisualizer.GetComponent<SliceTraceVisualizer>().updateCuttingPlane(sortedEdgeVertices);
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
							areLinesIntersect(
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
			Vector3 normalFace = crossProduct(
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

		Vector3[] cuttingPlaneVertices = new Vector3[cuttingPlaneVerticesList.Count];
		for (int i=0;i<cuttingPlaneVerticesList.Count;i++){
			cuttingPlaneVertices[i] = hitObj.transform.TransformPoint(cuttingPlaneVerticesList[i]);
		}
	}
	
	public void executeSlice(){

		if (edgeVerticesList.Count == 0) {
			sliceTraceVisualizer.GetComponent<SliceTraceVisualizer>().endVisualize();
			return;
		}

		bool isTrim = true;

		GameObject leftObj = hitObj;
		Mesh leftMesh = leftObj.GetComponent<MeshFilter>().mesh;
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
		Vector3 meshCenter = new Vector3(0, 0, 0);
		for (int i=0;i<leftVertices.Length;i++) {
			meshCenter += leftVertices[i];
		}
		meshCenter /= leftVertices.Length;
		for (int i=0;i<leftVertices.Length;i++) {
			leftVertices[i] -= meshCenter;
		}
		Debug.Log(meshCenter.x + " " + meshCenter.y + " " + meshCenter.z);
		meshCenter = leftObj.transform.TransformPoint(meshCenter);
		Debug.Log(meshCenter.x + " " + meshCenter.y + " " + meshCenter.z);
		leftObj.transform.position = meshCenter;

		//update mesh

		leftMesh.vertices = leftVertices;
		leftMesh.triangles = leftTriangles;
		leftMesh.MarkModified();
		leftMesh.RecalculateNormals();
		leftObj.GetComponent<MeshFilter>().mesh = leftMesh;
		leftObj.GetComponent<MeshCollider>().sharedMesh = leftMesh;

		leftObj.transform.position = leftObj.transform.position - (isTrim ? Vector3.zero : planeNormalWorld.normalized * 0.25f);
		leftObj.GetComponent<ObjectController>().isTransformUpdated = true;
		leftObj.GetComponent<ObjectController>().isMeshUpdated = true;

		if (!isTrim) {
			GameObject rightObj = Instantiate(leftObj, leftObj.transform);
			rightObj.transform.parent = null;
			rightObj.transform.position = leftObj.transform.position;
			rightObj.transform.rotation = leftObj.transform.rotation;
			rightObj.transform.localScale = leftObj.transform.localScale;
			Mesh rightMesh = rightObj.GetComponent<MeshFilter>().mesh;
			rightMesh.Clear();
			rightMesh.vertices = new Vector3[rightVerticesList.Count + cuttingPlaneVerticesList.Count];
			rightMesh.triangles = new int[rightVerticesList.Count + cuttingPlaneVerticesList.Count];
			Vector3[] rightVertices = rightMesh.vertices;
			int[] rightTriangles = rightMesh.triangles;
			for (int i=0;i<rightVerticesList.Count;i++) {
				rightVertices[i] = rightVerticesList[i];
				rightTriangles[i] = i;
			}
			for (int i=0;i<cuttingPlaneVerticesList.Count / 3;i++) {
				for (int j=0;j<3;j++) {
					rightVertices[i * 3 + j + rightVerticesList.Count] = cuttingPlaneVerticesList[i * 3 + j];
					rightTriangles[i * 3 + j + rightVerticesList.Count] = i * 3 + (2 - j) + rightVerticesList.Count;
				}
			}
			rightMesh.vertices = rightVertices;
			rightMesh.triangles = rightTriangles;
			rightMesh.MarkModified();
			rightMesh.RecalculateNormals();
			rightObj.GetComponent<MeshFilter>().mesh = rightMesh;
			rightObj.GetComponent<MeshCollider>().sharedMesh = rightMesh;
			rightObj.transform.position = rightObj.transform.position + planeNormalWorld.normalized * 0.25f;
			rightObj.GetComponent<ObjectController>().isTransformUpdated = true;
			rightObj.GetComponent<ObjectController>().isMeshUpdated = true;
		}

		sliceTraceVisualizer.GetComponent<SliceTraceVisualizer>().endVisualize();
		cancel();
	}
	/* #endregion */

	/* #region Calculator */
	private Vector3 crossProduct(Vector3 a, Vector3 b) {
		return new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
	}

	private float dotProduct(Vector3 a, Vector3 b) {
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	private float multXZ(Vector3 from, Vector3 to) {
		return from.x * to.x + from.z * to.z;
	}

	private Vector3 convertFromServer(Vector3 v) {
		Vector3 origin = new Vector3(camWidth / 2 + camWidth * Mathf.Cos(angle) / 2, 0, - camWidth * Mathf.Sin(angle) / 2);
		Vector3 x = new Vector3(Mathf.Cos(angle), 0, - Mathf.Sin(angle));
		Vector3 z = new Vector3(Mathf.Cos(Mathf.PI / 2 - angle), 0, Mathf.Sin(Mathf.PI / 2 - angle));
		v -= origin;
		return new Vector3(multXZ(v, x), v.y, multXZ(v, z));
	}

	private Vector3 intersectLinePlane(Vector3 a, Vector3 b, Vector3 p, Vector3 n) { //line passes a and b, plane passes p with normal n
		float t = (dotProduct(p, n) - dotProduct(a, n)) / dotProduct(n, a - b);
		return a + t * (a - b);
	}

	private bool areLinesIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2) {
		bool result = true;
		if (crossProduct(a2 - a1, b2 - a2).magnitude > 0 &&
			crossProduct(a2 - a1, b1 - a2).magnitude > 0 &&
			Vector3.Angle(crossProduct(a2 - a1, b2 - a2), crossProduct(a2 - a1, b1 - a2)) < 0.001f
		) {
			result = false;
		}
		if (crossProduct(b2 - b1, a1 - b2).magnitude > 0 &&
			crossProduct(b2 - b1, a2 - b2).magnitude > 0 &&
			Vector3.Angle(crossProduct(b2 - b1, a1 - b2), crossProduct(b2 - b1, a2 - b2)) < 0.001f
		) {
			result = false;
		}
		return result;
	}
	/* #endregion */

	/* #region Public */

	public void switchSelectMode() {
		cancel();
		if (smode == SelectMode.selectFace) {
			smode = SelectMode.selectObject;
		}
		else {
			smode = SelectMode.selectFace;
		}
	}

	public void updateExtrudeScale(float factor, bool isThisScreen) {
		if (smode != SelectMode.selectFace) {
			return;
		}
		if (selectTriangleIndex != focusTriangleIndex || !isEdgeAligned) {
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

	/* #region Transform */

	public void startMoving(Vector3 panDelta, bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		if (!isThisScreenFocused && !isOtherScreenFocused) {
			hitObj.transform.position += panDelta;
		}
		else {
			hitObj.transform.position += new Vector3(0, panDelta.y, 0);
			if (isThisScreenFocused && isMainScreen) {
				if (isEdgeAligned) {
					if (panDelta.x < 0) {
						hitObj.transform.position += new Vector3(panDelta.x, 0, 0);
						isEdgeAligned = false;
						closestVertex = -1;
						secondVertex = -1;
					}
				}
				else {
					hitObj.transform.position += new Vector3(panDelta.x, 0, 0);
					adjustAlign(true);
				}
			}
			if (isOtherScreenFocused && !isMainScreen) {
				if (isEdgeAligned) {
					if (panDelta.x > 0) {
						hitObj.transform.position += new Vector3(panDelta.x, 0, panDelta.z);
						isEdgeAligned = false;
						closestVertex = -1;
						secondVertex = -1;
					}
				}
				else {
					hitObj.transform.position += new Vector3(panDelta.x, 0, panDelta.z);
					adjustAlign(false);
				}
			}
		}
		hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
	}

	public void startScaling(float pinchDelta, bool isMainScreen) {
		if (state != Status.select) {
			return;
		}
		if (hitObj.transform.localScale.x + pinchDelta > 0) {
			hitObj.transform.localScale += new Vector3(pinchDelta, pinchDelta, pinchDelta);
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
		Quaternion rot = Quaternion.AngleAxis(turnDelta, (isMainScreen ? new Vector3(0, 0, 1) : new Vector3(-Mathf.Sin(-angle), 0, Mathf.Cos(-angle))));
		hitObj.transform.rotation = rot * hitObj.transform.rotation;
		hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
	}

	private void adjustAlign(bool isMainScreen) {
		if (isMainScreen && !isOtherScreenCuttingPlane) {
			int faceNum = selectEdgeVertices.Count;
			closestVertex = -1;
			secondVertex = -1;
			for (int i=0;i<faceNum;i++) {
				if (closestVertex == -1 || hitObj.transform.TransformPoint(hitVertices[closestVertex]).x < hitObj.transform.TransformPoint(hitVertices[selectEdgeVertices[i]]).x) {
					closestVertex = selectEdgeVertices[i];
				}
			}
			for (int i=0;i<faceNum;i++) {
				if ((secondVertex == -1 || hitObj.transform.TransformPoint(hitVertices[secondVertex]).x < hitObj.transform.TransformPoint(hitVertices[selectEdgeVertices[i]]).x) && selectEdgeVertices[i] != closestVertex) {
					secondVertex = selectEdgeVertices[i];
				}
			}
			Vector3 closestVector = hitObj.transform.TransformPoint(hitVertices[closestVertex]);
			Vector3 secondVector = hitObj.transform.TransformPoint(hitVertices[secondVertex]);
			if (closestVector.x > camWidth / 2 && secondVector.x > camWidth / 2) {
				if (closestVector.x != secondVector.x) {
					float k = (closestVector.y - secondVector.y) / (closestVector.x - secondVector.x);
					k = 1/k;
					float deltaAngle = Mathf.Atan(k);
					Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(0, 0, 1));
					hitObj.transform.rotation = rot * hitObj.transform.rotation;
				}
				closestVector = hitObj.transform.TransformPoint(hitVertices[closestVertex]);
				hitObj.transform.position -= new Vector3(closestVector.x - camWidth / 2, 0, 0);
				isEdgeAligned = true;
			}
			else if (closestVector.x > camWidth / 2) {
				float dist = (closestVector - new Vector3(hitObj.transform.position.x, hitObj.transform.position.y, 0)).magnitude;
				Vector3 targetVector = new Vector3(camWidth / 2, Mathf.Sqrt(dist * dist - (camWidth / 2 - hitObj.transform.position.x) * (camWidth / 2 - hitObj.transform.position.x)) * (closestVector.y > hitObj.transform.position.y ? 1 : -1), 0);
				Vector3 a = targetVector - new Vector3(hitObj.transform.position.x, hitObj.transform.position.y, 0);
				Vector3 b = closestVector - new Vector3(hitObj.transform.position.x, hitObj.transform.position.y, 0);
				float deltaAngle = Mathf.Acos(dotProduct(a, b) / a.magnitude / b.magnitude) * (closestVector.y > hitObj.transform.position.y ? 1 : -1);
				Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(0, 0, 1));
				hitObj.transform.rotation = rot * hitObj.transform.rotation;
			}
		}
		else if (!isMainScreen && !isThisScreenCuttingPlane) {
			int faceNum = selectEdgeVertices.Count;
			closestVertex = -1;
			secondVertex = -1;
			for (int i=0;i<faceNum;i++) {
				if (closestVertex == -1 || convertFromServer(hitObj.transform.TransformPoint(hitVertices[closestVertex])).x > convertFromServer(hitObj.transform.TransformPoint(hitVertices[selectEdgeVertices[i]])).x) {
					closestVertex = selectEdgeVertices[i];
				}
			}
			for (int i=0;i<faceNum;i++) {
				if ((secondVertex == -1 || convertFromServer(hitObj.transform.TransformPoint(hitVertices[secondVertex])).x > convertFromServer(hitObj.transform.TransformPoint(hitVertices[selectEdgeVertices[i]])).x) && selectEdgeVertices[i] != closestVertex) {
					secondVertex = selectEdgeVertices[i];
				}
			}
			Vector3 closestVector = convertFromServer(hitObj.transform.TransformPoint(hitVertices[closestVertex]));
			Vector3 secondVector = convertFromServer(hitObj.transform.TransformPoint(hitVertices[secondVertex]));
			if (closestVector.x < - camWidth / 2 && secondVector.x < - camWidth / 2) {
				if (closestVector.x != secondVector.x) {
					float k = (closestVector.y - secondVector.y) / (closestVector.x - secondVector.x);
					k = 1/k;
					float deltaAngle = Mathf.Atan(k);
					Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(-Mathf.Sin(-angle), 0, Mathf.Cos(-angle)));
					hitObj.transform.rotation = rot * hitObj.transform.rotation;
				}
				closestVector = convertFromServer(hitObj.transform.TransformPoint(hitVertices[closestVertex]));
				hitObj.transform.position += new Vector3(Mathf.Cos(-angle) * (- closestVector.x - camWidth / 2), 0, Mathf.Sin(-angle) * (- closestVector.x - camWidth / 2));
				isEdgeAligned = true;
			}
			else if (closestVector.x < - camWidth / 2) {
				float dist = (closestVector - new Vector3(convertFromServer(hitObj.transform.position).x, convertFromServer(hitObj.transform.position).y, 0)).magnitude;
				Vector3 targetVector = new Vector3(- camWidth / 2, Mathf.Sqrt(dist * dist - (camWidth / 2 + convertFromServer(hitObj.transform.position).x) * (camWidth / 2 + convertFromServer(hitObj.transform.position).x)) * (closestVector.y > hitObj.transform.position.y ? 1 : -1), 0);
				Vector3 a = targetVector - new Vector3(convertFromServer(hitObj.transform.position).x, convertFromServer(hitObj.transform.position).y, 0);
				Vector3 b = closestVector - new Vector3(convertFromServer(hitObj.transform.position).x, convertFromServer(hitObj.transform.position).y, 0);
				float deltaAngle = Mathf.Acos(dotProduct(a, b) / a.magnitude / b.magnitude) * (closestVector.y > hitObj.transform.position.y ? 1 : -1);
				Quaternion rot = Quaternion.AngleAxis(deltaAngle / Mathf.PI * 180, new Vector3(-Mathf.Sin(-angle), 0, Mathf.Cos(-angle)));
				hitObj.transform.rotation = rot * hitObj.transform.rotation;
			}
		}
	}

	private void focus() {
		float deltaAngle = angleToFocus - Mathf.Lerp(angleToFocus, 0, focusSpeed * Time.deltaTime);
		angleToFocus -= deltaAngle;
		hitObj.transform.rotation = Quaternion.AngleAxis(deltaAngle, axisToFocus) * hitObj.transform.rotation;
		if (Mathf.Abs(angleToFocus) < 0.01f) {
			angleToFocus = 0;
			hitObj.transform.position = posToFocus;
			hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
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
			cam.orthographic = true;
			gridController.GetComponent<GridController>().isFixed = true;
		}
		hitObj.transform.position = Vector3.Lerp(hitObj.transform.position, posToFocus, focusSpeed * Time.deltaTime);
		hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
	}

	private void startFocus(bool isThisScreen, bool isNewFocus) {

		if (isNewFocus) {
			for (int i=0;i<selectEdgeVertices.Count;i++) {
				prevEdgeVertices.Add(selectEdgeVertices[i]);
			}
			hitObj.GetComponent<ObjectController>().newFocus();
			focusNormal = hit.normal;
		}

		if (state == Status.select && (smode == SelectMode.selectFace || !isNewFocus)) {
			axisToFocus = crossProduct(focusNormal, (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(Mathf.Sin(-angle), 0, -Mathf.Cos(-angle))));
			angleToFocus = Vector3.Angle(focusNormal, (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(Mathf.Sin(-angle), 0, -Mathf.Cos(-angle))));
			centerToHitFace =
				hitObj.transform.InverseTransformPoint(
						focusNormal + hitObj.transform.position
					).normalized *
				dotProduct(
					hitObj.transform.InverseTransformPoint(
						focusNormal + hitObj.transform.position
					).normalized,
					hitVertices[hitTriangles[focusTriangleIndex * 3 + 0]]
				);

			float depth = (hitObj.transform.TransformPoint(centerToHitFace) - hitObj.transform.position).magnitude * 1.025f;
			float offset = camWidth / 2;
			if (!isNewFocus) {
				if (isThisScreen) {
					offset = camWidth / 2 - hitObj.transform.position.x;
				}
				else {
					offset = camWidth / 2 + convertFromServer(hitObj.transform.position).x;
				}
			}

			if (isThisScreen) {
				posToFocus = new Vector3(camWidth / 2 - offset, isNewFocus ? 0 : hitObj.transform.position.y, depth);
				focusingThisScreen = true;
			}
			else {
				posToFocus = new Vector3(camWidth / 2 + Mathf.Cos(-angle) * offset - Mathf.Sin(-angle) * depth, isNewFocus ? 0 : hitObj.transform.position.y, Mathf.Sin(-angle) * offset + Mathf.Cos(-angle) * depth);
				focusingOtherScreen = true;
			}
			state = Status.focus;
			focusNormal = (isThisScreen ? new Vector3(0, 0, -1) : new Vector3(-Mathf.Sin(angle), 0, -Mathf.Cos(-angle)));
		}
	}

	public void startNewFocusThisScreen() {
		startFocus(true, true);
	}

	public void startNewFocusOtherScreen() {
		startFocus(false, true);
	}

	/* #endregion */

	private void updateMesh() {
		hitVertices = hitObj.GetComponent<MeshFilter>().mesh.vertices;
		hitTriangles = hitObj.GetComponent<MeshFilter>().mesh.triangles;
		hitVerticesNum = hitVertices.Length;
		hitTrianglesNum = hitTriangles.Length;
	}

	public void recordRealMeasure() {
		hitObj.GetComponent<ObjectController>().isRealMeasure = true;
		hitObj.GetComponent<ObjectController>().realMeasure = hitObj.transform.localScale;
	}

	public void recoverRealMeasure() {
		if (!hitObj.GetComponent<ObjectController>().isRealMeasure) {
			hitObj.GetComponent<ObjectController>().isRealMeasure = true;
			hitObj.transform.localScale = hitObj.GetComponent<ObjectController>().realMeasure;
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

		hitObj.transform.position = new Vector3(0, 0, 4.5f);
		hitObj.transform.localScale = new Vector3(2, 2, 2);
		hitObj.transform.rotation = Quaternion.Euler(30, 60, 45);

		hitObj.GetComponent<MeshFilter>().mesh = defaultMesh;
		hitObj.GetComponent<MeshCollider>().sharedMesh = defaultMesh;
		hitObj.GetComponent<ObjectController>().isTransformUpdated = true;
		hitObj.GetComponent<ObjectController>().isMeshUpdated = true;
	}

	public void cancel() {
		isThisScreenFocused = false;
		isOtherScreenFocused = false;
		isEdgeAligned = false;
		selectTriangleIndex = -1;
		focusTriangleIndex = -1;
		hitObj.GetComponent<ObjectController>().cleanHighlight();
		closestVertex = -1;
		secondVertex = -1;
		touchPosition = INF;
		prevTouchPosition = INF;
		cam.orthographic = false;
		gridController.GetComponent<GridController>().isFixed = false;
	}
	/* #endregion */
}