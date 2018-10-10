using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterOfMass : MonoBehaviour {



	public GameObject[] boids;
	public Vector3 Position2B;
	public Vector3 Vel2B;

	public Vector3 Avg_Vel;
	public float Vel_Augment = 1f;


	// Use this for initialization
	void Start () {
		Calculate ();
	}
	
	// Update is called once per frame
	void Update () {
		Calculate ();
	}

	void Calculate(){
		Position2B = Vector3.zero;
		Vel2B = Vector3.zero;
		for (int i = 0; i < boids.Length; i++) {
			Movement tempM = boids [i].GetComponent<Movement> ();
			Position2B += boids [i].transform.position;
			Vel2B += tempM.getVel ();

		}
		Position2B /= boids.Length;
		Vel2B /= boids.Length;
		Avg_Vel = Vel2B * Vel_Augment;
		transform.position = Position2B;


	}


}
