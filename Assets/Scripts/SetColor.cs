using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetColor : MonoBehaviour {
    [SerializeField] private Color color;

	// Use this for initialization
	void Start () {
		GetComponent<MeshRenderer>().material.color = color;
	}
}
