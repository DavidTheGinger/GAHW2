using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Separation{
	public Vector3 direction;
	public int amnt;


}


public class Movement : MonoBehaviour {

	public enum Type{
		pursue,
		arrive,
		evade,
		wander,
		startFollowPath,
		followPath,
		align,
		flock,
		none

	}

	public int FLOCK_ID;
	public Vector3 _F_COG;
	public Vector3 _F_Vel;
	Vector3 F_Separation;
	[SerializeField] float F_Radius = 4f;
	public GameObject FLOCK_CENTER;
	[SerializeField] private float Separation_Weight = 1f;
	[SerializeField] bool cone = false;
	[SerializeField] float CollisioPredictionWeight = 2f;



	public Type _state = Type.wander;
	public float maxSpeed = 2f;
	[SerializeField] float slowRadius = 6f;
	float targetRadius = 1f;
	float wanderSpeed = 1f;
	float wanderRange = 100f;
	float wanderRate = 20f;
	float wanderOffset = 1f;
	float wanderOrientation = 0f;
	public float persueDetectRange = 5f;
	float maxPrediction = 5f;
	float maxAngularAcceleration = 50f;
	float maxRotation = 50f;

	public Transform[] _path;
	int _pathIndex = 0;

	float slowRangeDeg = 20f;

	float timeToTarget = 0.25f;

	Vector3 cVel;
	float cRot = 0f;
	public GameObject _target;

	float rotationCooldown = 20f;
	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

		Vector3 vel;



