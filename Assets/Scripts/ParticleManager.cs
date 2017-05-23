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
	private const int   MAX_SPECIES 		= 8;
	private const float MAX_DELTA_TIME 		= ONE / 20.0f;
	private const float TEST_DELTA_TIME 	= MAX_DELTA_TIME;
  	private const float BOUNDARY_FORCE		= 0.5f;
  	private const float MIN_DRAG 			= 0.0f;
  	private const float MAX_DRAG 			= 0.5f;
  	private const float PARTICLE_RADIUS		= 0.01f; //meters
  	private const float ENVIRONMENT_RADIUS	= 0.6f;  //meters
  	private const float MAX_SOCIAL_RANGE 	= 0.3f;  //meters
  	private const float MIN_COLLISION_FORCE = 0.0f;
  	private const float MAX_COLLISION_FORCE = 0.9f;
  	private const float MAX_SOCIAL_FORCE 	= 0.2f;

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
    	public	Vector3 position;
    	public	Vector3 rightward;
    	public	Vector3 upward;
    	public	Vector3 forward;
		public	float 	focalDistance;
  	}

  	private struct Hand 
	{
		public  bool	isRightHand;
    	public	Vector3 position;
    	public	Vector3 rightward;
    	public	Vector3 upward;
    	public	Vector3 forward;
    	public	Vector3 thumbFinger;
    	public	Vector3 indexFinger;
    	public	Vector3 middleFinger;
    	public	Vector3 ringFinger;
    	public	Vector3 pinkyFinger;
  	}

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
		public	bool	active;
		public  float 	age;
		public	int		species;
    	public 	Vector3	position;
    	public 	Vector3	velocity;
    	public 	Vector4	accumulatedForce;
  	}

	[SerializeField]
	private ComputeShader 	_simulationShader;
	private Particle[]		_particles;
	private Particle[] 		_backBuffer;
	private Species[]		_species;
	private Head			_myHead;
	private Hand			_myLeftHand;
	private Hand			_myRightHand;
	private Vector3			_homePosition;
	private Material 		_cpuMaterial;
	private Material 		_computeMaterial;
	private ComputeBuffer	_particleBuffer;
	private int				_numParticles;
	private ComputeBuffer 	_argBuffer;
	private int 			_simulationKernelIndex;
	private Camera			_camera;

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

    //-----------------------------------------
    // intitialize particle array
    //-----------------------------------------
    _particles = new Particle[MAX_PARTICLES];
    _backBuffer = new Particle[MAX_PARTICLES];
    for (int i = 0; i < MAX_PARTICLES; i++) {
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
    for (int s = 0; s < MAX_SPECIES; s++) {
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

	void SimulateParticles( float deltaTime ) 
	{
		//----------------------------------------------------
		// this is just a test to make sure active is working.......
		//----------------------------------------------------
		for (int i = 0; i < _numParticles; i++) 
		{
			if ( i < _numParticles * 0.9f )
			{
				_particles[i].active = true;
			}
		}

		//----------------------------------------------------
		// set the position, forward, and up of my head...
		//----------------------------------------------------
		_myHead.position 	= _camera.transform.position;
		_myHead.rightward	= _camera.transform.right;
		_myHead.upward   	= _camera.transform.up;
		_myHead.forward  	= _camera.transform.forward;

		//----------------------------------------------------
		// set the values for my hands and fingers...
		//----------------------------------------------------
		_myRightHand.isRightHand 	= true;
		_myRightHand.position		= _myHead.position - Vector3.up * 2.0f + _myHead.forward * 2.0f + _myHead.rightward * 1.0f;
    	_myRightHand.rightward		= _myHead.rightward;
    	_myRightHand.upward			= Vector3.up;
    	_myRightHand.forward		= _myHead.forward;
    	_myRightHand.thumbFinger 	= _myRightHand.position + _myRightHand.rightward * -0.05f;
    	_myRightHand.indexFinger  	= _myRightHand.position + _myRightHand.forward   *  0.05f+ _myRightHand.rightward * -0.01f;
    	_myRightHand.middleFinger 	= _myRightHand.position + _myRightHand.forward   *  0.05f+ _myRightHand.rightward * -0.02f;
    	_myRightHand.ringFinger 	= _myRightHand.position + _myRightHand.forward   *  0.05f+ _myRightHand.rightward * -0.03f;
    	_myRightHand.pinkyFinger 	= _myRightHand.position + _myRightHand.forward   *  0.05f+ _myRightHand.rightward * -0.04f;

    if (_useComputeShader) {
      simulateParticlesCompute(deltaTime);
    } else {
      simulateParticlesCPU(deltaTime);
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
  private void randomizeEcosystem() {
    //----------------------------------------
    // randomize species genes
    //----------------------------------------
    for (int s = 0; s < MAX_SPECIES; s++) {
      _species[s].drag = MIN_DRAG + Random.value * (MAX_DRAG - MIN_DRAG);
      _species[s].socialForce = -MAX_SOCIAL_FORCE + Random.value * MAX_SOCIAL_FORCE * 2.0f;
      _species[s].socialRange = Random.value * MAX_SOCIAL_RANGE;
      _species[s].collisionForce = MIN_COLLISION_FORCE + Random.value * (MAX_COLLISION_FORCE - MIN_COLLISION_FORCE);
      _species[s].color = new Color(Random.value, Random.value, Random.value, ONE);
    }

    //--------------------------------------
    // randomize particles
    //--------------------------------------
    _numParticles = MAX_PARTICLES;
    for (int i = 0; i < _numParticles; i++) {
      _particles[i].species = MIN_SPECIES + (int)(Random.value * (MAX_SPECIES - MIN_SPECIES));
      _particles[i].position = _homePosition + Random.insideUnitSphere * ENVIRONMENT_RADIUS;
      _particles[i].velocity = Vector3.zero;
    }
  }


 	//-------------------------------------------------
  	// emit particles!
  	//-------------------------------------------------
  	private void emitParticles( Vector3 emitPosition ) 
	{
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
		// Loop through every other particle 				  
		//-------------------------------------------------------
   		for (int index = startIndex; index < endIndex; index++) 
		{
			if ( _particles[index].active ) 
			{
	      		Particle p = _particles[index];

				//------------------------------------------------------
				// reset accumulated force for this go-round
				//------------------------------------------------------
				p.accumulatedForce = new float4(ZERO, ZERO, ZERO, ZERO);

				//------------------
				// advance age
				//------------------
				p.age += deltaTime;

				//-------------------------------------------------------------------
				// Loop through every other particle to compare against this particle
				//-------------------------------------------------------------------
				for (uint i = 0; i < _numParticles; i++) 
				{
					if ( _particles[i].active )
					{
		        		if (i == index) continue; //Dont compare against self!
		
				        Particle other = _particles[i];
				        float3 	 vectorToOther = other.position - p.position;
				        float 	 distanceSquared = vectorToOther.sqrMagnitude;
		
		        		float socialRangeSquared = _species[p.species].socialRange * _species[p.species].socialRange;
		        		if ((distanceSquared < socialRangeSquared) && (distanceSquared > ZERO)) 
						{
							float distance = Mathf.Sqrt(distanceSquared);
							float3 directionToOther = vectorToOther / distance;
		
							//--------------------------------------------------------------------------
							// Accumulate forces from social attractions/repulsions to other particles
							//--------------------------------------------------------------------------
							p.accumulatedForce += (float4)(_species[p.species].socialForce * directionToOther);
							p.accumulatedForce.w += 1; //keeping track of the number of particles exerting a force
		
							//----------------------------------------
							// collisions
							//----------------------------------------
							float combinedRadius = PARTICLE_RADIUS * 2;
		          			if (distance < combinedRadius) 
							{
								float penetration = ONE - distance / combinedRadius;
								float averageCollisionForce =
		            			(
		              				_species[p.species].collisionForce +
		              				_species[other.species].collisionForce
		            			) * ONE_HALF;
		
		            			p.velocity -= deltaTime * averageCollisionForce * directionToOther * penetration;
		          			}
						}
	        		}
	      		}
	
				//---------------------------------------------
				// Apply accumulated forces to the velocity
				//---------------------------------------------
				if (p.accumulatedForce.w > 0) 
				{
					//--------------------------------------------------------------
					// NOTE _ we divide by w, which is the number of particles 
					// that contributed to the accumulated force
					//--------------------------------------------------------------
					p.velocity += deltaTime * (Vector3)p.accumulatedForce / p.accumulatedForce.w;
				}
	
				//--------------------------------------------------------------------------------------
				// apply forces to keep the particle from getting past the boundary of the environment
				//--------------------------------------------------------------------------------------
				Vector3 vectorFromHome = p.position - _homePosition;
				float distanceFromHome = vectorFromHome.magnitude;
	
				if (distanceFromHome > ENVIRONMENT_RADIUS) 
				{
					Vector3 directionFromHome = vectorFromHome / distanceFromHome;
					float force = (distanceFromHome - ENVIRONMENT_RADIUS) * BOUNDARY_FORCE;
					p.velocity -= force * directionFromHome * deltaTime;
				}
	
				//-------------------------------------------
				// dampening (kinda like air friction)
				//-------------------------------------------
				p.velocity *= (ONE - _species[p.species].drag);
	
				//------------------------------------
				// update position by velocity
				//------------------------------------
				p.position += p.velocity;
	
				//----------------------
				// fill back buffer
				//----------------------
				_backBuffer[index] = p;
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
