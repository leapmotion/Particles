using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleControl : MonoBehaviour {

	//---------------------------------------
	// head and hand metrics
	//---------------------------------------
	private const float HEAD_LENGTH	= 0.15f;
	private const float HEAD_WIDTH	= 0.12f;
	private const float HEAD_DEPTH	= 0.12f;
	private const float HAND_LENGTH	= 0.1f;
	private const float HAND_WIDTH	= 0.02f;
	private const float HAND_DEPTH	= 0.02f;

	//---------------------------------------
	// finger indices
	//---------------------------------------
	private const int LEFT_THUMB_FINGER		= 0;
	private const int LEFT_INDEX_FINGER 	= 1;
	private const int LEFT_MIDDLE_FINGER 	= 2;
	private const int LEFT_RING_FINGER 		= 3;
	private const int LEFT_PINKY_FINGER 	= 4;
	private const int RIGHT_THUMB_FINGER	= 5;
	private const int RIGHT_INDEX_FINGER 	= 6;
	private const int RIGHT_MIDDLE_FINGER 	= 7;
	private const int RIGHT_RING_FINGER 	= 8;
	private const int RIGHT_PINKY_FINGER 	= 9;
	private const int NUM_FINGERS			= 10;

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
  	}

 	private struct Hand 
	{
		public GameObject gameObject;
		public bool isRightHand;
  	}

	private Head _myHead;
	private Hand _myLeftHand;
	private Hand _myRightHand;
	private Camera _camera;
	private ParticleEmitter[] _emitters;

	//---------------------------------------
	void Start() 
	{
		_camera = GetComponent<Camera>();

		_myHead.gameObject 		= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myLeftHand.gameObject 	= GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_myRightHand.gameObject = GameObject.CreatePrimitive( PrimitiveType.Sphere );

	 	_myHead.gameObject.transform.localScale 	 = new Vector3( HEAD_WIDTH, HEAD_LENGTH, HEAD_DEPTH );	
	 	_myLeftHand.gameObject.transform.localScale  = new Vector3( HAND_WIDTH, HAND_LENGTH, HAND_DEPTH );	
	 	_myRightHand.gameObject.transform.localScale = new Vector3( HAND_WIDTH, HAND_LENGTH, HAND_DEPTH );

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
		}

		_emitters[ LEFT_THUMB_FINGER	].species = 0;
		_emitters[ LEFT_INDEX_FINGER 	].species = 1;
		_emitters[ LEFT_MIDDLE_FINGER 	].species = 2;
		_emitters[ LEFT_RING_FINGER 	].species = 3;
		_emitters[ LEFT_PINKY_FINGER 	].species = 4;
		_emitters[ RIGHT_THUMB_FINGER	].species = 5;
		_emitters[ RIGHT_INDEX_FINGER 	].species = 6;
		_emitters[ RIGHT_MIDDLE_FINGER 	].species = 7;
		_emitters[ RIGHT_RING_FINGER	].species = 8;
		_emitters[ RIGHT_PINKY_FINGER	].species = 9;
	}



	//--------------
	void Update () 
	{
		//--------------------------------------------------------------------------
		// update the positions, rotations, etc. of the head, hands, and fingers...
		//--------------------------------------------------------------------------
		_myHead.gameObject.transform.position = _camera.transform.position;
		_myHead.gameObject.transform.rotation = _camera.transform.rotation; 

		// I will astral project to a locatin behind my camera so I can see my own head!
		_myHead.gameObject.transform.position += _camera.transform.forward * 0.6f + Vector3.up * -0.2f;

    	Vector3 headRightward 	= _myHead.gameObject.transform.right;
    	Vector3 headUpward 		= _myHead.gameObject.transform.up;
    	Vector3 headForward 	= _myHead.gameObject.transform.forward;

		//---------------------------------------------------------
		// set the positions of the hands...
		//---------------------------------------------------------
		float rightwardAmount = 0.2f;
		float upwardAmount 	  = 0.1f;
		float forwardAmount   = 0.2f;

		_myLeftHand.gameObject.transform.position  = _myHead.gameObject.transform.position + headRightward * -rightwardAmount + Vector3.up * -upwardAmount + headForward * forwardAmount;
		_myRightHand.gameObject.transform.position = _myHead.gameObject.transform.position + headRightward *  rightwardAmount + Vector3.up * -upwardAmount + headForward * forwardAmount;

		float waveAroundRange = 0.06f;
		float leftEnvelope  = Mathf.Sin( Time.time * 0.4f );
		float rightEnvelope = Mathf.Cos( Time.time * 1.0f );
		_myLeftHand.gameObject.transform.position  += waveAroundRange * leftEnvelope * Vector3.up    * Mathf.Sin( Time.time * 1.0f );
		_myLeftHand.gameObject.transform.position  += waveAroundRange * leftEnvelope * Vector3.right * Mathf.Cos( Time.time * 1.5f );

		_myRightHand.gameObject.transform.position += waveAroundRange * leftEnvelope * Vector3.up    * Mathf.Cos( Time.time * 1.2f );
		_myRightHand.gameObject.transform.position += waveAroundRange * leftEnvelope * Vector3.right * Mathf.Sin( Time.time * 1.8f );

		//---------------------------------------------------------
		// set the rotations of the hands...
		//---------------------------------------------------------
		_myLeftHand.gameObject.transform.rotation  = Quaternion.identity;
		_myRightHand.gameObject.transform.rotation = Quaternion.identity;

		_myLeftHand.gameObject.transform.Rotate ( 60.0f + 30.0f * Mathf.Sin( Time.time * 1.3f ), -30.0f + 20.0f * Mathf.Sin( Time.time * 3.3f ), 0.0f );
		_myRightHand.gameObject.transform.Rotate( 60.0f + 30.0f * Mathf.Cos( Time.time * 1.0f ),  30.0f + 20.0f * Mathf.Cos( Time.time * 2.5f ), 0.0f );
	
		//---------------------------------------------------------
		// set the finger emitters...
		// NOT IMPLEMENTED YET
		//---------------------------------------------------------
   		_emitters[ LEFT_THUMB_FINGER 	].position	= _myLeftHand.gameObject.transform.position;
    	_emitters[ LEFT_INDEX_FINGER 	].position	= _myLeftHand.gameObject.transform.position;
    	_emitters[ LEFT_MIDDLE_FINGER 	].position	= _myLeftHand.gameObject.transform.position;
    	_emitters[ LEFT_RING_FINGER 	].position	= _myLeftHand.gameObject.transform.position;
    	_emitters[ LEFT_PINKY_FINGER 	].position 	= _myLeftHand.gameObject.transform.position;
   		_emitters[ RIGHT_THUMB_FINGER 	].position	= _myRightHand.gameObject.transform.position;
    	_emitters[ RIGHT_INDEX_FINGER 	].position	= _myRightHand.gameObject.transform.position;
    	_emitters[ RIGHT_MIDDLE_FINGER 	].position	= _myRightHand.gameObject.transform.position;
    	_emitters[ RIGHT_RING_FINGER 	].position	= _myRightHand.gameObject.transform.position;
    	_emitters[ RIGHT_PINKY_FINGER 	].position 	= _myRightHand.gameObject.transform.position;

   		_emitters[ LEFT_THUMB_FINGER 	].direction	= _myLeftHand.gameObject.transform.up;
    	_emitters[ LEFT_INDEX_FINGER 	].direction	= _myLeftHand.gameObject.transform.up;
    	_emitters[ LEFT_MIDDLE_FINGER 	].direction	= _myLeftHand.gameObject.transform.up;
    	_emitters[ LEFT_RING_FINGER 	].direction	= _myLeftHand.gameObject.transform.up;
    	_emitters[ LEFT_PINKY_FINGER 	].direction = _myLeftHand.gameObject.transform.up;
   		_emitters[ RIGHT_THUMB_FINGER 	].direction	= _myRightHand.gameObject.transform.up;
    	_emitters[ RIGHT_INDEX_FINGER 	].direction	= _myRightHand.gameObject.transform.up;
    	_emitters[ RIGHT_MIDDLE_FINGER 	].direction	= _myRightHand.gameObject.transform.up;
    	_emitters[ RIGHT_RING_FINGER 	].direction	= _myRightHand.gameObject.transform.up;
    	_emitters[ RIGHT_PINKY_FINGER 	].direction = _myRightHand.gameObject.transform.up;

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
