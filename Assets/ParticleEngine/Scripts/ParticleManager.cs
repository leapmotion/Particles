using System.Runtime.InteropServices;
using UnityEngine;
//using float4 = UnityEngine.Vector4;
//using float3 = UnityEngine.Vector3;

public class ParticleManager : MonoBehaviour {

	private const string SIMULATION_KERNEL_NAME = "Simulate_Basic";

 	private const float ZERO 		= 0.0f;
  	private const float ONE_HALF  	= 0.5f;
  	private const float ONE  		= 1.0f;

	//---------------------------------------------
	// particle physics constants
	//---------------------------------------------
	private	const int	NULL_PARTICLE		= -1;
  	private const int 	NUM_PARTICLES 		= 64 * 8;
	private const int   MIN_FORCE_STEPS 	= 1;
	private const int   MAX_FORCE_STEPS 	= 5;
	private const int   MIN_SPECIES 		= 1;
	private const int   MAX_SPECIES 		= 12;
	private const float MAX_DELTA_TIME 		= ONE / 20.0f;
	private const float TEST_DELTA_TIME 	= MAX_DELTA_TIME;
  	private const float PARTICLE_RADIUS		= 0.005f; //meters
 	private const float BOUNDARY_FORCE		= 0.1f;
  	private const float ENVIRONMENT_RADIUS	= 1.0f;   //meters
  	private const float ENVIRONMENT_FRONT_OFFSET = ENVIRONMENT_RADIUS + 0.2f;

	//---------------------------------------------------------
	// These parameters are critical for clustering behavior
	//---------------------------------------------------------
   	private const float MIN_DRAG 			= 0.05f;
  	private const float MAX_DRAG 			= 0.3f;
  	private const float MIN_COLLISION_FORCE = 0.1f;
	private const float MAX_COLLISION_FORCE = 0.5f;
//private const float MAX_SOCIAL_FORCE 	= 0.1f;
  	private const float MAX_SOCIAL_FORCE 	= 0.005f;
  	private const float MAX_SOCIAL_RANGE 	= 0.4f;

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

	//-------------------------------------------------------------------
	// all the attributes of particles are specified in terms of species
	//-------------------------------------------------------------------
 	private struct Species 
	{
		public  float 	drag;
        public 	int		steps;
		public	float[]	socialForce;
		public	float[]	socialRange;
		public	float	collisionForce;
    	public	Vector4 color;
  	}

 	[StructLayout(LayoutKind.Sequential)]
  	private struct Particle 
	{
		public	bool		active;				// currently being used for birth and death
		public  float 		age;				// in seconds
		public	int			species;			// set at birth
    	public 	Vector3		position;			// dynamic
    	public 	Vector3		velocity;			// dynamic
		public 	Vector3[]   accumulatedForce;	// dynamic
  	}

	[SerializeField]
	private ComputeShader 		_simulationShader;
	private Particle[]			_particles;
	private Particle[] 			_backBuffer;
	private Species[]			_species;
	private Vector3				_homePosition;
	private Material 			_cpuMaterial;
	private Material 			_computeMaterial;
	private ComputeBuffer		_particleBuffer;
	private ComputeBuffer 		_argBuffer;
	private int 				_simulationKernelIndex;
	public  ParticleControl		_particleController;


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
    _particleBuffer = new ComputeBuffer(NUM_PARTICLES, Marshal.SizeOf(typeof(Particle)));

    _cpuMaterial = new Material(_cpuShader);

    _computeMaterial = new Material(_displayShader);
    _computeMaterial.SetBuffer("_Particles", _particleBuffer);

    _simulationKernelIndex = _simulationShader.FindKernel(SIMULATION_KERNEL_NAME);
    _simulationShader.SetInt("_MaxParticles", NUM_PARTICLES);
    _simulationShader.SetBuffer(_simulationKernelIndex, "_Particles", _particleBuffer);

    _homePosition = Vector3.zero;

	//_particleController = GetComponent<ParticleControl>();

