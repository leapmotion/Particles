using Leap.Unity;
using UnityEngine;
using System.Collections;

public class StickyClay : MonoBehaviour 
{
	//--------------------------------------------------
	// constants
	//--------------------------------------------------
	private const float ZERO	 			= 0.0f;
	private const float ONE_HALF 			= 0.5f;
	private const float ONE					= 1.0f;
	private const int	NULL				= -1;
	private const int	INIT_RES			= 10;
	private const int	NUM_PARTICLES		= INIT_RES * INIT_RES * INIT_RES;
	private const int	NUM_NEIGHBORS		= 16;
	private const int	TEST_CHUNK_SIZE		= 100;
	private const int	DEBUG_PARTICLE		= 10;

	struct Particle
	{
		public GameObject 	gameObject;
		public Vector3    	position;
		public Vector3    	velocity;
		public Vector3		force;
		public int[]		neighbors;
		public int			testParticle;
		public float 		farthestNeighborDistance;
		public int  		farthestNeighborIndex;
	} 

	struct ClayProperties
	{
		public float particleRadius;
		public float airFriction;
		public float gravity;
		public float collisionRadius;
		public float attractionRadius;
		public float collisionForce;
		public float collisionFriction;
		public float attractionForce;
		public float sculptingForce;
		public float sculptingFriction;
		public float floorBounce;
	} 

	struct Sculpting
	{
		public Vector3 position;
		public Vector3 velocity;
		public Vector3 force;
	} 

	private Particle[] 		_particles = new Particle[ NUM_PARTICLES ];
	private ClayProperties 	_clayProperties;
	private Sculpting 		_sculpting;


	public Vector3 rightIndexTipPos = Vector3.zero;
	public Vector3 leftIndexTipPos  = Vector3.zero;

	public Quaternion indexTipRot = Quaternion.identity;

	public GameObject sculptingTool;



	//------------------------------------------
	// Start
	//------------------------------------------
	void Start() 
	{
		_clayProperties.particleRadius			= 0.02f;
		_clayProperties.collisionRadius			= 0.02f;
		_clayProperties.attractionRadius		= 0.07f;
		_clayProperties.attractionForce			= 0.0005f;
		_clayProperties.collisionForce			= 0.003f;
		_clayProperties.collisionFriction		= 0.07f;
		_clayProperties.airFriction				= 0.01f;
		_clayProperties.gravity					= 0.0f;
		_clayProperties.sculptingForce			= 0.1f;
		_clayProperties.sculptingFriction		= 0.5f;
		_clayProperties.floorBounce				= 0.2f;

		_sculpting.position = Vector3.zero;
		_sculpting.velocity = Vector3.zero;
		_sculpting.force 	= Vector3.zero;

		//------------------------------------------
		// initialize particles
		//------------------------------------------
		for (int p=0; p<NUM_PARTICLES; p++)
		{
			_particles[p] = new Particle();
			_particles[p].position		= Vector3.zero;
			_particles[p].velocity		= Vector3.zero;
			_particles[p].force			= Vector3.zero;
			_particles[p].testParticle	= (int)( NUM_PARTICLES * Random.value );
			_particles[p].farthestNeighborIndex		= NULL;
			_particles[p].farthestNeighborDistance	= ZERO;
		}

		//-----------------------
		// create initial blob
		//-----------------------
		initializeParticleBlob();

		//-------------------------------------
		// set up the particle game objects
		//-------------------------------------
		createGameObjects();




			//_particles[p].gameObject = GameObject.CreatePrimitive( PrimitiveType.Sphere );
			sculptingTool = GameObject.CreatePrimitive( PrimitiveType.Sphere );

			Material photoMaterial = (Material)Resources.Load( "Whatever" );
			Texture2D  photoTexture = Resources.Load( "clay_particle_for_quad" ) as Texture2D;
			sculptingTool.GetComponent<MeshRenderer>().material = photoMaterial;
			sculptingTool.GetComponent<Renderer>().material.mainTexture = photoTexture;
			sculptingTool.transform.localScale = new Vector3( 0.1f, 0.1f, 0.1f );			
			//sculptingTool.transform.position = _particles[p].position;


		//------------------------------------------
		// initialize neighbors
		//------------------------------------------
		for (int p=0; p<NUM_PARTICLES; p++)
		{
			//------------------------------------------------------------------
			// initialize test particle - which will increment and cycle 
			//------------------------------------------------------------------
			_particles[p].testParticle = (int)( NUM_PARTICLES * Random.value );
			_particles[p].neighbors = new int[ NUM_NEIGHBORS ];
			for (int n=0; n<NUM_NEIGHBORS; n++)
			{
				_particles[p].neighbors[n] = ( p + 1 + n ) % NUM_PARTICLES;
			}
		}
	}
	


