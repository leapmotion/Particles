using System.Runtime.InteropServices;
using UnityEngine;
using float4 = UnityEngine.Vector4;
using float3 = UnityEngine.Vector3;

public class ParticleManager : MonoBehaviour {

	private const string SIMULATION_KERNEL_NAME = "Simulate_Basic";

 	private const float ZERO 		= 0.0f;
  	private const float ONE_HALF  	= 0.5f;
  	private const float ONE  		= 1.0f;

	//---------------------------------------------
	// particle physics constants
	//---------------------------------------------
  	private const int 	MAX_PARTICLES 		= 64 * 8;
	private const int   MIN_FORCE_STEPS 	= 1;
	private const int   MAX_FORCE_STEPS 	= 5;
	private const int   MIN_SPECIES 		= 1;
	private const int   MAX_SPECIES 		= 10; // let's allow one for each finger
	private const float MAX_DELTA_TIME 		= ONE / 20.0f;
	private const float TEST_DELTA_TIME 	= MAX_DELTA_TIME;
  	private const float BOUNDARY_FORCE		= 0.01f;
  	private const float MIN_DRAG 			= 0.01f;
  	private const float MAX_DRAG 			= 0.2f;
  	private const float PARTICLE_RADIUS		= 0.005f; //meters
  	private const float ENVIRONMENT_RADIUS	= 0.8f;   //meters
  	private const float MAX_SOCIAL_RANGE 	= 0.4f;   //meters
  	private const float MIN_COLLISION_FORCE = 0.0f;
	private const float MAX_COLLISION_FORCE = 1.0f;
  	private const float MAX_SOCIAL_FORCE 	= 0.1f;

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

	[SerializeField]
	private bool _useComputeShader = false;

  	[SerializeField]
  	private bool _useMultithreading = false;

 	[SerializeField]
	private Mesh _mesh;

	[SerializeField]
	private Shader _cpuShader;

	[SerializeField]
	private Shader _displayShader;

  	private struct Head 
	{
		public GameObject 	gameObject;
		public Quaternion 	rotation;
  	}

  	private struct ParticleEmitter 
	{
		public	Vector3 position;
		public	Vector3 direction;
		public	float   strength;
		public	float   jitter;
		public  float 	rate;
		public	int		species;
  	}

	//-------------------------------------------------------------------
	// all the attributes of particles are specified in terms of species
	//-------------------------------------------------------------------
 	private struct Species 
	{
		public  float 	drag;
		public	float	socialForce;
		public	float	socialRange;
		public	float	collisionForce;
    	public	Vector4 color;
  	}

 	[StructLayout(LayoutKind.Sequential)]
  	private struct Particle 
	{
		public	bool	active;		// currently being used for birth and death
		public  float 	age;		// in seconds
		public	int		species;	// set at birth
    	public 	Vector3	position;	// dynamic
    	public 	Vector3	velocity;	// dynamic
    	public 	Vector4	accumulatedForce; // dynamic
  	}

	[SerializeField]
	private ComputeShader 		_simulationShader;
	private ParticleEmitter[] 	_emitters;
	private Particle[]			_particles;
	private Particle[] 			_backBuffer;
	private Species[]			_species;
	private Head				_myHead;
	private GameObject			_myLeftHand;
	private GameObject			_myRightHand;
	private Vector3				_homePosition;
	private Material 			_cpuMaterial;
	private Material 			_computeMaterial;
	private ComputeBuffer		_particleBuffer;
	private int					_numParticles;
	private ComputeBuffer 		_argBuffer;
	private int 				_simulationKernelIndex;
	private Camera				_camera;

