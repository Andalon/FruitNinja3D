private var motor : CharacterMotor;
private var weapNum : int;
private var Meshkatana1 : GameObject;
private var Meshkatana2 : GameObject[];
private var MeshGrapple1 : GameObject;
private var MeshGrapple2 : GameObject;
private var MeshGrapple3 : GameObject;
private var MeshGrapple4 : GameObject;
private var MeshGrapple5 : GameObject;
private var MeshStar1 : GameObject;
private var MeshStar2 : GameObject;
private var timer : float;


// Use this for initialization
function Awake () {
	motor = GetComponent(CharacterMotor);
}

function Start() {
	weapNum = 0;
	Meshkatana1 = GameObject.FindGameObjectWithTag("Katana1");
	Meshkatana2 = GameObject.FindGameObjectsWithTag("Katana2");
	MeshGrapple1 = GameObject.FindGameObjectWithTag("Grapple1");
	MeshGrapple2 = GameObject.FindGameObjectWithTag("Grapple2");
	MeshGrapple3 = GameObject.FindGameObjectWithTag("Grapple3");
	MeshGrapple4 = GameObject.FindGameObjectWithTag("Grapple4");
	MeshGrapple5 = GameObject.FindGameObjectWithTag("Grapple5");
	MeshStar1 = GameObject.FindGameObjectWithTag("NinjaStar1");
	MeshStar2 = GameObject.FindGameObjectWithTag("NinjaStar2");
	
	Meshkatana1.active = true;
	for (var go : GameObject in Meshkatana2)
		go.active = true;
	MeshGrapple1.active = false;
	MeshGrapple2.active = false;
	MeshGrapple3.active = false;
	MeshGrapple4.active = false;
	MeshGrapple5.active = false;
	MeshStar1.active = false;
	MeshStar2.active = false;

	timer = 0.0F;
}

// Update is called once per frame
function Update () {
	// Get the input vector from kayboard or analog stick
	var directionVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
	
	if (directionVector != Vector3.zero) {
		// Get the length of the directon vector and then normalize it
		// Dividing by the length is cheaper than normalizing when we already have the length anyway
		var directionLength = directionVector.magnitude;
		directionVector = directionVector / directionLength;
		
		// Make sure the length is no bigger than 1
		directionLength = Mathf.Min(1, directionLength);
		
		// Make the input vector more sensitive towards the extremes and less sensitive in the middle
		// This makes it easier to control slow speeds when using analog sticks
		directionLength = directionLength * directionLength;
		
		// Multiply the normalized direction vector by the modified length
		directionVector = directionVector * directionLength;
	}
	
	// Apply the direction to the CharacterMotor
	motor.inputMoveDirection = transform.rotation * directionVector;
	motor.inputJump = Input.GetButton("Jump");
	
	//Code for changing weapons
	if (Input.GetKey("tab")){
		if (timer <= 0.9){
			if (weapNum == 2)
				weapNum = 0;
			else
				weapNum++;
		
		
			if (weapNum == 0){
				Meshkatana1.active = true;
				for (var go : GameObject in Meshkatana2)
					go.active = true;
				MeshGrapple1.active = false;
				MeshGrapple2.active = false;
				MeshGrapple3.active = false;
				MeshGrapple4.active = false;
				MeshGrapple5.active = false;
				MeshStar1.active = false;
				MeshStar2.active = false;
				timer = 1.0F;
			}
		
			else if (weapNum == 1){
				Meshkatana1.active = false;
				for (var go : GameObject in Meshkatana2)
					go.active = false;
				MeshStar1.active = true;
				MeshStar2.active = true;
				MeshGrapple1.active = false;
				MeshGrapple2.active = false;
				MeshGrapple3.active = false;
				MeshGrapple4.active = false;
				MeshGrapple5.active = false;
				timer = 1.0F;
			}
		
			else{
				Meshkatana1.active = false;
				for (var go : GameObject in Meshkatana2)
					go.active = false;
				MeshStar1.active = false;
				MeshStar2.active = false;
				MeshGrapple1.active = true;
				MeshGrapple2.active = true;
				MeshGrapple3.active = true;
				MeshGrapple4.active = true;
				MeshGrapple5.active = true;
				timer = 1.0F;
			}
		}
		else
			timer -= Time.deltaTime;
		
		print (weapNum);
	}
	
	
}

// Require a character controller to be attached to the same game object
@script RequireComponent (CharacterMotor)
@script AddComponentMenu ("Character/FPS Input Controller")
