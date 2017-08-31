Shader "Particle Demo/IEParticle" {
	Properties {
    _ToonRamp("Toon Ramp", 2D) = "white" {}
    _LightDir("Light Direction", Vector) = (0.577, 0.577, 0.577, 0)
	}

  CGINCLUDE
  #include "UnityCG.cginc"
  
  #pragma target 2.0

  struct appdata {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
  };

  struct v2f {
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float3 normal : NORMAL;
  };

  UNITY_INSTANCING_CBUFFER_START(MyProperties)
  UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
  UNITY_INSTANCING_CBUFFER_END

  sampler2D _ToonRamp;
  half4 _LightDir;

  v2f vert(appdata v) {
    UNITY_SETUP_INSTANCE_ID(v);

    v2f o;
    o.position = UnityObjectToClipPos(v.vertex);
    o.color = UNITY_ACCESS_INSTANCED_PROP(_Color);
    o.normal = v.normal;
    return o;
  }

  fixed4 frag(v2f i) : SV_Target {
    half NdotL = dot(i.normal, _LightDir);

    NdotL = tex2D(_ToonRamp, float2(NdotL * 0.5 + 0.5, 0));

    return i.color * NdotL;
  }
  ENDCG

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

    Pass{
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing
      ENDCG
    }
	}
}