  void OnEnable() {
    if (!SystemInfo.supportsComputeShaders) {
      Debug.LogError("This system does not support compute shaders");
      return;
    }

    if (!SystemInfo.supportsInstancing) {
      Debug.LogError("This system does not support instancing!");
      return;
    }

    _argBuffer = new ComputeBuffer(5, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
    _particleBuffer = new ComputeBuffer(MAX_PARTICLES, Marshal.SizeOf(typeof(Particle)));

    _cpuMaterial = new Material(_cpuShader);

    _computeMaterial = new Material(_displayShader);
    _computeMaterial.SetBuffer("_Particles", _particleBuffer);

    _simulationKernelIndex = _simulationShader.FindKernel(SIMULATION_KERNEL_NAME);
    _simulationShader.SetInt("_MaxParticles", _numParticles);
    _simulationShader.SetBuffer(_simulationKernelIndex, "_Particles", _particleBuffer);

    _camera = GetComponent<Camera>();

    _homePosition = Vector3.zero;

	//-----------------------------------------------
	// create head and hands (for testing purposes)
	//-----------------------------------------------
	createHeadAndHands();

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
		_emitters[e].rate		= ZERO;
		_emitters[e].strength 	= ZERO;
		_emitters[e].jitter 	= ZERO;
	}

	//-----------------------------------------
	// intitialize particle array
	//-----------------------------------------
	_particles  = new Particle[MAX_PARTICLES];
	_backBuffer = new Particle[MAX_PARTICLES];
	for (int i = 0; i < MAX_PARTICLES; i++) 
	{
	  _particles[i] = new Particle();
	  _particles[i].age = 0.0f;
	  _particles[i].active = false;
	  _particles[i].species = 0;
	  _particles[i].accumulatedForce = Vector4.zero;
	  _particles[i].velocity = Vector3.zero;
	  _particles[i].position = Vector3.zero;
	}

    _particleBuffer.SetData(_particles);

    //-----------------------------------------
    // intitialize species array
    //-----------------------------------------
    _species = new Species[MAX_SPECIES];
    for (int s = 0; s < MAX_SPECIES; s++) 
	{
      _species[s] = new Species();
      _species[s].drag = ZERO;
      _species[s].socialForce = ZERO;
      _species[s].socialRange = ZERO;
      _species[s].collisionForce = ZERO;
      _species[s].color = new Color(ZERO, ZERO, ZERO, ZERO);
    }

    //-----------------------------------------
    // randomize ecosystem
    //-----------------------------------------
    randomizeEcosystem();

    //-----------------------------------------
    // set up the emitters
    //-----------------------------------------
	initializeEmitters();


    uint[] args = new uint[5];
    args[0] = (uint)_mesh.GetIndexCount(0);
    args[1] = MAX_PARTICLES;
    _argBuffer.SetData(args);
  }

  void OnDisable() {
    if (_particleBuffer != null) {
      _particleBuffer.Release();
    }

    if (_argBuffer != null) {
      _argBuffer.Release();
    }
  }

  void Update() {
    SimulateParticles(TEST_DELTA_TIME);
    DisplayParticles();
  }

  void OnPostRender() {
    _computeMaterial.SetPass(0);
    Graphics.DrawProcedural(MeshTopology.Points, _numParticles);
  }


	//---------------------------
	// create head and hands...
	//---------------------------
	void createHeadAndHands()
	{
		_myHead.gameObject = GameObject.CreatePrimitive( PrimitiveType.Sphere );
	 	_myHead.gameObject.transform.localScale = new Vector3( HEAD_WIDTH, HEAD_LENGTH, HEAD_DEPTH );	

		_myLeftHand = GameObject.CreatePrimitive( PrimitiveType.Sphere );
	 	_myLeftHand.transform.localScale = new Vector3( HAND_WIDTH, HAND_LENGTH, HAND_DEPTH );	

		_myRightHand = GameObject.CreatePrimitive( PrimitiveType.Sphere );
	 	_myRightHand.transform.localScale = new Vector3( HAND_WIDTH, HAND_LENGTH, HAND_DEPTH );
	}


	//-------------------------------------------
	// initialize emitters...
	//-------------------------------------------
	void initializeEmitters() 
	{
		for (int e=0; e<NUM_FINGERS; e++)
		{
			_emitters[e].strength = 0.05f;
			_emitters[e].rate = 0.1f;
		}

		_emitters[ LEFT_THUMB_FINGER	].species = 0;
		_emitters[ LEFT_INDEX_FINGER 	].species = 1;
		_emitters[ LEFT_MIDDLE_FINGER 	].species = 2;
		_emitters[ LEFT_RING_FINGER 	].species = 3;
		_emitters[ LEFT_PINKY_FINGER 	].species = 4;
		_emitters[ RIGHT_THUMB_FINGER	].species = 0;
		_emitters[ RIGHT_INDEX_FINGER 	].species = 1;
		_emitters[ RIGHT_MIDDLE_FINGER 	].species = 2;
		_emitters[ RIGHT_RING_FINGER	].species = 3;
		_emitters[ RIGHT_PINKY_FINGER	].species = 4;
	}