		switch (_state) {

		case Type.pursue:
			Pursue (_target);

			break;
		case Type.align:
			Align(_target.transform.position);
			break;

		case Type.arrive:

			Arrive (_target.transform.position);

			break;

		case Type.evade:
			Evade (_target);


			break;

		case Type.wander:
			Wander (_target);


			break;
		case Type.startFollowPath:
			float minDist = Mathf.Infinity;

			for (int i = 0; i < _path.Length; i++) {
				if ((transform.position - _path [i].position).magnitude < minDist) {
					minDist = (transform.position - _path [i].position).magnitude;
					_pathIndex = i;
				}
			}
			_state = Type.followPath;
			break;
		case Type.followPath:
			FollowPath (_path);
			break;

		case Type.flock:

			Separation s = CalculateSeperation (FLOCK_ID, F_Radius);
			F_Separation = s.direction;


			Pursue (FLOCK_CENTER);
			//Seek ((F_Separation)* s.amnt * Separation_Weight);
			Pursue (_target);

			if (cone) {

			} else {
				collisionPrediction (FLOCK_ID, F_Radius * 10f);
			}

			break;

		case Type.none:

			break;



		}


	}

	void collisionPrediction(int id, float radius){
		float dist = Mathf.Infinity;
		Movement closest = null;
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
		for (int i = 0; i < hitColliders.Length; i++) {
			Movement tempM = hitColliders [i].gameObject.GetComponent<Movement> ();
			if (tempM != null) {
				if (tempM.FLOCK_ID != FLOCK_ID) {
					if (Mathf.Abs ((-tempM.gameObject.transform.position + transform.position).magnitude) < dist) {
						closest = tempM;
					}
					//Flee (tempM.gameObject.transform.position );

				}

			}

		}
		if (closest != null) {
			Flee (closest.gameObject.transform.position);
		}
	}

	Separation CalculateSeperation (int id, float radius){

		Vector3 separation = Vector3.zero;
		int boidz = 0;
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
		for (int i = 0; i < hitColliders.Length; i++) {
			Movement tempM = hitColliders [i].gameObject.GetComponent<Movement> ();
			if (tempM != null) {
				if (tempM.FLOCK_ID == FLOCK_ID) {
					//print ("BiRD HIT");
					Flee (tempM.gameObject.transform.position );
					separation += (-tempM.gameObject.transform.position + transform.position) * Mathf.Abs((-tempM.gameObject.transform.position + transform.position).magnitude);
					boidz += 1;
				}

			}

		}

		separation /= boidz;
		//todo return calculated separation vector
		return new Separation{direction = separation, amnt = boidz};
	}

	void SeekPath(Vector3 target){
		Vector3 vel = target - transform.position;
		if (vel.magnitude < slowRadius) {
			//_state = Type.arrive;
		}
		vel /= timeToTarget;
		if (vel.magnitude > maxSpeed) {
			vel.Normalize ();
			vel *= maxSpeed;
		}
		Face (target);
		//transform.rotation = Quaternion.Euler (0f, getNewOrientation (transform.rotation.eulerAngles, vel), 0f);
		move (vel,   0   );
		Debug.DrawLine (transform.position, target, Color.blue);

	}


	void Seek(Vector3 target){
		Vector3 vel = target - transform.position;
		if (vel.magnitude < slowRadius) {
			_state = Type.arrive;
			//Arrive(target);
		}
		vel /= timeToTarget;
		if (vel.magnitude > maxSpeed) {
			vel.Normalize ();
			vel *= maxSpeed;
		}
		Face (target);
		//transform.rotation = Quaternion.Euler (0f, getNewOrientation (transform.rotation.eulerAngles, vel), 0f);
		move (vel,   0   );

	}

	void Flee(Vector3 target){
		Vector3 vel = -target + transform.position;


		vel /= timeToTarget;
		if (vel.magnitude > maxSpeed) {
			vel.Normalize ();
			vel *= maxSpeed;
		}

		//transform.rotation = Quaternion.Euler (0f, getNewOrientation (transform.rotation.eulerAngles, vel), 0f);
		Face(-target);
		move (vel,   0   );
		Debug.DrawLine (transform.position, target, Color.grey);

	}

	void Pursue(GameObject target){
		Vector3 direction = target.transform.position - transform.position;
		float dist = direction.magnitude;
		float speed = cVel.magnitude;
		float preditction = 0f;
		if (speed <= dist / maxPrediction) {
			preditction = maxPrediction;
		} else {
			maxPrediction = dist / speed;
		}
		Vector3 tVel = Vector3.zero;
		if (target.GetComponent<Movement> ()) {
			Movement temp = target.GetComponent<Movement> ();
			tVel = temp.cVel;
		} else if (target.GetComponent<CenterOfMass> ()) {
			CenterOfMass temp = target.GetComponent<CenterOfMass> ();
			tVel = temp.Avg_Vel;

		}
		Face (target.transform.position);
		Seek (target.transform.position + tVel * preditction);
		Debug.DrawLine (transform.position, target.transform.position + tVel * preditction, Color.red);

	}

	void Arrive(Vector3 target){

		Vector3 vel = target - transform.position;
		float dist = vel.magnitude;
		if (dist > slowRadius) {
			_state = Type.pursue;
		}
		if (dist < targetRadius) {
			return;
		}

		float targetSpeed = maxSpeed * dist / slowRadius;
		vel.Normalize ();
		vel *= targetSpeed;
		vel = vel - cVel;
		vel /= timeToTarget;

		Face (target);
		//transform.rotation = Quaternion.Euler (0f, getNewOrientation (transform.rotation.eulerAngles, vel), 0f);
		move (vel,   0   );
	}

	void Evade(GameObject target){
		Vector3 direction = target.transform.position - transform.position;
		float dist = direction.magnitude;
		float speed = cVel.magnitude;
		float preditction = 0f;
		if (speed <= dist / maxPrediction) {
			preditction = maxPrediction;
		} else {
			maxPrediction = dist / speed;
		}
		Vector3 tVel = Vector3.zero;

		if (target.GetComponent<Movement>()) {
			Movement temp = target.GetComponent<Movement>();
			tVel = temp.cVel;
		}

		Flee (target.transform.position + tVel * preditction);


	}

	void Wander(GameObject targetG){

		wanderOrientation += randomBinomial () * wanderRate;
		float targetOrientation = wanderOrientation + transform.rotation.eulerAngles.y;

		Vector3 target = transform.position + wanderOffset * transform.forward;
		//print ("target position before: " + target);
		target += wanderRange * new Vector3 (Mathf.Cos(Mathf.Deg2Rad * targetOrientation), 0, Mathf.Sin(Mathf.Deg2Rad * targetOrientation));
		Face (target);

		Debug.DrawLine (transform.position, target, Color.green);
		//print ("target position: " + target);
		//print ("current position " + transform.position);
		//transform.rotation = Quaternion.Euler (0f, transform.rotation.eulerAngles.y + randomBinomial () * wanderRange, 0f);
		move (transform.forward * maxSpeed,0);
		if ((targetG.transform.position - transform.position).magnitude < persueDetectRange) {
			_state = Type.pursue;

		}

	}

	void Align(Vector3 target){
		float trot = getNewOrientation (transform.rotation.eulerAngles, target - transform.position);
		//print (trot);
		float rotation = (trot - transform.rotation.eulerAngles.y) % 360f;

		/*
		print ("rotation needed as degrees:" + rotation);
		rotation = Mathf.Deg2Rad * rotation;
		print ("before: "+ rotation);
		if (Mathf.Abs(rotation) > 2) {
			rotation = 2 - rotation;
		}
		print ("after: "+ rotation);*/
		if (Mathf.Abs(rotation) > 180) {
			rotation = 360f - Mathf.Abs(rotation) * -(rotation/Mathf.Abs(rotation));
		}
		//print("needed rotation in degrees: " + rotation);
		float rotationSize = Mathf.Abs (rotation);
		if (rotationSize < targetRadius) {
			cRot = 0f;
			return;
		}
		float targetRotation;
		if (rotationSize > slowRangeDeg) {
			targetRotation = maxRotation;
		} else {
			targetRotation = maxRotation * rotationSize / slowRangeDeg;
		}
		targetRotation *= rotation / rotationSize;
		//print ("target rotation: " + targetRotation);
		//float sAngular = targetRotation - transform.rotation.eulerAngles.y;
		float sAngular = targetRotation;
		//print ("sAngular: " + sAngular);
		sAngular /= timeToTarget;

		float angularAcceleration = Mathf.Abs (sAngular);
		if (angularAcceleration > maxAngularAcceleration) {
			sAngular /= angularAcceleration;
			sAngular *= maxAngularAcceleration;
		}
		move (Vector3.zero, sAngular);


	}

	void Face(Vector3 target){
		Align (target);

	}

	void FollowPath(Transform[] path){
		float dist = (transform.position - path [_pathIndex].position).magnitude;
		if (dist < targetRadius * 4) {
			_pathIndex += 1;
		}
		if (_pathIndex < path.Length - 1) {
			SeekPath (path [_pathIndex].position);
		} else if(_pathIndex < path.Length){
			Seek (path [_pathIndex].position);
		}else {
			_state = Type.none;
		}

	}




	float getNewOrientation(Vector3 currentOrientation, Vector3 velocity){
		if (velocity.magnitude > 0) {
			return Mathf.Atan2 (velocity.x, velocity.z) * Mathf.Rad2Deg;
		} else {
			return currentOrientation.y;

		}


	}

	float randomBinomial(){

		return Random.Range (0f, 1f) - Random.Range (0f, 1f);
	}

	void move(Vector3 vel, float rot){
		//transform.position = new Vector3 (transform.position.x + vel.x * Time.deltaTime, transform.position.y + vel.y * Time.deltaTime, transform.position.z + vel.z * Time.deltaTime);
		transform.position = transform.position + Time.deltaTime * cVel;
		transform.rotation = Quaternion.Euler (0,transform.rotation.eulerAngles.y + cRot * Time.deltaTime,0);

		cVel += vel* Time.deltaTime;
		cRot += rot* Time.deltaTime;



		if(cVel.magnitude > maxSpeed){
			cVel.Normalize ();
			cVel *= maxSpeed;
		}
		if (cRot > maxRotation) {
			cRot /= Mathf.Abs (cRot);
			cRot *= maxRotation;
		}
	}

	public Vector3 getVel(){

		return cVel;
	}
	public float getRot(){
		return cRot;
	}

}