	//---------------------------------
	// initialize particle blob 
	//---------------------------------
	void initializeParticleBlob() 
	{
		float blobRadius = _clayProperties.particleRadius * INIT_RES;

		//----------------------------------------
		// set position of particles within blob
		//----------------------------------------
		int p = 0;
		for (int i=0; i<INIT_RES; i++)
		{
			for (int j=0; j<INIT_RES; j++)
			{
				for (int k=0; k<INIT_RES; k++)
				{
					float x = -blobRadius * ONE_HALF + ( (float)i / (float)( INIT_RES - 1 ) ) * blobRadius;
					float y = -blobRadius * ONE_HALF + ( (float)j / (float)( INIT_RES - 1 ) ) * blobRadius;
					float z = -blobRadius * ONE_HALF + ( (float)k / (float)( INIT_RES - 1 ) ) * blobRadius;

					_particles[p].position = new Vector3( x, y, z ) + Vector3.up * ( blobRadius * ONE_HALF + _clayProperties.particleRadius );
					p++;				
				}
			}
		}
	}


	//-----------------------
	// create game objects
	//-----------------------
	void createGameObjects() 
	{
		for (int p=0; p<NUM_PARTICLES; p++)
		{
			//_particles[p].gameObject = GameObject.CreatePrimitive( PrimitiveType.Sphere );
			_particles[p].gameObject = GameObject.CreatePrimitive( PrimitiveType.Quad );

			Material photoMaterial = (Material)Resources.Load( "Whatever" );
			Texture2D  photoTexture = Resources.Load( "clay_particle_for_quad" ) as Texture2D;
			_particles[p].gameObject.GetComponent<MeshRenderer>().material = photoMaterial;
			_particles[p].gameObject.GetComponent<Renderer>().material.mainTexture = photoTexture;
			_particles[p].gameObject.transform.localScale = new Vector3( _clayProperties.particleRadius, _clayProperties.particleRadius, _clayProperties.particleRadius );			
			_particles[p].gameObject.transform.position = _particles[p].position;
		}
	}


	//------------------------------------------
	// Update
	//------------------------------------------
	void Update() 
	{

		var rightHand = Hands.Right;
		var leftHand  = Hands.Left;

		if ( leftHand != null ) 
		{
			leftIndexTipPos = leftHand.Fingers[1].TipPosition.ToVector3();
			//indexTipRot = leftHand.Fingers[1].bones[3].Rotation.ToQuaternion();
			var boneWidth = leftHand.Fingers[1].bones[3].Width;
		}

		if (rightHand != null) 
		{
			rightIndexTipPos = rightHand.Fingers[1].TipPosition.ToVector3();
			//indexTipRot = rightHand.Fingers[1].bones[3].Rotation.ToQuaternion();
			var boneWidth = rightHand.Fingers[1].bones[3].Width;
		}


sculptingTool.transform.position = leftIndexTipPos;

		//----------------------------------
		// clear all forces
		//----------------------------------
		for (int p=0; p<NUM_PARTICLES; p++)
		{
			_particles[p].force = Vector3.zero;
		}

		//----------------------------------
		// update
		//----------------------------------
		for (int p=0; p<NUM_PARTICLES; p++)
		{
			//----------------------------------
			// gather forces
			//----------------------------------
			gatherForces(p);

			//----------------------------------------------
			// apply forces to velocity
			//----------------------------------------------
			_particles[p].velocity += _particles[p].force;

			//----------------------------------
			// update position by velocity
			//----------------------------------
			_particles[p].position += _particles[p].velocity;
			_particles[p].gameObject.transform.position = _particles[p].position;
	
			//-------------------------
			// update neighbors
			//-------------------------
			updateNeighbors(p);
		}

		/*
		//----------------------------------
		// show debug colors...
		//----------------------------------
		for (int p=0; p<NUM_PARTICLES; p++)
		{
			_particles[p].gameObject.GetComponent<Renderer> ().material.color = new Color( 0.0f, 0.0f, 0.0f );
		}

		_particles[ DEBUG_PARTICLE ].gameObject.GetComponent<Renderer> ().material.color = new Color( 1.0f, 0.0f, 0.0f );

		for (int n=0; n<NUM_NEIGHBORS; n++)
		{
			_particles[ _particles[ DEBUG_PARTICLE ].neighbors[n] ].gameObject.GetComponent<Renderer> ().material.color = new Color( 0.0f, 1.0f, 0.0f );
		}

		int farthestNeighbor = _particles[ DEBUG_PARTICLE ].neighbors[ _particles[ DEBUG_PARTICLE ].farthestNeighborIndex ];

		if ( farthestNeighbor != NULL )
		{
			_particles[ farthestNeighbor ].gameObject.GetComponent<Renderer> ().material.color = new Color( 1.0f, 1.0f, 0.0f );
		}
		*/
	}