	//-------------------------------------------
	// simulate particles...
	//-------------------------------------------
	void SimulateParticles( float deltaTime ) 
	{
		//-----------------------------------------------------------
		// for testing purposes: update the positions, rotations, 
		// etc. of the head, hands, and fingers...
		//-----------------------------------------------------------
		updateHeadAndHands();

		//--------------------------------------------------------------------------------
		// The home position is the centroid of the environment in which particles live...
		//--------------------------------------------------------------------------------
	   _homePosition = _myHead.gameObject.transform.position + _myHead.gameObject.transform.forward * ENVIRONMENT_RADIUS;

		//------------------------------------
		// update the particle emitters
		//------------------------------------
		for (int e=0; e<NUM_FINGERS; e++)
		{
			updateParticleEmitter(e);
		}

		//------------------------------------
		// simulate particle physics...
		//------------------------------------
		if ( _useComputeShader ) 
		{
			simulateParticlesCompute( deltaTime );
		} 
		else 
		{
			simulateParticlesCPU( deltaTime );
		}
	}



	//-----------------------------------------------------------------------------
	// This is mostly for testing purposes...
	// 
	// I am update the positions, rotations, etc. of the head, and hands
	// all of which the particles may care about from time to time.
	//-----------------------------------------------------------------------------
	void updateHeadAndHands()
	{
		//-----------------------------------------------------------
		// set the position, rotation, and directions of my head...
		//-----------------------------------------------------------
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
   		_emitters[ LEFT_THUMB_FINGER 	].position	= _myLeftHand.transform.position;
    	_emitters[ LEFT_INDEX_FINGER 	].position	= _myLeftHand.transform.position;
    	_emitters[ LEFT_MIDDLE_FINGER 	].position	= _myLeftHand.transform.position;
    	_emitters[ LEFT_RING_FINGER 	].position	= _myLeftHand.transform.position;
    	_emitters[ LEFT_PINKY_FINGER 	].position 	= _myLeftHand.transform.position;
   		_emitters[ RIGHT_THUMB_FINGER 	].position	= _myRightHand.transform.position;
    	_emitters[ RIGHT_INDEX_FINGER 	].position	= _myRightHand.transform.position;
    	_emitters[ RIGHT_MIDDLE_FINGER 	].position	= _myRightHand.transform.position;
    	_emitters[ RIGHT_RING_FINGER 	].position	= _myRightHand.transform.position;
    	_emitters[ RIGHT_PINKY_FINGER 	].position 	= _myRightHand.transform.position;

		//---------------------------------------------------------
		// set the directions of the finger emitters ...
		// NOT IMPLEMENTED YET
		//---------------------------------------------------------
   		_emitters[ LEFT_THUMB_FINGER 	].direction	= _myLeftHand.transform.up;
    	_emitters[ LEFT_INDEX_FINGER 	].direction	= _myLeftHand.transform.up;
    	_emitters[ LEFT_MIDDLE_FINGER 	].direction	= _myLeftHand.transform.up;
    	_emitters[ LEFT_RING_FINGER 	].direction	= _myLeftHand.transform.up;
    	_emitters[ LEFT_PINKY_FINGER 	].direction = _myLeftHand.transform.up;
   		_emitters[ RIGHT_THUMB_FINGER 	].direction	= _myRightHand.transform.up;
    	_emitters[ RIGHT_INDEX_FINGER 	].direction	= _myRightHand.transform.up;
    	_emitters[ RIGHT_MIDDLE_FINGER 	].direction	= _myRightHand.transform.up;
    	_emitters[ RIGHT_RING_FINGER 	].direction	= _myRightHand.transform.up;
    	_emitters[ RIGHT_PINKY_FINGER 	].direction = _myRightHand.transform.up;
	}