	//-----------------------------------------
	// intitialize particle array
	//-----------------------------------------
	_particles  = new Particle[NUM_PARTICLES];
	_backBuffer = new Particle[NUM_PARTICLES];
	for (int i = 0; i < NUM_PARTICLES; i++) 
	{
	  	_particles[i] = new Particle();
	  	_particles[i].age = ZERO;
	  	_particles[i].active = false;
	  	_particles[i].species = 0;
	  	_particles[i].velocity = Vector3.zero;
	  	_particles[i].position = Vector3.zero;

		_particles[i].accumulatedForce = new Vector3[ MAX_FORCE_STEPS ];
		
		for (int a=0; a<MAX_FORCE_STEPS; a++) 
		{
			_particles[i].accumulatedForce[a] = Vector3.zero;
		}
	}

    _particleBuffer.SetData(_particles);

    //-----------------------------------------
    // intitialize species array
    //-----------------------------------------
    _species = new Species[ MAX_SPECIES ];
    for (int s = 0; s < MAX_SPECIES; s++) 
	{
      	_species[s] = new Species();
      	_species[s].drag = ZERO;
      	_species[s].steps = 0;
		_species[s].collisionForce = ZERO;
		_species[s].color = new Color(ZERO, ZERO, ZERO, ZERO);

		_species[s].socialForce = new float[ MAX_SPECIES ];
		_species[s].socialRange = new float[ MAX_SPECIES ];

		for (int o=0; o<MAX_SPECIES; o++)
		{
			_species[s].socialForce[o] = ZERO;
			_species[s].socialRange[o] = ZERO;
		}
    }

    //-----------------------------------------
    // randomize species
    //-----------------------------------------
    randomizeSpecies();

