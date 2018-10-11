using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Separation{
	public Vector3 direction;
	public float rot;
	public int amnt;


}

public struct Steering{
	public Vector3 vel;
	public float rot;

	public Steering(Vector3 n_vel, float n_rot){
		vel = n_vel;
		rot = n_rot;
	}

	public static Steering operator+(Steering lhs, Steering rhs){
		return new Steering(lhs.vel + rhs.vel, (lhs.rot + rhs.rot) % 360.0f);
	}

	public static Steering operator*(Steering lhs, float rhs){
		return new Steering(lhs.vel *rhs, (lhs.rot *rhs) % 360.0f);
	}

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
	[SerializeField] private float Repultion_Strength = 5f;
	[SerializeField] private float Separation_Weight = 1f;
	[SerializeField] private float Cohesion_Weight = 1f;
	[SerializeField] private float GroupVel_Weight = 1f;
	[SerializeField] private float Target_Weight = 1f;
	[SerializeField] bool cone = false;
	[SerializeField] float CollisioPredictionWeight = 2f;
	[SerializeField] float AngleOfPerception = 30f;


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
	float maxAngularAcceleration = 90f;
	float maxRotation = 120f;

	public Transform[] _path;
	int _pathIndex = 0;

	float slowRangeDeg = 20f;

	float timeToTarget = 0.25f;

	Vector3 cVel;
	float cRot = 0f;
	public GameObject _target;

    private SphereCollider collider;

	float rotationCooldown = 20f;
	// Use this for initialization
	void Start () {
        collider = GetComponent<SphereCollider>();
	}

	// Update is called once per frame
	void Update () {

		//Vector3 vel;
		Steering s;


		switch (_state) {

		case Type.pursue:
			
			s = Pursue (_target);
			move (s.vel,s.rot);
			break;
		case Type.align:
			float tempRoatation = Align(_target.transform.position);
			move (Vector3.zero,tempRoatation);
			break;

		case Type.arrive:

			s = Arrive (_target.transform.position);
			move (s.vel,s.rot);

			break;

		case Type.evade:
			s = Evade (_target);
			move (s.vel,s.rot);


			break;

		case Type.wander:
			s = Wander (_target);
			move (s.vel,s.rot);


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
			s = FollowPath (_path);

			move (s.vel,s.rot);
			break;

		case Type.flock:

			s = CalculateSeperation (FLOCK_ID, F_Radius) * Separation_Weight;
			//F_Separation = s.direction;


			s += Pursue (FLOCK_CENTER) * Cohesion_Weight;
			s += Pursue (_target) * Target_Weight;
			//Seek ((F_Separation)* s.amnt * Separation_Weight);
			

			if (cone) {
				s += ConeCheck (FLOCK_ID, F_Radius * 2) * Separation_Weight;
			} else {
				collisionPrediction (FLOCK_ID, F_Radius * 10f);
			}

			if (s.vel.magnitude > maxSpeed) {
				s.vel.Normalize ();
				s.vel *= maxSpeed;
			}

			float angularAcceleration = Mathf.Abs (s.rot);
			if (angularAcceleration > maxAngularAcceleration) {
				s.rot /= angularAcceleration;
				s.rot *= maxAngularAcceleration;
			}
			s.rot = Face (_target.transform.position);
			move (s.vel, s.rot);
			//move ();
			break;

		case Type.none:

			break;



		}


	}

