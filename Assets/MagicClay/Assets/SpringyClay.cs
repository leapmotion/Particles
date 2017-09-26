using UnityEngine;
using System.Collections;

public class SpringyClay : MonoBehaviour 
{
	//--------------------------------------------------
	// math constants
	//--------------------------------------------------
	private const float ZERO	 	= 0.0f;
	private const float ONE_HALF 	= 0.5f;
	private const float ONE			= 1.0f;
	private const int	NULL		= -1;
	private const float	LARGE_FLOAT	= 100000.0f;

	//--------------------------------------------------
	// integer/counting constants
	//--------------------------------------------------
	private const int	MAX_PARTICLES				= 4000;
	private const int	INIT_RES					= 18;  // an important number related to MAX_PARTICLES
	private const int	NUM_SPRINGS_PER_PARTICLE	= 6;

	//--------------------------------------------------
	// size/length constants
	//--------------------------------------------------
	private const float	BLOB_RADIUS				= 0.1f;
	private const float	PARTICLE_RADIUS			= 0.01f;
	private const float VISUAL_PARTICLE_RADIUS	= PARTICLE_RADIUS * 1.2f;
	private const float INITIAL_JITTER 			= PARTICLE_RADIUS * 0.5f;
	private const float	MINIMUM_SPRING_LENGTH	= PARTICLE_RADIUS * 1.5f;
	private const float	MAXIMUM_SPRING_LENGTH	= PARTICLE_RADIUS * 3.0f;
//private const float LENGTH_READJUSTMENT		= 0.8f;
	private const float LENGTH_READJUSTMENT		= 0.0f;

	//--------------------------------------------------
	// physics constants
	//--------------------------------------------------
	private const float	AIR_FRICTION			= 0.5f;
	private const float	SPRING_TENSION			= 0.02f;
	private const float	SPRING_RELAXATION		= 0.2f;
	private const float	SCULPTING_FORCE			= 0.01f;
	private const float SCULPTING_RESISTANCE	= 0.0f;

	struct Spring
	{
		public int   index;
		public float length;
	} 

	struct Particle
	{
		public GameObject 	gameObject;
		public Vector3    	position;
		public Vector3    	velocity;
		public Spring[] 	springs;
	} 

	private Particle[] 	_particles = new Particle[ MAX_PARTICLES ];
	private int			_numParticles;
	private float 		_defaultSumOfSpringLengths;


	//------------------------------------------
	// Start
	//------------------------------------------
	void Start() 
	{
		_defaultSumOfSpringLengths = ZERO;
		_numParticles = 0;

		for (int p=0; p<MAX_PARTICLES; p++)
		{
			_particles[p] = new Particle();
			_particles[p].springs = new Spring[ NUM_SPRINGS_PER_PARTICLE ];

			for (int s=0; s<NUM_SPRINGS_PER_PARTICLE; s++)
			{
				_particles[p].springs[s] = new Spring();
				_particles[p].springs[s].index 	= NULL;
				_particles[p].springs[s].length = ZERO;
			}
		}
	
		//-----------------------
		// create initial blob
		//-----------------------
		initializeBlob( BLOB_RADIUS );
	}
	


