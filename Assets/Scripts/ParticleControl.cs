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
	private const float HAND_WIDTH	= 0.05f;
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
		public	Vector3 position;
		public	Vector3 direction;
		public	float   strength;
		public	float   jitter;
		public  float 	rate;
		public	int		species;
  	}

  	private struct Head 
	{
		public GameObject 	gameObject;
		public Quaternion 	rotation;
  	}

	private Head 				_myHead;
	private GameObject			_myLeftHand;
	private GameObject			_myRightHand;
	private Camera				_camera;
	private ParticleEmitter[] 	_emitters;

	//---------------------------------------
	void Start() 
	{
		_camera = GetComponent<Camera>();

		_myHead.gameObject = GameObject.CreatePrimitive( PrimitiveType.Sphere );
	 	_myHead.gameObject.transform.localScale = new Vector3( ParticleControl.HEAD_WIDTH, ParticleControl.HEAD_LENGTH, ParticleControl.HEAD_DEPTH );	

		_myLeftHand = GameObject.CreatePrimitive( PrimitiveType.Sphere );
	 	_myLeftHand.transform.localScale = new Vector3( ParticleControl.HAND_WIDTH, ParticleControl.HAND_LENGTH, ParticleControl.HAND_DEPTH );	

		_myRightHand = GameObject.CreatePrimitive( PrimitiveType.Sphere );
	 	_myRightHand.transform.localScale = new Vector3( ParticleControl.HAND_WIDTH, ParticleControl.HAND_LENGTH, ParticleControl.HAND_DEPTH );

		//-----------------------------------------
		// intitialize emitter array
		//-----------------------------------------
		_emitters = new ParticleEmitter[ NUM_FINGERS ];
		for (int e = 0; e < NUM_FINGERS; e++) 
		{
			_emitters[e] = new ParticleEmitter();
			_emitters[e].position 	= Vector3.zero;
			_emitters[e].direction 	= Vector3.one;
			_emitters[e].species   	= 0;
			_emitters[e].rate		= 0.0f;
			_emitters[e].strength 	= 0.0f;
			_emitters[e].jitter 	= 0.0f;
		}

		initializeEmitters();
	}


	//-------------------------------------------
	// initialize emitters...
	//-------------------------------------------
	private void initializeEmitters() 
	{
		for (int e=0; e<ParticleControl.NUM_FINGERS; e++)
		{
			_emitters[e].strength = 0.05f;
			_emitters[e].rate = 0.1f;
		}

		_emitters[ ParticleControl.LEFT_THUMB_FINGER	].species = 0;
		_emitters[ ParticleControl.LEFT_INDEX_FINGER 	].species = 1;
		_emitters[ ParticleControl.LEFT_MIDDLE_FINGER 	].species = 2;
		_emitters[ ParticleControl.LEFT_RING_FINGER 	].species = 3;
		_emitters[ ParticleControl.LEFT_PINKY_FINGER 	].species = 4;
		_emitters[ ParticleControl.RIGHT_THUMB_FINGER	].species = 0;
		_emitters[ ParticleControl.RIGHT_INDEX_FINGER 	].species = 1;
		_emitters[ ParticleControl.RIGHT_MIDDLE_FINGER 	].species = 2;
		_emitters[ ParticleControl.RIGHT_RING_FINGER	].species = 3;
		_emitters[ ParticleControl.RIGHT_PINKY_FINGER	].species = 4;
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

		_myLeftHand.transform.position  = _myHead.gameObject.transform.position + headRightward * -rightwardAmount + Vector3.up * -upwardAmount + headForward * forwardAmount;
		_myRightHand.transform.position = _myHead.gameObject.transform.position + headRightward *  rightwardAmount + Vector3.up * -upwardAmount + headForward * forwardAmount;

		float waveAroundRange = 0.06f;
		float leftEnvelope  = Mathf.Sin( Time.time * 0.4f );
		float rightEnvelope = Mathf.Cos( Time.time * 1.0f );
		_myLeftHand.transform.position  += waveAroundRange * leftEnvelope * Vector3.up    * Mathf.Sin( Time.time * 1.0f );
		_myLeftHand.transform.position  += waveAroundRange * leftEnvelope * Vector3.right * Mathf.Cos( Time.time * 1.5f );

		_myRightHand.transform.position += waveAroundRange * leftEnvelope * Vector3.up    * Mathf.Cos( Time.time * 1.2f );
		_myRightHand.transform.position += waveAroundRange * leftEnvelope * Vector3.right * Mathf.Sin( Time.time * 1.8f );

		//---------------------------------------------------------
		// set the rotations of the hands...
		//---------------------------------------------------------
		_myLeftHand.transform.rotation  = Quaternion.identity;
		_myRightHand.transform.rotation = Quaternion.identity;

		_myLeftHand.transform.Rotate ( 60.0f + 30.0f * Mathf.Sin( Time.time * 1.3f ), -30.0f + 20.0f * Mathf.Sin( Time.time * 3.3f ), 0.0f );
		_myRightHand.transform.Rotate( 60.0f + 30.0f * Mathf.Cos( Time.time * 1.0f ),  30.0f + 20.0f * Mathf.Cos( Time.time * 2.5f ), 0.0f );
	
		//---------------------------------------------------------
		// set the positions of the finger emitters...
		// NOT IMPLEMENTED YET
		//---------------------------------------------------------
   		_emitters[ ParticleControl.LEFT_THUMB_FINGER 	].position	= _myLeftHand.transform.position;
    	_emitters[ ParticleControl.LEFT_INDEX_FINGER 	].position	= _myLeftHand.transform.position;
    	_emitters[ ParticleControl.LEFT_MIDDLE_FINGER 	].position	= _myLeftHand.transform.position;
    	_emitters[ ParticleControl.LEFT_RING_FINGER 	].position	= _myLeftHand.transform.position;
    	_emitters[ ParticleControl.LEFT_PINKY_FINGER 	].position 	= _myLeftHand.transform.position;
   		_emitters[ ParticleControl.RIGHT_THUMB_FINGER 	].position	= _myRightHand.transform.position;
    	_emitters[ ParticleControl.RIGHT_INDEX_FINGER 	].position	= _myRightHand.transform.position;
    	_emitters[ ParticleControl.RIGHT_MIDDLE_FINGER 	].position	= _myRightHand.transform.position;
    	_emitters[ ParticleControl.RIGHT_RING_FINGER 	].position	= _myRightHand.transform.position;
    	_emitters[ ParticleControl.RIGHT_PINKY_FINGER 	].position 	= _myRightHand.transform.position;

		//---------------------------------------------------------
		// set the directions of the finger emitters ...
		// NOT IMPLEMENTED YET
		//---------------------------------------------------------
   		_emitters[ ParticleControl.LEFT_THUMB_FINGER 	].direction	= _myLeftHand.transform.up;
    	_emitters[ ParticleControl.LEFT_INDEX_FINGER 	].direction	= _myLeftHand.transform.up;
    	_emitters[ ParticleControl.LEFT_MIDDLE_FINGER 	].direction	= _myLeftHand.transform.up;
    	_emitters[ ParticleControl.LEFT_RING_FINGER 	].direction	= _myLeftHand.transform.up;
    	_emitters[ ParticleControl.LEFT_PINKY_FINGER 	].direction = _myLeftHand.transform.up;
   		_emitters[ ParticleControl.RIGHT_THUMB_FINGER 	].direction	= _myRightHand.transform.up;
    	_emitters[ ParticleControl.RIGHT_INDEX_FINGER 	].direction	= _myRightHand.transform.up;
    	_emitters[ ParticleControl.RIGHT_MIDDLE_FINGER 	].direction	= _myRightHand.transform.up;
    	_emitters[ ParticleControl.RIGHT_RING_FINGER 	].direction	= _myRightHand.transform.up;
    	_emitters[ ParticleControl.RIGHT_PINKY_FINGER 	].direction = _myRightHand.transform.up;
	}

	//----------------------------------------------------------------------------------------
	// Here are the public get methods...
	//----------------------------------------------------------------------------------------
	public Vector3	getHeadPosition		() { return _myHead.gameObject.transform.position; 	}
	public Vector3	getHeadRightward 	() { return _myHead.gameObject.transform.right; 	}
	public Vector3	getHeadUpward 		() { return _myHead.gameObject.transform.up; 		}
	public Vector3	getHeadForward 		() { return _myHead.gameObject.transform.forward; 	}

	public int 		getEmitterSpecies	( int e ) { return _emitters[e].species;	}
	public float 	getEmitterJitter	( int e ) { return _emitters[e].jitter;		}
	public Vector3	getEmitterPosition	( int e ) { return _emitters[e].position;	}
	public Vector3	getEmitterDirection	( int e ) { return _emitters[e].direction;	}
	public float	getEmitterStrength	( int e ) { return _emitters[e].strength;	}
	public float	getEmitterRate		( int e ) { return _emitters[e].rate;		}

	public int getNumEmitters() { return NUM_FINGERS; }
}