	Steering collisionPrediction(int id, float radius){
		float dist = Mathf.Infinity;
		Movement closest = null;
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
		float minTime = Mathf.Infinity;
        float minSeparation = Mathf.Infinity;
        float distance = -1;
        Vector3 relativePos = Vector3.zero;
        Vector3 relativeVel = Vector3.zero;
        for (int i = 0; i < hitColliders.Length; i++) {
			Movement tempM = hitColliders [i].gameObject.GetComponent<Movement> ();
			if (tempM != null) {
				if (tempM.FLOCK_ID != FLOCK_ID && tempM.FLOCK_ID != -1) {
                    Vector3 rp = tempM.transform.position - transform.position;
                    Vector3 rv = tempM.getVel() - getVel();
                    float relativeSpeed = rv.sqrMagnitude;
                    float collisionTime = Vector3.Dot(rp, rv)/relativeSpeed;

                    float relativeDist = rp.magnitude;
                    float ms = relativeDist - relativeSpeed * minTime;
                    if (minTime > 2 * collider.radius) {
                        continue;
                    }
                    
                    if(collisionTime > 0 && collisionTime < minTime) {
                        minTime = collisionTime;
                        minSeparation = ms;
                        distance = relativeDist;
                        relativePos = rp;
                        relativeVel = rv;
                        closest = tempM;
                    }
				}
			}
		}
		if (closest != null) {
			if (minSeparation <= 0 || distance <= 2 * collider.radius) {
                relativePos = closest.transform.position - transform.position;
            } else {
                relativePos = relativePos + relativeVel * minTime;
            }
            
            return Flee(transform.position + relativePos);
		}
		return new Steering (Vector3.zero, transform.rotation.eulerAngles.y);
	}

	Steering ConeCheck (int id, float radius){

		Vector3 separation = Vector3.zero;
		int boidz = 0;
		//Movement closest;
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
		for (int i = 0; i < hitColliders.Length; i++) {
			Movement tempM = hitColliders [i].gameObject.GetComponent<Movement> ();
			if (tempM != null && tempM.gameObject != gameObject) {
				if(Mathf.Abs(Vector3.Angle(transform.forward, tempM.transform.position - transform.position)) < AngleOfPerception){
					if (tempM.FLOCK_ID != FLOCK_ID && tempM.FLOCK_ID != -1f) {
						//print ("BiRD HIT");
						//Flee (tempM.gameObject.transform.position );
						//separation += (-tempM.gameObject.transform.position + transform.position) * Mathf.Abs((-tempM.gameObject.transform.position + transform.position).magnitude);

						Vector3 tempDirection = tempM.gameObject.transform.position - transform.position;
						float lenSq = tempDirection.sqrMagnitude;
						separation += Repultion_Strength/lenSq * tempDirection;
						boidz += 1;
					}
				}

			}

		}

		//separation /= boidz;
		//todo return calculated separation vector
		//return new Separation{direction = separation, amnt = boidz};
		return Flee(separation + transform.position);
	}



	Steering CalculateSeperation (int id, float radius){

		Vector3 separation = Vector3.zero;
		int boidz = 0;
		//Movement closest;
		Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
		for (int i = 0; i < hitColliders.Length; i++) {
			Movement tempM = hitColliders [i].gameObject.GetComponent<Movement> ();
			if (tempM != null && tempM.gameObject != gameObject) {
				if (tempM.FLOCK_ID == FLOCK_ID) {
					//print ("BiRD HIT");
					//Flee (tempM.gameObject.transform.position );
					//separation += (-tempM.gameObject.transform.position + transform.position) * Mathf.Abs((-tempM.gameObject.transform.position + transform.position).magnitude);

					Vector3 tempDirection = tempM.gameObject.transform.position - transform.position;
					float lenSq = tempDirection.sqrMagnitude;
					separation += Repultion_Strength/lenSq * tempDirection;
					boidz += 1;
				}

			}

		}

		//separation /= boidz;
		//todo return calculated separation vector
		//return new Separation{direction = separation, amnt = boidz};
		return Flee(separation + transform.position);
	}

	Steering SeekPath(Vector3 target){
		Vector3 vel = target - transform.position;
		if (vel.magnitude < slowRadius) {
			//_state = Type.arrive;
		}
		vel /= timeToTarget;
		if (vel.magnitude > maxSpeed) {
			vel.Normalize ();
			vel *= maxSpeed;
		}
		//Face (target);
		Debug.DrawLine (transform.position, target, Color.blue);

		//transform.rotation = Quaternion.Euler (0f, getNewOrientation (transform.rotation.eulerAngles, vel), 0f);
		return new Steering (vel, Face(target));
		//move (vel,   0   );

	}


