using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public int buttonNum;
	public GameObject faceTracker;
	void Start() {
		
	}

	// Update is called once per frame
	void Update() {
		
	}
	public void OnPointerDown(PointerEventData eventData){
		switch (buttonNum) {
			case 0: faceTracker.GetComponent<FaceTracker>().increaseX = true; break;
			case 1: faceTracker.GetComponent<FaceTracker>().decreaseX = true; break;
			case 2: faceTracker.GetComponent<FaceTracker>().increaseY = true; break;
			case 3: faceTracker.GetComponent<FaceTracker>().decreaseY = true; break;
			case 4: faceTracker.GetComponent<FaceTracker>().increaseZ = true; break;
			case 5: faceTracker.GetComponent<FaceTracker>().decreaseZ = true; break;
		}
	}
	
	public void OnPointerUp(PointerEventData eventData){
		switch (buttonNum) {
			case 0: faceTracker.GetComponent<FaceTracker>().increaseX = false; break;
			case 1: faceTracker.GetComponent<FaceTracker>().decreaseX = false; break;
			case 2: faceTracker.GetComponent<FaceTracker>().increaseY = false; break;
			case 3: faceTracker.GetComponent<FaceTracker>().decreaseY = false; break;
			case 4: faceTracker.GetComponent<FaceTracker>().increaseZ = false; break;
			case 5: faceTracker.GetComponent<FaceTracker>().decreaseZ = false; break;
		}
	}
}
