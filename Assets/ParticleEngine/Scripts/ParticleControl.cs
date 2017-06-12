using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleControl : MonoBehaviour {

	private const float HEAD_LENGTH		= 0.13f;
	private const float HEAD_WIDTH		= 0.10f;
	private const float HEAD_DEPTH		= 0.10f;
	private const float PALM_LENGTH		= 0.07f;
	private const float PALM_WIDTH		= 0.07f;
	private const float PALM_DEPTH		= 0.03f;
	private const float FINGER_LENGTH	= 0.08f;
	private const float FINGER_RADIUS	= 0.02f;

	private const int NUM_HAND_COLLIDERS = 3;
	
	private const int NUM_HANDS = 2;

	private const int THUMB_FINGER 	= 0;
	private const int INDEX_FINGER 	= 1;
	private const int PINKY_FINGER 	= 2;
	private const int NUM_FINGERS_PER_HAND	= 3;
	
	private const int NUM_EMITTERS = NUM_FINGERS_PER_HAND * NUM_HANDS;

	public const int ECOSYSTEM_NULL		= -1;
	public const int ECOSYSTEM_CHASE 	=  0;
	public const int ECOSYSTEM_FLURRY	=  1;
	public const int ECOSYSTEM_WTF		=  2;
	public const int NUM_ECOSYSTEMS		=  3;

	private int BUTTON_MARGIN 		= 10;
	private int CLEAR_BUTTON_WIDTH  = 120;
	private int CLEAR_BUTTON_HEIGHT = 20;

  	private struct ParticleEmitter 
	{
		public  bool	active;
		public	Vector3 position;
		public	Vector3 direction;
		public	float   strength;
		public  float 	rate;
		public	int		species;
  	}

 	private struct Finger 
	{
		public GameObject gameObject;
		public float rightOffset;
		public float upOffset;
		public float forwardRotation;
  	}

	private struct Hand 
	{
		public GameObject palm;
		public Finger[] fingers;
		public bool isRightHand;
  	}

 	private struct CapsuleCollisionVolume 
	{
		public  Vector3 p0;
		public  Vector3 p1;
        public 	float radius;
  	}

	public  Camera		_camera;
	private GameObject 	_myHead;
	private Hand 		_myLeftHand;
	private Hand 		_myRightHand;
	private ParticleEmitter[] _emitters;
	private CapsuleCollisionVolume[] _handColliderArray;

	private bool 	_clearRequested = false;
	private Rect 	_clearButtonRect;
	private string 	_clearButtonString;
	private bool	_displayBody = true;

	//---------------------------------------
	// public tweakers
	//---------------------------------------
	public int  _ecosystem 			= ECOSYSTEM_CHASE;
	public bool _showHeadAndHands 	= false;
	public bool _leftThumbActive 	= false;
	public bool _leftIndexActive 	= true;
	public bool _leftPinkyActive 	= true;
	public bool _rightThumbActive 	= false;
	public bool _rightIndexActive 	= true;
	public bool _rightPinkyActive	= true;

	//---------------------------------------
	void Start() 
	{
		//------------------------------------
		// create the clear button
		//------------------------------------
		_clearButtonRect = new 
		Rect
		( 
			Screen.width  - CLEAR_BUTTON_WIDTH  - BUTTON_MARGIN, 
			Screen.height - CLEAR_BUTTON_HEIGHT - BUTTON_MARGIN, 
			CLEAR_BUTTON_WIDTH, 
			CLEAR_BUTTON_HEIGHT 
		);
	

		_myHead				= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myLeftHand.palm 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myRightHand.palm 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );

		_myLeftHand.fingers  = new Finger[ NUM_FINGERS_PER_HAND ];
		_myRightHand.fingers = new Finger[ NUM_FINGERS_PER_HAND ];
		
		for (int f=0; f<NUM_FINGERS_PER_HAND; f++) 
		{
			_myLeftHand.fingers [f].rightOffset = PALM_WIDTH * 0.3f;
			_myRightHand.fingers[f].rightOffset = PALM_WIDTH * 0.3f;

			_myLeftHand.fingers [f].forwardRotation = 0.0f;
			_myRightHand.fingers[f].forwardRotation = 0.0f;

			if ( f == INDEX_FINGER ) 
			{ 
				_myRightHand.fingers[f].rightOffset *= -1.0f; 
			}
			if ( f == THUMB_FINGER ) 
			{ 
				_myRightHand.fingers[f].rightOffset *= -1.0f; 
				_myLeftHand.fingers [f].forwardRotation = -60.0f;
				_myRightHand.fingers[f].forwardRotation =  60.0f;
			}
			if ( f == PINKY_FINGER )
			{
				_myLeftHand.fingers[f].rightOffset *= -1.0f;
			}

			_myLeftHand.fingers [f].upOffset = PALM_WIDTH * 0.4f;
			_myRightHand.fingers[f].upOffset = PALM_WIDTH * 0.4f;

			_myLeftHand.fingers [f].gameObject = GameObject.CreatePrimitive( PrimitiveType.Sphere );
			_myRightHand.fingers[f].gameObject = GameObject.CreatePrimitive( PrimitiveType.Sphere );

	 		_myLeftHand.fingers	[f].gameObject.transform.localScale = new Vector3( FINGER_RADIUS, FINGER_LENGTH, FINGER_RADIUS 	);	
	 		_myRightHand.fingers[f].gameObject.transform.localScale = new Vector3( FINGER_RADIUS, FINGER_LENGTH, FINGER_RADIUS 	);	
		}

	 	_myHead.gameObject.transform.localScale	= new Vector3( HEAD_WIDTH,	HEAD_LENGTH,	HEAD_DEPTH	);	
	 	_myLeftHand.palm.transform.localScale  	= new Vector3( PALM_WIDTH,	PALM_LENGTH, 	PALM_DEPTH	);	
	 	_myRightHand.palm.transform.localScale 	= new Vector3( PALM_WIDTH,	PALM_LENGTH, 	PALM_DEPTH	);
		
		_myLeftHand.isRightHand  = false;
		_myRightHand.isRightHand = true;

		//-----------------------------------------
		// create and intitialize emitter array
		//-----------------------------------------
		_emitters = new ParticleEmitter[ NUM_EMITTERS ];
		for (int e = 0; e < NUM_EMITTERS; e++) 
		{
			_emitters[e] = new ParticleEmitter();
		}

		initializeEmitters();
	}



	//-------------------------------------------
	// GUI...
	//-------------------------------------------
    void OnGUI() 
	{
		if ( GUI.Button( _clearButtonRect, "clear all particles" ) )
		{
            clearAllParticles();
        }
    }

	//-------------------------------------------
	// initialize emitters...
	//-------------------------------------------
	private void initializeEmitters() 
	{
		for (int e=0; e<NUM_EMITTERS; e++)
		{
			_emitters[e].strength 	= 0.05f;
			_emitters[e].rate 		= 0.0f;
			_emitters[e].active		= false;
			_emitters[e].position 	= Vector3.zero;
			_emitters[e].direction 	= Vector3.one;
			_emitters[e].species 	= e;
		}
	}


	//-------------------------------------------
	// clearAllParticles...
	//-------------------------------------------
	private void clearAllParticles()
	{
		_clearRequested = true;
	}




	//--------------
	void Update () 
	{ 
		//---------------------------------------------------------
		// manage display of head and hands...
		//---------------------------------------------------------
		if ( _showHeadAndHands ) 	
				{ if ( ! _displayBody ) { setBodyDisplay( true  ); } } 
		else 	{ if (   _displayBody ) { setBodyDisplay( false ); } }

		//---------------------------------------------------------
		// set the positions of the hands...
		//---------------------------------------------------------
    	Vector3 headRightward	= _myHead.gameObject.transform.right;
    	Vector3 headUpward 		= _myHead.gameObject.transform.up;
    	Vector3 headForward 	= _myHead.gameObject.transform.forward;

		float rightwardAmount = 0.2f;
		float upwardAmount 	  = 0.1f;
		float forwardAmount   = 0.2f;

		_myLeftHand.palm.transform.position  = _myHead.gameObject.transform.position + headRightward * -rightwardAmount + Vector3.up * -upwardAmount + headForward * forwardAmount;
		_myRightHand.palm.transform.position = _myHead.gameObject.transform.position + headRightward *  rightwardAmount + Vector3.up * -upwardAmount + headForward * forwardAmount;

		float waveAroundRange = 0.06f;
		float leftEnvelope  = Mathf.Sin( Time.time * 0.4f );
		_myLeftHand.palm.transform.position  += waveAroundRange * leftEnvelope * Vector3.up    * Mathf.Sin( Time.time * 1.0f );
		_myLeftHand.palm.transform.position  += waveAroundRange * leftEnvelope * Vector3.right * Mathf.Cos( Time.time * 1.5f );
		_myRightHand.palm.transform.position += waveAroundRange * leftEnvelope * Vector3.up    * Mathf.Cos( Time.time * 1.2f );
		_myRightHand.palm.transform.position += waveAroundRange * leftEnvelope * Vector3.right * Mathf.Sin( Time.time * 1.8f );

		//---------------------------------------------------------
		// set the rotations of the hands...
		//---------------------------------------------------------
		_myLeftHand.palm.transform.rotation  = Quaternion.identity;
		_myRightHand.palm.transform.rotation = Quaternion.identity;

		_myLeftHand.palm.transform.Rotate ( 30.0f + 30.0f * Mathf.Sin( Time.time * 1.3f ), -30.0f + 20.0f * Mathf.Sin( Time.time * 3.3f ), 0.0f );
		_myRightHand.palm.transform.Rotate( 30.0f + 30.0f * Mathf.Cos( Time.time * 1.0f ),  30.0f + 20.0f * Mathf.Cos( Time.time * 2.5f ), 0.0f );
	
		//---------------------------------------------------------
		// attach fingers to the hands...
		//---------------------------------------------------------
		for (int f=0; f<NUM_FINGERS_PER_HAND; f++) 
		{
			_myLeftHand.fingers[f].gameObject.transform.position = _myLeftHand.palm.transform.position
			+ _myLeftHand.palm.transform.right * _myLeftHand.fingers[f].rightOffset
			+ _myLeftHand.palm.transform.up	   * _myLeftHand.fingers[f].upOffset;

			_myRightHand.fingers[f].gameObject.transform.position = _myRightHand.palm.transform.position
			+ _myRightHand.palm.transform.right * _myRightHand.fingers[f].rightOffset
			+ _myRightHand.palm.transform.up	* _myRightHand.fingers[f].upOffset;

			_myLeftHand.fingers [f].gameObject.transform.rotation = _myLeftHand.palm.transform.rotation;
			_myRightHand.fingers[f].gameObject.transform.rotation = _myRightHand.palm.transform.rotation;

			_myLeftHand.fingers[f].gameObject.transform.RotateAround ( _myLeftHand.palm.transform.position,  _myLeftHand.palm.transform.forward,  _myLeftHand.fingers [f].forwardRotation );
			_myRightHand.fingers[f].gameObject.transform.RotateAround( _myRightHand.palm.transform.position, _myRightHand.palm.transform.forward, _myRightHand.fingers[f].forwardRotation );
		}

		//---------------------------------------------------------
		// attach emitters to fingers...
		//---------------------------------------------------------
		for (int f=0; f<NUM_FINGERS_PER_HAND; f++) 
		{
			int left  = f;
			int right = f + NUM_FINGERS_PER_HAND;

	    	_emitters[ left  ].position  = _myLeftHand.fingers [f].gameObject.transform.position;
			_emitters[ right ].position  = _myRightHand.fingers[f].gameObject.transform.position;
	    	_emitters[ left  ].direction = _myLeftHand.fingers [f].gameObject.transform.up;
	    	_emitters[ right ].direction = _myRightHand.fingers[f].gameObject.transform.up;
		}

		//---------------------------------------------------------
		// get emission values from the editor...
		//---------------------------------------------------------
		_emitters[0].active = _leftThumbActive;
		_emitters[1].active = _leftIndexActive;
		_emitters[2].active = _leftPinkyActive;
		_emitters[3].active = _rightThumbActive;
		_emitters[4].active = _rightIndexActive;
		_emitters[5].active	= _rightPinkyActive;


		//----------------------------------------------
		// This is a prototype - used for testing 
		// particle collisions with hand capsules...
		//----------------------------------------------
		_handColliderArray = new CapsuleCollisionVolume[ NUM_HAND_COLLIDERS ];

		for (int c=0; c<NUM_HAND_COLLIDERS; c++)
		{
			string gameObjectName = "Capsule" + c.ToString();

			GameObject capsule = GameObject.Find( gameObjectName );
			float capsuleRadius = capsule.transform.localScale.x / 2.0f;
			Vector3 axis = capsule.transform.up * ( capsule.transform.localScale.y - capsuleRadius );

			_handColliderArray[c] = new CapsuleCollisionVolume();
			_handColliderArray[c].p0 	 = capsule.transform.position - axis;
			_handColliderArray[c].p1 	 = capsule.transform.position + axis;
			_handColliderArray[c].radius = capsuleRadius;
		}
	}

	//------------------------------------------------------------------------------------
	// Here are the public get methods...
	//------------------------------------------------------------------------------------
	public Vector3 getHeadPosition	() { return _myHead.gameObject.transform.position; 	}
	public Vector3 getHeadRightward () { return _myHead.gameObject.transform.right; 	}
	public Vector3 getHeadUpward 	() { return _myHead.gameObject.transform.up; 		}
	public Vector3 getHeadForward 	() { return _myHead.gameObject.transform.forward; 	}

	public bool 	getEmitterActive	( int e ) { return _emitters[e].active;		}
	public int 		getEmitterSpecies	( int e ) { return _emitters[e].species;	}
	public Vector3	getEmitterPosition	( int e ) { return _emitters[e].position;	}
	public Vector3	getEmitterDirection	( int e ) { return _emitters[e].direction;	}
	public float	getEmitterStrength	( int e ) { return _emitters[e].strength;	}
	public float	getEmitterRate		( int e ) { return _emitters[e].rate;		}
	
	public int 		getNumHandColliders() { return NUM_HAND_COLLIDERS; }
	public Vector3 	getHandColliderP0		( int c ) { return _handColliderArray[c].p0; }
	public Vector3 	getHandColliderP1		( int c ) { return _handColliderArray[c].p1; }
	public float 	getHandColliderRadius	( int c ) { return _handColliderArray[c].radius; }

	//---------------------------------------------
	// turn on or off the display of the body...
	//---------------------------------------------
	private void setBodyDisplay( bool display )
	{
		_displayBody = display;

		_myHead.GetComponentInChildren<MeshRenderer>().enabled  = display;

		_myLeftHand.palm.GetComponentInChildren<MeshRenderer>().enabled  = display;
		_myRightHand.palm.GetComponentInChildren<MeshRenderer>().enabled = display;

		for (int f=0; f<NUM_FINGERS_PER_HAND; f++) 
		{
			_myLeftHand.fingers [f].gameObject.GetComponentInChildren<MeshRenderer>().enabled = display;
			_myRightHand.fingers[f].gameObject.GetComponentInChildren<MeshRenderer>().enabled = display;
		}		
	}

	//------------------------------
	// clear requested...
	//------------------------------
	public bool getClearRequest() 
	{ 
		if ( _clearRequested )
		{
			_clearRequested = false;
			return true;
		}
		return false;
	}

	public int getNumEmitters() { return NUM_EMITTERS; }
}