    uint[] args = new uint[5];
    args[0] = (uint)_mesh.GetIndexCount(0);
    args[1] = NUM_PARTICLES;
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
    Graphics.DrawProcedural(MeshTopology.Points, NUM_PARTICLES);
  }

	//-------------------------------------------
	// simulate particles...
	//-------------------------------------------
	void SimulateParticles( float deltaTime ) 
	{
		//--------------------------------------------------------------------------------
		// The home position is the centroid of the environment in which particles live...
		//--------------------------------------------------------------------------------
	   _homePosition = _particleController.getHeadPosition() + _particleController.getHeadForward() * ENVIRONMENT_FRONT_OFFSET;

		//------------------------------------
		// update the emission of particles 
		//------------------------------------
		updateEmittingParticles();

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
    _simulationShader.Dispatch(_simulationKernelIndex, NUM_PARTICLES / 64, 1, 1);
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
	// randomize species
	//----------------------------------
	private void randomizeSpecies() 
	{
		for (int s = 0; s < MAX_SPECIES; s++) 
		{
			_species[s].steps = MIN_FORCE_STEPS + (int)( ( MAX_FORCE_STEPS - MIN_FORCE_STEPS ) * Random.value );            
			_species[s].drag = MIN_DRAG + Random.value * (MAX_DRAG - MIN_DRAG);
			_species[s].collisionForce 	= MIN_COLLISION_FORCE + Random.value * (MAX_COLLISION_FORCE - MIN_COLLISION_FORCE);
			_species[s].color 			= new Color(Random.value, Random.value, Random.value, ONE);

			for (int o=0; o<MAX_SPECIES; o++)
			{
				_species[s].socialForce[o] = -MAX_SOCIAL_FORCE + Random.value * MAX_SOCIAL_FORCE * 2.0f;
				_species[s].socialRange[o] = Random.value * MAX_SOCIAL_RANGE;
			}
		}
	}

 	//-------------------------------------------
	// update the emission of particles 
  	//-------------------------------------------
	private void updateEmittingParticles() 
	{
		for (int e=0; e<_particleController.getNumEmitters(); e++)
		{	
			if ( _particleController.getEmitterActive(e) )
			{
				if ( Random.value < _particleController.getEmitterRate(e) ) 
				{
					int p = getIndexOfInactiveParticle();
			
					if ( p != NULL_PARTICLE )
					{
						_particles[p].active   = true;
						_particles[p].species  = _particleController.getEmitterSpecies(e);
						_particles[p].position = _particleController.getEmitterPosition(e);
						_particles[p].velocity = _particleController.getEmitterDirection(e) * _particleController.getEmitterStrength(e); 
					}
				}
			}
		}
	}


 	//-----------------------------------------
  	// get an inactive particle
	//
	// THIS IS NOT EFFICIENT - it will be
	// replaced by a more efficient process.
  	//-----------------------------------------
	private int getIndexOfInactiveParticle() 
	{
		for (int p=0; p<NUM_PARTICLES; p++) 
		{
			if ( ! _particles[p].active )
			{
				return p;
			}
		}

		return NULL_PARTICLE;
	}


 	//----------------------------------
  	// kill particle
  	//----------------------------------
	private void killParticle( int p ) 
	{
		if ( ( p >=0 ) && ( p < NUM_PARTICLES) )
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

      _parallelForeach.Dispatch(NUM_PARTICLES);
      _parallelForeach.Wait();
    } else {

      particleSimulationLogic(0, NUM_PARTICLES, deltaTime);
    }

    //----------------------------
    // swap back and front buffer
    //----------------------------
    var temp 	= _backBuffer;
    _backBuffer = _particles;
    _particles 	= temp;
  }





 	private void particleSimulationLogic( int startIndex, int endIndex, float deltaTime ) 
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
				for (int a=0; a<MAX_FORCE_STEPS; a++) 
				{
					if (_particles[i].accumulatedForce == null) {
						_particles[i].accumulatedForce = new Vector3[ MAX_FORCE_STEPS ];
					}
					_particles[i].accumulatedForce[a] = Vector3.zero;
				}

				//------------------
				// advance age
				//------------------
				_particles[i].age += deltaTime;

				int numSocialForces = 0;
				Vector3 socialForces = Vector3.zero;

				//---------------------------------------
				// Loop through every (other) particle  
				//---------------------------------------
				for (uint o = 0; o < NUM_PARTICLES; o++) 
				{
					if ( _particles[o].active )
					{
		        		if (o == i) continue; //Dont compare against self!
		
				        Particle other = _particles[o];
				        Vector3	 vectorToOther = other.position - _particles[i].position;
				        float 	 distanceSquared = vectorToOther.sqrMagnitude;		

						float socialRangeSquared = 
						_species[ _particles[i].species ].socialRange[ other.species ] * 
						_species[ _particles[i].species ].socialRange[ other.species ];

		        		if ( ( distanceSquared < socialRangeSquared ) && ( distanceSquared > ZERO ) ) 
						{
							float distance = Mathf.Sqrt( distanceSquared );
							Vector3 directionToOther = vectorToOther / distance;
		
							//--------------------------------------------------------------------------
							// Accumulate forces from social attractions/repulsions to other particles
							//--------------------------------------------------------------------------
							socialForces += _species[ _particles[i].species ].socialForce[ other.species ] * directionToOther;
							numSocialForces ++; 

							//----------------------------------------
							// collisions
							//----------------------------------------
							float combinedRadius = PARTICLE_RADIUS * 2;
		          			if (distance < combinedRadius) 
							{
								float penetration = ONE - distance / combinedRadius;
								float averageCollisionForce =
		            			(
		              				_species[ _particles[i].species ].collisionForce +
		              				_species[ other.species ].collisionForce
		            			) * ONE_HALF;
		
		            			_particles[i].velocity -= deltaTime * averageCollisionForce * directionToOther * penetration;
		          			}
						}
	        		}
	      		}

				//--------------------------------
				// normalize social forces 
				//--------------------------------
				if ( numSocialForces > 0 ) 
				{
					//-----------------------------------------------------------------------------------------
					// divide by w, which is the number of particles that contributed to the accumulated force
					//-----------------------------------------------------------------------------------------
					socialForces /= numSocialForces;
				}
				else 
				{
					socialForces = Vector3.zero;
				}

				/*
				//----------------------------------------------------------
				// scroll the array of force steps and apply delayed force
				//----------------------------------------------------------
				_particles[i].accumulatedForce[ _species[ _particles[i].species ].steps ] = socialForces;

				for (int a=0; a<_species[ _particles[i].species ].steps-1; a++) 
				{
					_particles[i].accumulatedForce[a] = _particles[i].accumulatedForce[a+1];
				}
				*/

_particles[i].accumulatedForce[0] = socialForces;

				//----------------------------------------------------------
				// apply accumulated forces to velocity
				//----------------------------------------------------------
				_particles[i].velocity += _particles[i].accumulatedForce[0];

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
    	for (int i = 0; i < NUM_PARTICLES; i++) {
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