	Steering Seek(Vector3 target){
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
		//Face (target);
		//transform.rotation = Quaternion.Euler (0f, getNewOrientation (transform.rotation.eulerAngles, vel), 0f);
		//move (vel,   0   );
		return new Steering (vel, Face(target));

	}

	Steering Flee(Vector3 target){
		Vector3 vel = -target + transform.position;


		vel /= timeToTarget;
		if (vel.magnitude > maxSpeed) {
			vel.Normalize ();
			vel *= maxSpeed;
		}

		//transform.rotation = Quaternion.Euler (0f, getNewOrientation (transform.rotation.eulerAngles, vel), 0f);
		//Face(-target);
		//move (vel,   0   );
		Debug.DrawLine (transform.position, target, Color.yellow);
		return new Steering (vel, Face(-target));

	}

	Steering Pursue(GameObject target){
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
		//Face (target.transform.position);
		//Seek (target.transform.position + tVel * preditction);
        Debug.DrawLine (transform.position, target.transform.position + tVel * preditction, Color.red);
		return Seek(target.transform.position + tVel * preditction);
	}

	Steering Arrive(Vector3 target){

		Vector3 vel = target - transform.position;
		float dist = vel.magnitude;
		if (dist > slowRadius) {
			_state = Type.pursue;
		}
		if (dist < targetRadius) {
			return new Steering(Vector3.zero, transform.rotation.eulerAngles.y);
		}

		float targetSpeed = maxSpeed * dist / slowRadius;
		vel.Normalize ();
		vel *= targetSpeed;
		vel = vel - cVel;
		vel /= timeToTarget;

		//Face (target);
		//transform.rotation = Quaternion.Euler (0f, getNewOrientation (transform.rotation.eulerAngles, vel), 0f);
		//move (vel,   0   );
		return new Steering (vel, Face(target));
	}

	Steering Evade(GameObject target){
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

		return Flee (target.transform.position + tVel * preditction);


	}

	Steering Wander(GameObject targetG){

		wanderOrientation += randomBinomial () * wanderRate;
		float targetOrientation = wanderOrientation + transform.rotation.eulerAngles.y;

		Vector3 target = transform.position + wanderOffset * transform.forward;
		//print ("target position before: " + target);
		target += wanderRange * new Vector3 (Mathf.Cos(Mathf.Deg2Rad * targetOrientation), 0, Mathf.Sin(Mathf.Deg2Rad * targetOrientation));
		//Face (target);

		Debug.DrawLine (transform.position, target, Color.green);
		//print ("target position: " + target);
		//print ("current position " + transform.position);
		//transform.rotation = Quaternion.Euler (0f, transform.rotation.eulerAngles.y + randomBinomial () * wanderRange, 0f);
		//move (transform.forward * maxSpeed,0);
		if ((targetG.transform.position - transform.position).magnitude < persueDetectRange) {
			_state = Type.pursue;

		}
		return new Steering (transform.forward * maxSpeed, Face(target));

	}

	float Align(Vector3 target){
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
			return transform.rotation.eulerAngles.y;
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
		//move (Vector3.zero, sAngular);
		return sAngular;


	}

	float Face(Vector3 target){
		return Align (target);

	}

	Steering FollowPath(Transform[] path){
		float dist = (transform.position - path [_pathIndex].position).magnitude;
		if (dist < targetRadius * 4) {
			_pathIndex += 1;
		}
		if (_pathIndex < path.Length - 1) {
			return SeekPath (path [_pathIndex].position);
		} else if(_pathIndex < path.Length){
			return Seek (path [_pathIndex].position);
		}else {
			_state = Type.none;
			return new Steering (Vector3.zero, transform.rotation.eulerAngles.y);
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
