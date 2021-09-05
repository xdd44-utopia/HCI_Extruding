using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour
{
	// public GameObject slider;
	// public GameObject cam;

	public Text debugText;

	[HideInInspector]
	public float angle;
	public GameObject pointer;

	private float defaultAngle = - Mathf.PI / 2;
	private const float leftMost = -337.5f;
	private const float rightMost = 337.5f;

	// Start is called before the first frame update
	void Start()
	{
		angle = defaultAngle;
	}

	// Update is called once per frame
	void Update() {
		float pos = leftMost + (rightMost - leftMost) * (Mathf.PI / 2 + angle) / (Mathf.PI / 2);
		pointer.GetComponent<RectTransform>().anchoredPosition = new Vector3(pos, 750f, 0);
	}
}