	void OnDrawGizmos() 
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere( rightIndexTipPos, 0.05f);

		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere( leftIndexTipPos, 0.05f);
	}


	//--------------------------
	// gather forces
	//--------------------------
	void gatherForces( int p ) 
	{
		//----------------------------------
		// air friction
		//----------------------------------
		_particles[p].force -= _particles[p].velocity * _clayProperties.airFriction;

		//----------------------------------
		// gravity
		//----------------------------------
		_particles[p].force += _clayProperties.gravity * Vector3.down;

		//-------------------------------------------------------
		// gather forces from interacting with other particles
		//-------------------------------------------------------
		gatherInteractionForces(p);

		//----------------------------------
		// floor collisions
		//----------------------------------
		if ( _particles[p].position.y < _clayProperties.particleRadius )
		{
			float upwardForce = _clayProperties.particleRadius - _particles[p].position.y;
//_particles[p].force += upwardForce * Vector3.up * _clayProperties.floorBounce;


//_particles[p].force -= _particles[p].velocity * 0.2f;

if ( _particles[p].velocity.y < ZERO )
{
//_particles[p].velocity.y = -_particles[p].velocity.y * 0.5f;
}

_particles[p].position.y = _clayProperties.particleRadius;

_particles[p].velocity.y = ZERO;
		}
	}





	//------------------------------------------------
	// gather interaction forces among neighbors
	//------------------------------------------------
	void gatherInteractionForces( int p ) 
	{
		for (int n=0; n<NUM_NEIGHBORS; n++)
		{
			Vector3 vectorBetweenParticles = _particles[ _particles[p].neighbors[n] ].position - _particles[p].position;
			float distanceSquared = vectorBetweenParticles.sqrMagnitude;

			if ( distanceSquared > ZERO )
			{
				//-----------------------------------------------
				// apply particle interaction forces here
				//-----------------------------------------------
				float distance = vectorBetweenParticles.magnitude;
				Vector3 direction = vectorBetweenParticles / distance;

				//-----------------------------------------------
				// collisions
				//-----------------------------------------------
				if ( distance < _clayProperties.collisionRadius )
				{
					float collisionForce = ( ONE - ( distance / _clayProperties.collisionRadius ) ) * _clayProperties.collisionForce;

					_particles[ p							].force -= direction * collisionForce;
					_particles[ _particles[p].neighbors[n] 	].force += direction * collisionForce;

					_particles[ p							].force -= _particles[ p							].velocity * _clayProperties.collisionFriction;
					_particles[ _particles[p].neighbors[n] 	].force -= _particles[ _particles[p].neighbors[n] 	].velocity * _clayProperties.collisionFriction;
				}	

				//-----------------------------------------------
				// attractions
				//-----------------------------------------------
				else if ( distance < _clayProperties.attractionRadius )
				{
					float f = ( distance - _clayProperties.collisionRadius ) / ( _clayProperties.attractionRadius - _clayProperties.collisionRadius );
	
					float ff = f * 2.0f;
	
					if ( f > ONE_HALF ) 
					{ 
						ff = 2.0f - f; 
					}
	
					float attractionForce = ff * _clayProperties.attractionForce;
					_particles[ p							].force += direction * attractionForce;
					_particles[ _particles[p].neighbors[n] 	].force -= direction * attractionForce;
				}
			}
		}
	}



	//-----------------------------
	// update neighbors
	//-----------------------------
	void updateNeighbors( int p ) 
	{
		//------------------------------------
		// find the farthest neighbor...
		//------------------------------------
		updateFarthestNeighbor(p);

		//------------------------------------------------------
		// scan through a chunk of the particle array...
		//------------------------------------------------------
		for (var o=0; o<TEST_CHUNK_SIZE; o++)
		{
			//---------------------------------------
			// update the particle index to test...
			//---------------------------------------
			_particles[p].testParticle ++;

			if ( _particles[p].testParticle == NUM_PARTICLES )
			{
				_particles[p].testParticle = 0;
			}

			//---------------------------------------------------------------------------------------
			// make sure the test particle is not already a neighbor, and also not p itself...
			//---------------------------------------------------------------------------------------
			if (( ! isANeighbor( p, _particles[p].testParticle ) )
			&&  ( p != _particles[p].testParticle ) )
			{
				//----------------------------------------------------------------------------
				// if the test particle is closer than the farthest neighbor, then replace it
				//----------------------------------------------------------------------------
				Vector3 v = _particles[p].position - _particles[ _particles[p].testParticle ].position;
				float distance = v.magnitude;
				if ( distance < _particles[p].farthestNeighborDistance )
				{
					_particles[p].neighbors[ _particles[p].farthestNeighborIndex ] = _particles[p].testParticle;
				}
			}
		}
	}



	//-----------------------------------
	void updateFarthestNeighbor( int p )
	{
		_particles[p].farthestNeighborDistance = ZERO;

		for (int n=0; n<NUM_NEIGHBORS; n++)
		{
			Vector3 v = _particles[p].position - _particles[ _particles[p].neighbors[n] ].position;
			float distance = v.magnitude;
			if ( distance > _particles[p].farthestNeighborDistance )
			{
				_particles[p].farthestNeighborIndex = n;
				_particles[p].farthestNeighborDistance 	= distance;
			}
		}
	}



	
	//------------------------------
	bool isANeighbor( int p, int o )
	{		
		for (int n=0; n<NUM_NEIGHBORS; n++)
		{
			if ( _particles[p].neighbors[n] == o )
			{
				return true;
			}
		}
	
		return false;	
	}



	//------------------------------------------
	// sculpting
	//------------------------------------------
	public void setScluptingManipulator( Vector3 sculptingPosition, float sculptingRadius ) 
	{
		_sculpting.velocity = sculptingPosition - _sculpting.position;
		_sculpting.position = sculptingPosition;

		for (int p=0; p<NUM_PARTICLES; p++)
		{
			Vector3 vectorFromParticleTpSculptingTool = sculptingPosition - _particles[p].position;

			float distanceSquared = vectorFromParticleTpSculptingTool.sqrMagnitude;
			float minDistance = sculptingRadius * ONE_HALF + _clayProperties.particleRadius; 
			if ( distanceSquared < minDistance * minDistance )
			{
				if ( distanceSquared > ZERO )
				{			
					float distance = Mathf.Sqrt( distanceSquared );	
					Vector3 direction = vectorFromParticleTpSculptingTool / distance;
					float penetration = ONE - ( distance / minDistance );

					_particles[p].position = _sculpting.position - direction * ( sculptingRadius * ONE_HALF + _clayProperties.particleRadius );
					//_particles[p].velocity -= direction * penetration * _clayProperties.sculptingForce;	
					//_particles[p].velocity *= ( ONE - _clayProperties.sculptingFriction );	
				}
			}
		}
	}

} //---------------------------------------------
 // end of class
//-----------------------------------------------

 