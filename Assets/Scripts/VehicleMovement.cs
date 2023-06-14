using UnityEngine;

public class VehicleMovement : MonoBehaviour
{
	public float speed;						

	[Header("Drive Settings")]
	public float driveForce = 17f;			
	public float slowingVelFactor = .99f;   
	public float brakingVelFactor = .95f;   
	public float angleOfRoll = 30f;			

	[Header("Hover Settings")]
	public float hoverHeight = 1.5f;        
	public float maxGroundDist = 5f;        
	public float hoverForce = 300f;			
	public LayerMask whatIsGround;			
	public PIDController hoverPID;			

	[Header("Physics Settings")]
	public Transform shipBody;				
	public float terminalVelocity = 100f;  //Max move speed 
	public float hoverGravity = 20f;        
	public float fallGravity = 80f;         

	public LineRenderer lineRenderer;


    Rigidbody rigidBody;					
	PlayerInput input;						
	float drag;								
	bool isOnGround;						


	void Start()
	{
		rigidBody = GetComponent<Rigidbody>();
		input = GetComponent<PlayerInput>();

		drag = driveForce / terminalVelocity;
        
    }

	void FixedUpdate()
	{
		speed = Vector3.Dot(rigidBody.velocity, transform.forward);

		CalculatHover();
		CalculatePropulsion();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

	void CalculatHover()
	{
		Vector3 groundNormal;

		Ray ray = new Ray(transform.position, -transform.up);

        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        RaycastHit hitInfo;

        isOnGround = Physics.Raycast(ray, out hitInfo, maxGroundDist, whatIsGround);

		lineRenderer.SetPosition(0, new Vector3(ray.origin.x, ray.origin.y, ray.origin.z));
		lineRenderer.SetPosition(1, new Vector3(transform.position.x, hitInfo.distance, transform.position.z));

        float height = hitInfo.distance;

        if (isOnGround)
		{
			Debug.Log("На замле" + height + " < " + (maxGroundDist - 3));

            //...determine how high off the ground it is...
            //...save the normal of the ground...
            groundNormal = hitInfo.normal.normalized;
            //...use the PID controller to determine the amount of hover force needed...
            float forcePercent = hoverPID.Seek(hoverHeight, height);

            //...calulcate the total amount of hover force based on normal (or "up") of the ground...
            Vector3 force = groundNormal * hoverForce * forcePercent;
            //...calculate the force and direction of gravity to adhere the ship to the 
            //track (which is not always straight down in the world)...
            Vector3 gravity = -groundNormal * hoverGravity * height;

            //...and finally apply the hover and gravity forces
            rigidBody.AddForce(force, ForceMode.Acceleration);
            rigidBody.AddForce(gravity, ForceMode.Acceleration);



            //groundNormal = hitInfo.normal.normalized;
			//float forcePercent = hoverPID.Seek(hoverHeight, height);
			//Vector3 force = groundNormal * hoverForce * forcePercent;
			//Vector3 gravity = groundNormal * hoverGravity;
            //if (height < maxGroundDist - 3)
            //rigidBody.AddForce(gravity, ForceMode.Acceleration);
            //rigidBody
        }
		else
		{
            Debug.Log("Не На замле: " + height+  " > "+ maxGroundDist + 3);
            //...use Up to represent the "ground normal". This will cause our ship to
            //self-right itself in a case where it flips over
            groundNormal = Vector3.up;

            //Calculate and apply the stronger falling gravity straight down on the ship
            Vector3 gravity = -groundNormal * fallGravity;
            rigidBody.AddForce(gravity, ForceMode.Acceleration);
        }

		Vector3 projection = Vector3.ProjectOnPlane(transform.forward, groundNormal);
		Quaternion rotation = Quaternion.LookRotation(projection, groundNormal);

		//rigidBody.MoveRotation(Quaternion.Lerp(rigidBody.rotation, rotation, Time.deltaTime * 10f));

		float angle = angleOfRoll * -input.rudder;

		//Quaternion bodyRotation = transform.rotation * Quaternion.Euler(0f, 0f, angle);
		//shipBody.rotation = Quaternion.Lerp(shipBody.rotation, bodyRotation, Time.deltaTime * 10f);
	}

	void CalculatePropulsion()
	{
		float rotationTorque = input.rudder - rigidBody.angularVelocity.y; //rudder - horizontal
		rigidBody.AddRelativeTorque(0f, rotationTorque, 0f, ForceMode.VelocityChange); //Добавление крутящего момента

		float sidewaysSpeed = Vector3.Dot(rigidBody.velocity, transform.right); //проверка направления, скаляр векторов
		Vector3 sideFriction = -transform.right * (sidewaysSpeed / Time.fixedDeltaTime); //добавляем замедление поворота
		rigidBody.AddForce(sideFriction, ForceMode.Acceleration);

		if (input.thruster <= 0f)	//thruster - vertical
			rigidBody.velocity *= slowingVelFactor;

		if (input.isBraking)
			rigidBody.velocity *= brakingVelFactor;

		float propulsion = driveForce * input.thruster - drag * Mathf.Clamp(speed, 0f, terminalVelocity);
		rigidBody.AddForce(transform.forward * propulsion, ForceMode.Acceleration);
	}

	//Отталкивание от объекта(временно стены)
	void OnCollisionStay(Collision collision)
	{
		if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
		{
			Vector3 upwardForceFromCollision = Vector3.Dot(collision.impulse, transform.up) * transform.up;
			rigidBody.AddForce(-upwardForceFromCollision, ForceMode.Impulse);
		}
	}

	public float GetSpeedPercentage()
	{
		return rigidBody.velocity.magnitude / terminalVelocity;
	}
}
