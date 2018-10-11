using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvoidanceSelector : MonoBehaviour {
    [SerializeField] private bool startWithConeCheck;
    [SerializeField] private Text text;

	// Use this for initialization
	void Awake () {
		avoidanceMode = startWithConeCheck;
	}

    void Start() {
        text.text = avoidanceMode ? "Cone Check" : "Collision Prediction";
    }

    public void OnModeSwitch() {
        avoidanceMode = !avoidanceMode;
        text.text = avoidanceMode ? "Cone Check" : "Collision Prediction";
    }

    public static bool avoidanceMode{get; private set;}
}
