using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleControl : MonoBehaviour {

	//---------------------------------------
	// head and hand metrics
	//---------------------------------------
	private const float HEAD_LENGTH		= 0.13f;
	private const float HEAD_WIDTH		= 0.10f;
	private const float HEAD_DEPTH		= 0.10f;
	private const float PALM_LENGTH		= 0.07f;
	private const float PALM_WIDTH		= 0.07f;
	private const float PALM_DEPTH		= 0.03f;
	private const float FINGER_LENGTH	= 0.08f;
	private const float FINGER_RADIUS	= 0.02f;

	//---------------------------------------
	// finger indices
	//---------------------------------------
	private const int LEFT_THUMB_FINGER 	= 0;
	private const int LEFT_INDEX_FINGER 	= 1;
	private const int LEFT_PINKY_FINGER 	= 2;
	private const int RIGHT_THUMB_FINGER	= 3;
	private const int RIGHT_INDEX_FINGER	= 4;
	private const int RIGHT_PINKY_FINGER	= 5;
	private const int NUM_FINGERS			= 6;

  	private struct ParticleEmitter 
	{
		public  bool	active;
		public	Vector3 position;
		public	Vector3 direction;
		public	float   strength;
		public  float 	rate;
		public	int		species;
  	}

  	private struct Head 
	{
		public GameObject gameObject;
		public bool movingForward;
		public bool turning;
		public float turnAmount;
  	}

 	private struct Hand 
	{
		public GameObject palm;
		public GameObject thumb;
		public GameObject index;
		public GameObject pinky;
		public bool isRightHand;
  	}

	public  Camera	_camera;
	private Head 	_myHead;
	private Hand 	_myLeftHand;
	private Hand 	_myRightHand;
	private ParticleEmitter[] _emitters;

	//---------------------------------------
	void Start() 
	{
		_myHead.gameObject	= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myLeftHand.palm 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myRightHand.palm 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myLeftHand.thumb 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myRightHand.thumb 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myLeftHand.index 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myRightHand.index 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myLeftHand.pinky 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myRightHand.pinky 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );

		float pinkyRadius = FINGER_RADIUS * 0.8f;
		float thumbLength = FINGER_LENGTH * 0.8f;

	 	_myHead.gameObject.transform.localScale	= new Vector3( HEAD_WIDTH, 		HEAD_LENGTH, 	HEAD_DEPTH 		);	
	 	_myLeftHand.palm.transform.localScale  	= new Vector3( PALM_WIDTH, 		PALM_LENGTH, 	PALM_DEPTH 		);	
	 	_myRightHand.palm.transform.localScale 	= new Vector3( PALM_WIDTH, 		PALM_LENGTH, 	PALM_DEPTH 		);
	 	_myLeftHand.thumb.transform.localScale  = new Vector3( FINGER_RADIUS, 	thumbLength, 	FINGER_RADIUS 	);	
	 	_myRightHand.thumb.transform.localScale = new Vector3( FINGER_RADIUS, 	thumbLength, 	FINGER_RADIUS 	);
	 	_myLeftHand.index.transform.localScale  = new Vector3( FINGER_RADIUS, 	FINGER_LENGTH, 	FINGER_RADIUS 	);	
	 	_myRightHand.index.transform.localScale = new Vector3( FINGER_RADIUS, 	FINGER_LENGTH, 	FINGER_RADIUS 	);
	 	_myLeftHand.pinky.transform.localScale  = new Vector3( pinkyRadius, 	FINGER_LENGTH, 	pinkyRadius 	);	
	 	_myRightHand.pinky.transform.localScale = new Vector3( pinkyRadius, 	FINGER_LENGTH, 	pinkyRadius 	);

		_myHead.movingForward = false;
		_myHead.turning = false;
		_myHead.turnAmount = 0.0f;
		
		_myLeftHand.isRightHand  = false;
		_myRightHand.isRightHand = true;

		//-----------------------------------------
		// create and intitialize emitter array
		//-----------------------------------------
		_emitters = new ParticleEmitter[ NUM_FINGERS ];
		for (int e = 0; e < NUM_FINGERS; e++) 
		{
			_emitters[e] = new ParticleEmitter();
		}

		initializeEmitters();
	}


	//-------------------------------------------
	// initialize emitters...
	//-------------------------------------------
	private void initializeEmitters() 
	{
		for (int e=0; e<NUM_FINGERS; e++)
		{
			_emitters[e].strength 	= 0.05f;
			_emitters[e].rate 		= 0.12f;
			_emitters[e].active		= false;
			_emitters[e].position 	= Vector3.zero;
			_emitters[e].direction 	= Vector3.one;
			_emitters[e].species 	= e+1;
		}
	}



	//--------------
	void Update () 
	{
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
		float rightEnvelope = Mathf.Cos( Time.time * 1.0f );
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
		// set the finger emitters...
		//---------------------------------------------------------
		_myLeftHand.index.transform.position = _myLeftHand.palm.transform.position 
		+ _myLeftHand.palm.transform.right   * PALM_WIDTH *  0.3f
		+ _myLeftHand.palm.transform.up 	 * PALM_WIDTH *  0.4f;
		
		_myRightHand.index.transform.position = _myRightHand.palm.transform.position 
		+ _myRightHand.palm.transform.right   * PALM_WIDTH * -0.3f
		+ _myRightHand.palm.transform.up 	  * PALM_WIDTH *  0.4f;



		_myLeftHand.thumb.transform.position = _myLeftHand.palm.transform.position 
		+ _myLeftHand.palm.transform.right   * PALM_WIDTH *  0.3f
		+ _myLeftHand.palm.transform.up 	 * PALM_WIDTH *  0.4f;
		
		_myRightHand.thumb.transform.position = _myRightHand.palm.transform.position 
		+ _myRightHand.palm.transform.right   * PALM_WIDTH * -0.3f
		+ _myRightHand.palm.transform.up 	  * PALM_WIDTH *  0.4f;

		_myLeftHand.thumb.transform.rotation  = _myLeftHand.palm.transform.rotation;
		_myRightHand.thumb.transform.rotation = _myRightHand.palm.transform.rotation;

		_myLeftHand.thumb.transform.RotateAround ( _myLeftHand.palm.transform.position,  _myLeftHand.palm.transform.forward, -60.0f );
		_myRightHand.thumb.transform.RotateAround( _myRightHand.palm.transform.position, _myRightHand.palm.transform.forward, 60.0f );




		_myLeftHand.pinky.transform.position = _myLeftHand.palm.transform.position 
		+ _myLeftHand.palm.transform.right   * PALM_WIDTH * -0.3f
		+ _myLeftHand.palm.transform.up 	 * PALM_WIDTH *  0.4f;
		
		_myRightHand.pinky.transform.position = _myRightHand.palm.transform.position 
		+ _myRightHand.palm.transform.right   * PALM_WIDTH *  0.3f
		+ _myRightHand.palm.transform.up 	  * PALM_WIDTH *  0.4f;

		_myLeftHand.index.transform.rotation  = _myLeftHand.palm.transform.rotation;
		_myRightHand.index.transform.rotation = _myRightHand.palm.transform.rotation;
		_myLeftHand.pinky.transform.rotation  = _myLeftHand.palm.transform.rotation;
		_myRightHand.pinky.transform.rotation = _myRightHand.palm.transform.rotation;

    	_emitters[ LEFT_THUMB_FINGER 	].position	= _myLeftHand.thumb.transform.position;
    	_emitters[ LEFT_INDEX_FINGER 	].position	= _myLeftHand.index.transform.position;
    	_emitters[ LEFT_PINKY_FINGER 	].position	= _myLeftHand.pinky.transform.position;
    	_emitters[ RIGHT_THUMB_FINGER 	].position	= _myRightHand.thumb.transform.position;
    	_emitters[ RIGHT_INDEX_FINGER 	].position	= _myRightHand.index.transform.position;
    	_emitters[ RIGHT_PINKY_FINGER 	].position	= _myRightHand.pinky.transform.position;

    	_emitters[ LEFT_THUMB_FINGER 	].direction	= _myLeftHand.thumb.transform.up;
    	_emitters[ LEFT_INDEX_FINGER 	].direction	= _myLeftHand.index.transform.up;
    	_emitters[ LEFT_PINKY_FINGER 	].direction	= _myLeftHand.pinky.transform.up;
    	_emitters[ RIGHT_THUMB_FINGER 	].direction	= _myRightHand.thumb.transform.up;
    	_emitters[ RIGHT_INDEX_FINGER 	].direction	= _myRightHand.index.transform.up;
    	_emitters[ RIGHT_PINKY_FINGER 	].direction	= _myRightHand.pinky.transform.up;

		//---------------------------------------------------------
		// turning finger emitters of and off...
		//---------------------------------------------------------
		float chance = 0.95f;

		for (int e=0; e<NUM_FINGERS; e++)
		{
			if ( Random.value > chance ) { _emitters[e].active = true;  }
			if ( Random.value > chance ) { _emitters[e].active = false; }
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

	public int getNumEmitters() { return NUM_FINGERS; }
}