  void DisplayParticles() {
    if (_useComputeShader) {
      displayParticlesCompute();
    } else {
      displayParticlesCPU();
    }
  }

  #region COMPUTE SHADER IMPLEMENTATION
  private void simulateParticlesCompute(float deltaTime) {
    _simulationShader.SetFloat("_DeltaTime", deltaTime);
    _simulationShader.Dispatch(_simulationKernelIndex, _numParticles / 64, 1, 1);
  }

  private void displayParticlesCompute() {
    Graphics.DrawMeshInstancedIndirect(_mesh,
                                        0,
                                        _computeMaterial,
                                        new Bounds(Vector3.zero, Vector3.one * 10000),
                                        _argBuffer);
  }
  #endregion



	//----------------------------------
	// randomize ecosystem
	//----------------------------------
	private void randomizeEcosystem() 
	{
		//----------------------------------------
		// randomize species genes
		//----------------------------------------
		for (int s = 0; s < MAX_SPECIES; s++) 
		{
			_species[s].drag 			= MIN_DRAG + Random.value * (MAX_DRAG - MIN_DRAG);
			_species[s].socialForce 	= -MAX_SOCIAL_FORCE + Random.value * MAX_SOCIAL_FORCE * 2.0f;
			_species[s].socialRange 	= Random.value * MAX_SOCIAL_RANGE;
			_species[s].collisionForce 	= MIN_COLLISION_FORCE + Random.value * (MAX_COLLISION_FORCE - MIN_COLLISION_FORCE);
			_species[s].color 			= new Color(Random.value, Random.value, Random.value, ONE);
		}

		//--------------------------------------
		// randomize particles
		//--------------------------------------
		_numParticles = MAX_PARTICLES;
		for (int i = 0; i < _numParticles; i++) 
		{
			_particles[i].species  = MIN_SPECIES + (int)(Random.value * (MAX_SPECIES - MIN_SPECIES));
			_particles[i].position = _homePosition + Random.insideUnitSphere * ENVIRONMENT_RADIUS;
			_particles[i].velocity = Vector3.zero;
		}
	}



 	//-------------------------------------------
  	// update particle emitter 
  	//-------------------------------------------
	private void updateParticleEmitter( int e ) 
	{
		if ( Random.value < 0.1 ) 
		{
			int p = (int)( Random.value * _numParticles );
	
			if ( ! _particles[p].active )
			{
				_particles[p].active   = true;
				_particles[p].species  = _emitters[e].species;
				_particles[p].position = _emitters[e].position;
				_particles[p].velocity = _emitters[e].direction * _emitters[e].strength; 
	
				if ( _emitters[e].jitter > 0.0f )
				{
					_particles[p].velocity += 
					new Vector3
					( 
						-_emitters[e].jitter + Random.value * _emitters[e].jitter * 2.0f, 
						-_emitters[e].jitter + Random.value * _emitters[e].jitter * 2.0f, 
						-_emitters[e].jitter + Random.value * _emitters[e].jitter * 2.0f 
					);
				}
			}
		}
	}

 	//----------------------------------
  	// kill particle
  	//----------------------------------
	private void killParticle( int p ) 
	{
		if ( ( p >=0 ) && ( p < MAX_PARTICLES) )
		{
			_particles[p].active = false;
		}
	}


  #region CPU IMPLEMENTATION
  ParallelForeach _parallelForeach;

  //----------------------------------------------------
  // run the physics for all the particles
  //----------------------------------------------------
  private void simulateParticlesCPU(float deltaTime) {
    if (_useMultithreading) {
      if (_parallelForeach == null) {
        _parallelForeach = new ParallelForeach((a, b) => particleSimulationLogic(a, b, deltaTime));
      }

      _parallelForeach.Dispatch(_numParticles);
      _parallelForeach.Wait();
    } else {
      particleSimulationLogic(0, _numParticles, deltaTime);
    }

    //----------------------------
    // swap back and front buffer
    //----------------------------
    var temp 	= _backBuffer;
    _backBuffer = _particles;
    _particles 	= temp;
  }

