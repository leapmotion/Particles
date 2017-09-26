using UnityEngine;
using System.Collections;

public class Main : MonoBehaviour 
{
	private const float ZERO	 	= 0.0f;
	private const float ONE_HALF 	= 0.5f;
	private const float ONE			= 1.0f;
	private const int	NULL		= -1;

	struct SculptingTool
	{
		public GameObject 	gameObject;
		public Color		color;
		public float		duration;
		public float		startTime;
		public float		length;
		public float		amp;
		public float		radius;
		public Vector3		direction;
	} 

	private SpringyClay 	_springyClay = null;
	private StickyClay 		_stickyClay  = null;
	private SculptingTool 	_sculptingTool;

	//---------------------------------------------
	// initialize
	//---------------------------------------------
	void Start () 
	{		
		//---------------------------------------------
		// initialize Clay
		//---------------------------------------------
		_springyClay = GetComponent<SpringyClay>();
		_stickyClay  = GetComponent<StickyClay>();

		//---------------------------------------------
		// initialize sculpting tool
		//---------------------------------------------
		_sculptingTool.radius 	= 0.1f;
		_sculptingTool.duration = 1.5f;
		_sculptingTool.length 	= 0.15f;
		_sculptingTool.amp    	= 0.1f;
		_sculptingTool.color 	= new Color( 0.9f, 0.8f, 0.7f );

		_sculptingTool.startTime = Time.time;
		_sculptingTool.direction = Vector3.right;

		Material photoMaterial = (Material)Resources.Load( "Whatever" );
		Texture2D  photoTexture = Resources.Load( "clay_tool" ) as Texture2D;
		_sculptingTool.gameObject = GameObject.CreatePrimitive( PrimitiveType.Sphere );
		_sculptingTool.gameObject.GetComponent<MeshRenderer>().material = photoMaterial;
		_sculptingTool.gameObject.GetComponent<Renderer>().material.mainTexture = photoTexture;
		_sculptingTool.gameObject.transform.localScale = new Vector3( _sculptingTool.radius, _sculptingTool.radius, _sculptingTool.radius );			
		_sculptingTool.gameObject.transform.position = Vector3.zero;
	}


	//---------------------------------------------
	// update
	//---------------------------------------------
	void Update() 
	{
		//_sculptingTool.gameObject.transform.localScale = new Vector3( ZERO, ZERO, ZERO );
		updateSculptingTool();
	}




	//---------------------------------------------
	// update sculpting tool
	//---------------------------------------------
	void updateSculptingTool() 
	{
		//--------------------------------------------------------------------------
		// periodically reset sculpting tool for the next jab
		//--------------------------------------------------------------------------
		if ( Time.time > _sculptingTool.startTime + _sculptingTool.duration )
		{
			_sculptingTool.startTime = Time.time;

			_sculptingTool.direction = 
			new Vector3
			( 
				Mathf.Sin( Time.time * 1.0f ), 
				Mathf.Cos( Time.time * 1.4f ), 
				-1.0f + Random.value * 2.0f
			);

			_sculptingTool.direction.Normalize();
			_sculptingTool.radius = 0.1f + 0.3f * Random.value;
		}

		//--------------------------------------------------------------------------
		// apply the jab action
		//--------------------------------------------------------------------------
		float timeFraction = ( Time.time - _sculptingTool.startTime ) / _sculptingTool.duration;
		
		_sculptingTool.gameObject.transform.position = _sculptingTool.length * _sculptingTool.direction;
		_sculptingTool.gameObject.transform.position += _sculptingTool.direction * _sculptingTool.amp * Mathf.Cos( timeFraction * Mathf.PI * 2.0f );

		float m = 0.3f;
		float mm = ONE - m;
		float r = _sculptingTool.radius;

		if ( timeFraction < m )
		{
			float f = timeFraction / m;
			r *= Mathf.Sin( f * Mathf.PI * ONE_HALF );
		}
		else if ( timeFraction > mm )
		{
			float f = ( timeFraction - mm ) / m;
			r *= Mathf.Cos( f * Mathf.PI * ONE_HALF );
		}

		_sculptingTool.gameObject.transform.localScale = new Vector3( r, r, r );
		
		_springyClay.setScluptingManipulator( _sculptingTool.gameObject.transform.position, r );
		_stickyClay.setScluptingManipulator ( _sculptingTool.gameObject.transform.position, r );
	}

}