	//-----------------------
	// initialize blob 
	//-----------------------
	void initializeBlob( float radius ) 
	{
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
					float x = -radius + ( (float)i / (float)( INIT_RES - 1 ) ) * radius * 2.0f;
					float y = -radius + ( (float)j / (float)( INIT_RES - 1 ) ) * radius * 2.0f;
					float z = -radius + ( (float)k / (float)( INIT_RES - 1 ) ) * radius * 2.0f;

					x += ( -INITIAL_JITTER * ONE_HALF + Random.value * INITIAL_JITTER );
					y += ( -INITIAL_JITTER * ONE_HALF + Random.value * INITIAL_JITTER );
					z += ( -INITIAL_JITTER * ONE_HALF + Random.value * INITIAL_JITTER );

					Vector3 testPosition = new Vector3( x, y, z );

					float distance = testPosition.magnitude;

					if (( p < MAX_PARTICLES )
					&& ( distance < radius + PARTICLE_RADIUS ))
					{
						_particles[p].position = testPosition;
						p++;				
					}
				}
			}
		}
	
		_numParticles = p;

		//-------------------------------------
		// set up the particle game objects
		//-------------------------------------
		createGameObjects();

		//-----------------------
		// calculate springs
		//-----------------------
		calculateSprings();
	}



	//-----------------------
	// create game objects
	//-----------------------
	void createGameObjects() 
	{
		for (int p=0; p<_numParticles; p++)
		{
			Material photoMaterial = (Material)Resources.Load( "Whatever" );
			Texture2D  photoTexture = Resources.Load( "clay_particle_for_quad" ) as Texture2D;
			//_particles[p].gameObject = GameObject.CreatePrimitive( PrimitiveType.Sphere );
			_particles[p].gameObject = GameObject.CreatePrimitive( PrimitiveType.Quad );
			_particles[p].gameObject.GetComponent<MeshRenderer>().material = photoMaterial;
			_particles[p].gameObject.GetComponent<Renderer>().material.mainTexture = photoTexture;
			_particles[p].gameObject.transform.localScale = new Vector3( VISUAL_PARTICLE_RADIUS, VISUAL_PARTICLE_RADIUS, VISUAL_PARTICLE_RADIUS );			
			_particles[p].gameObject.transform.position = _particles[p].position;
		}
	}

	//-----------------------
	// calculate springs
	//-----------------------
	void calculateSprings() 
	{
		//Debug.Log( "" );
		//Debug.Log( "--------------------------------------------" );
		//Debug.Log( "Let's calculate springs" );
		//Debug.Log( "--------------------------------------------" );
		
		//-------------------------------------
		// clear out all springs to start
		//-------------------------------------
		for (int p=0; p<_numParticles; p++)
		{
			for (int s=0; s<NUM_SPRINGS_PER_PARTICLE; s++)
			{
				_particles[p].springs[s].index  = NULL;
				_particles[p].springs[s].length = ZERO;
			}
		}

		for (int p=0; p<_numParticles; p++)
		{
			for (int s=0; s<NUM_SPRINGS_PER_PARTICLE; s++)
			{
				float smallestDistance = LARGE_FLOAT;

				//Debug.Log( "" );
				//Debug.Log( "--------------------------------------------" );
				//Debug.Log( "I am particle " + p + ", and I want to connect spring " + s + " to a particle..." );

				for (int o=0; o<_numParticles; o++)
				{
					if ( o != p )
					{
						bool okay = true;

						//Debug.Log( "checking particle " + o );

						//------------------------------------------------------------------------------------
						// make sure one of my previous springs hasn't already chosen this particle...
						//------------------------------------------------------------------------------------
						for (int os=0; os<s; os++)
						{
							if ( _particles[p].springs[os].index == o )
							{
								//Debug.Log( "oops: looks like spring " + os + " already has chosen particle " + o );
								okay = false;
							}	
						}

						//------------------------------------------------------------------------------------
						// make sure this particle hasn't already chosen me for one of its springs...
						//------------------------------------------------------------------------------------
						for (int os=0; os<NUM_SPRINGS_PER_PARTICLE; os++)
						{
							if ( _particles[o].springs[os].index == p )
							{
								//Debug.Log( "oops: looks like particle " + o + " already has chosen to attach its spring " + os + " to me." );
								okay = false;
							}
						}

						if ( okay )
						{
							Vector3 vectorToOther = _particles[o].position - _particles[p].position;
							float distance = vectorToOther.magnitude;
	
							//Debug.Log( "particle " + o + " has a distance of " + distance );
	
							if ( distance <= smallestDistance )
							{
								smallestDistance = distance;

								//Debug.Log( "the candidate is " + o );

								_particles[p].springs[s].index  = o;
								_particles[p].springs[s].length = distance;
							}
						}
					}
				}
			}
		}

		//----------------------------------------------
		// calculate the sum of all spring lengths...
		//----------------------------------------------
		_defaultSumOfSpringLengths = calculateSumOfSpringLengths();
	}


	//------------------------------------------
	// calculate sum of all spring lengths
	//------------------------------------------
	public float calculateSumOfSpringLengths() 
	{
		float sum = ZERO;

		for (int p=0; p<_numParticles; p++)
		{
			for (int s=0; s<NUM_SPRINGS_PER_PARTICLE; s++)
			{
				if ( _particles[p].springs[s].index != NULL )
				{
					sum += _particles[p].springs[s].length;
				}
			}
		}

		return sum;
	}



	//------------------------------------------
	// Update
	//------------------------------------------
	void Update() 
	{
		for (int p=0; p<_numParticles; p++)
		{
			//---------------------------------------------------------
			// air friction
			//---------------------------------------------------------
			_particles[p].velocity *= ( ONE - AIR_FRICTION );

			for (int s=0; s<NUM_SPRINGS_PER_PARTICLE; s++)
			{
				if ( _particles[p].springs[s].index != NULL )
				{
					int o = _particles[p].springs[s].index;
					Vector3 vectorToOther = _particles[o].position - _particles[p].position;
					float distance = vectorToOther.magnitude;
	
					if ( distance > ZERO )
					{
						Vector3 direction = vectorToOther / distance;
						float diff = distance - _particles[p].springs[s].length;
	
						float tensionForce    = diff * SPRING_TENSION;
						float relaxationForce = diff * SPRING_RELAXATION;
	
						_particles[p].velocity += direction * tensionForce;
						_particles[o].velocity -= direction * tensionForce;
	
						_particles[p].velocity += direction * relaxationForce;
						_particles[o].velocity -= direction * relaxationForce;
					}
				}
			}

			//---------------------------------------------------------
			// update position by velocity
			//---------------------------------------------------------
			_particles[p].position += _particles[p].velocity;
			_particles[p].gameObject.transform.position = _particles[p].position;
		}
	}

	//------------------------------------------
	// sculpting
	//------------------------------------------
	public void setScluptingManipulator( Vector3 sculptingPosition, float sculptingRadius ) 
	{
		float lengthAdjustment = _defaultSumOfSpringLengths - calculateSumOfSpringLengths();

		for (int p=0; p<_numParticles; p++)
		{
			Vector3 vectorFromParticleTpSculptingTool = sculptingPosition - _particles[p].position;

			float distanceSquared = vectorFromParticleTpSculptingTool.sqrMagnitude;
			float minDistance = sculptingRadius * ONE_HALF + PARTICLE_RADIUS; 
			if ( distanceSquared < minDistance * minDistance )
			{
				if ( distanceSquared > ZERO )
				{			
					float distance = Mathf.Sqrt( distanceSquared );	
					Vector3 direction = vectorFromParticleTpSculptingTool / distance;
					float penetration = ONE - ( distance / minDistance );

					_particles[p].velocity -= direction * penetration * SCULPTING_FORCE;
					_particles[p].position -= direction * penetration * SCULPTING_FORCE;

					//------------------------------------------------		
					// recalculate spring length (clay memory)	
					//------------------------------------------------		
					for (int s=0; s<NUM_SPRINGS_PER_PARTICLE; s++)
					{
						if ( _particles[p].springs[s].index != NULL )
						{
							int o = _particles[p].springs[s].index;
							Vector3 vectorToOther = _particles[o].position - _particles[p].position;
							_particles[p].springs[s].length = 
							_particles[p].springs[s].length * SCULPTING_RESISTANCE +
							vectorToOther.magnitude * ( ONE - SCULPTING_RESISTANCE );

							//--------------------------------------------------------------------
							// adjust length to accommodate change in sum of spring lengths
							//--------------------------------------------------------------------
							_particles[p].springs[s].length += ( lengthAdjustment / (float)_numParticles ) * LENGTH_READJUSTMENT;

							if ( _particles[p].springs[s].length < MINIMUM_SPRING_LENGTH ) { _particles[p].springs[s].length = MINIMUM_SPRING_LENGTH; }
							if ( _particles[p].springs[s].length > MAXIMUM_SPRING_LENGTH ) { _particles[p].springs[s].length = MAXIMUM_SPRING_LENGTH; }
						}
					}
				}
			}
		}
	}
}

