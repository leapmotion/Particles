Shader "Unlit/ParticlePointDisplay" {
	Properties { }
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

    Blend SrcAlpha One

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
      #include "Assets/Shaders/ParticleData.cginc"

			struct v2f {
				float4 vertex : SV_POSITION;
			};

      StructuredBuffer<Particle> _Particles;
			
			v2f vert (uint inst : SV_VertexID) {
				v2f o;
				o.vertex = UnityObjectToClipPos(_Particles[inst].position);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
        return fixed4(1, 0, 0, 0.5);
			}
			ENDCG
		}
	}
}
