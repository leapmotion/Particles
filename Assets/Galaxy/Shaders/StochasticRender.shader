Shader "Unlit/StochasticRender" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
    _Noise   ("Noise", 2D) = "white" {}
    _NoiseOffset("Noise Offset", Int) = 0
	}

  CGINCLUDE
  #pragma multi_compile _ USE_RAMP
  #pragma multi_compile _ BY_SPEED BY_DIRECTION BY_ACCEL BY_BLACK_HOLE
  #include "UnityCG.cginc"

  struct v2f {
    float4 vertex : SV_POSITION;
    float4 color : COLOR;
  };

  sampler2D _Ramp;
  sampler2D _Noise;
  sampler2D _MainTex;
  sampler2D _PrevPosition;
  sampler2D _LastPosition;

  float4x4 _ToWorldMat;
  float _Scale;
  float _Bright;
  float _Size;
  uint _NoiseOffset;

  float _PreScalar;
  float _PostScalar;

  v2f vert(uint id : SV_VertexID) {
    float4 uv;
    uv.x = (id / 512) / 512.0;
    uv.y = (id % 512) / 512.0;
    uv.z = 0;
    uv.w = 0;

    float4 position = tex2Dlod(_MainTex, uv);
    float4 prevPosition = tex2Dlod(_PrevPosition, uv);
    float4 lastPosition = tex2Dlod(_LastPosition, uv);

    float4 worldPosition = mul(_ToWorldMat, float4(position.xyz, 1));

    //uint id2 = (id + _NoiseOffset) % (32 * 32);
    //uv.x = (id2 / 32) / 32.0;
    //uv.y = (id2 % 32) / 32.0;
    //uv.z = 0;
    //uv.w = 0;
    //worldPosition.xyz += tex2Dlod(_Noise, uv) * _Size;

    float distToCamera = length(_WorldSpaceCameraPos - worldPosition);

    float screenSpaceWidth = _Scale * _Size * 400 / distToCamera;
    float brightness = _Bright * screenSpaceWidth * screenSpaceWidth;

    v2f o;
    o.vertex = mul(UNITY_MATRIX_VP, worldPosition);
    o.color = _PreScalar;

#if BY_SPEED
    o.color = _PreScalar * length(prevPosition - position);
#endif

#if BY_DIRECTION
    float3 delta = abs(prevPosition - position);
    float maxC = max(max(delta.x, delta.y), delta.z);

    delta /= maxC;
    delta = pow(delta, 2);
    delta *= min(1, maxC);

    o.color = float4(delta * _PreScalar, 1);
#endif

#if BY_ACCEL
    float4 vel0 = position - prevPosition;
    float4 vel1 = prevPosition - lastPosition;
    o.color = _PreScalar * length(vel0 - vel1);
#endif

#if BY_BLACK_HOLE
    o.color = position.w;
#endif

#if USE_RAMP
    float2 uv2 = o.color.rg;
    o.color = tex2Dlod(_Ramp, float4(uv2, 0, 0));
#endif

    o.color *= brightness * _PostScalar;

    return o;
  }

  fixed4 frag(v2f i) : SV_Target {
    return fixed4(i.color);
  }
  ENDCG

	SubShader {
    Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

    ZWrite Off
    Cull Off
    Blend One One

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