 	private void particleSimulationLogic(int startIndex, int endIndex, float deltaTime) 
	{
		//-------------------------------------------------------
		// Loop through every particle 				  
		//-------------------------------------------------------
   		for (int i = startIndex; i < endIndex; i++) 
		{
			if ( _particles[i].active ) 
			{
				//------------------------------------------------------
				// reset accumulated force 
				//------------------------------------------------------
				_particles[i].accumulatedForce = new float4(ZERO, ZERO, ZERO, ZERO);

				//------------------
				// advance age
				//------------------
				_particles[i].age += deltaTime;

				//---------------------------------------
				// Loop through every (other) particle  
				//---------------------------------------
				for (uint o = 0; o < _numParticles; o++) 
				{
					if ( _particles[o].active )
					{
		        		if (o == i) continue; //Dont compare against self!
		
				        Particle other = _particles[o];
				        float3 	 vectorToOther = other.position - _particles[i].position;
				        float 	 distanceSquared = vectorToOther.sqrMagnitude;
		
		        		float socialRangeSquared = _species[ _particles[i].species ].socialRange * _species[ _particles[i].species ].socialRange;
		        		if ((distanceSquared < socialRangeSquared) && (distanceSquared > ZERO)) 
						{
							float distance = Mathf.Sqrt(distanceSquared);
							float3 directionToOther = vectorToOther / distance;
		
							//--------------------------------------------------------------------------
							// Accumulate forces from social attractions/repulsions to other particles
							//--------------------------------------------------------------------------
							_particles[i].accumulatedForce += (float4)(_species[ _particles[i].species ].socialForce * directionToOther);
							_particles[i].accumulatedForce.w += 1; //keeping track of the number of particles exerting a force
		
							//----------------------------------------
							// collisions
							//----------------------------------------
							float combinedRadius = PARTICLE_RADIUS * 2;
		          			if (distance < combinedRadius) 
							{
								float penetration = ONE - distance / combinedRadius;
								float averageCollisionForce =
		            			(
		              				_species[_particles[i].species].collisionForce +
		              				_species[other.species].collisionForce
		            			) * ONE_HALF;
		
		            			_particles[i].velocity -= deltaTime * averageCollisionForce * directionToOther * penetration;
		          			}
						}
	        		}
	      		}

				//---------------------------------------------
				// Apply accumulated forces to the velocity
				//---------------------------------------------
				if ( _particles[i].accumulatedForce.w > 0 ) 
				{
					//-----------------------------------------------------------------------------------------
					// divide by w, which is the number of particles that contributed to the accumulated force
					//-----------------------------------------------------------------------------------------
					_particles[i].velocity += deltaTime * (Vector3)_particles[i].accumulatedForce / _particles[i].accumulatedForce.w;
				}
	
				//--------------------------------------------------------------------------------------
				// apply forces to keep the particle from getting past the boundary of the environment
				//--------------------------------------------------------------------------------------
				Vector3 vectorFromHome = _particles[i].position - _homePosition;
				float distanceFromHome = vectorFromHome.magnitude;
		
				if ( distanceFromHome > ENVIRONMENT_RADIUS ) 
				{
					Vector3 directionFromHome = vectorFromHome / distanceFromHome;
					float force = ( distanceFromHome - ENVIRONMENT_RADIUS ) * BOUNDARY_FORCE;
					_particles[i].velocity -= force * directionFromHome * deltaTime;
				}

				//-------------------------------------------
				// dampening (kinda like air friction)
				//-------------------------------------------
				_particles[i].velocity *= (ONE - _species[_particles[i].species].drag);
	
				//------------------------------------
				// update position by velocity
				//------------------------------------
				_particles[i].position += _particles[i].velocity;
	
				//----------------------
				// fill back buffer
				//----------------------
				_backBuffer[i] = _particles[i];
			}
		}
	}



	private void displayParticlesCPU() {
    	var block = new MaterialPropertyBlock();
    	for (int i = 0; i < _numParticles; i++) {
			if ( _particles[i].active ) 
			{
				block.SetColor("_Color", _species[_particles[i].species].color);
				var matrix = Matrix4x4.TRS(_particles[i].position, Quaternion.identity, Vector3.one * PARTICLE_RADIUS * 2);
				Graphics.DrawMesh(_mesh, matrix, _cpuMaterial, 0, null, 0, block);
    		}
  		}
	}


  #endregion

}
