Shader "Unlit/DisplayShader2" {
  Properties{
    _Lerp("Prev to Curr", Range(0, 1)) = 1
    _ToonRamp("Toon Ramp", 2D) = "white" {}
    _Size("Size", Range(0, 0.5)) = 0.01
    _TrailLength("Trail Length", Range(0, 10000)) = 1000
    _Brightness("Brightness", Float) = 1
  }
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

    Blend One Zero

		Pass {
			CGPROGRAM
      #pragma multi_compile COLOR_SPECIES COLOR_SPECIES_MAGNITUDE COLOR_VELOCITY COLOR_CLUSTER
      #pragma multi_compile _ ENABLE_INTERPOLATION
      #pragma multi_compile FISH_TAIL SQUASH_TAIL
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
      #pragma target 4.0

      sampler2D _PrevPos;
      sampler2D _CurrPos;
      sampler2D _PrevVel;
      sampler2D _CurrVel;

      sampler2D _ToonRamp;

      StructuredBuffer<uint> _ClusterAssignments;

      float _ParticleCount;

      struct Input {
        float4 color : COLOR;
        float3 viewDir;
      };

      half _Lerp;
      half _Glossiness;
      half _Metallic;
      float4 _Colors[32];
      float _Size;
      float _TrailLength;
      float _Brightness;

      float nrand(float2 n) {
        return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
      }

			struct appdata {
				float4 vertex : POSITION;
        float4 texcoord : TEXCOORD0;
        float3 normal : NORMAL;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
        float4 color : COLOR;
        float lighting : TEXCOORD0;
			};
			
			v2f vert (appdata v) {
        v2f o;

#ifdef ENABLE_INTERPOLATION
        float4 particle = lerp(tex2Dlod(_PrevPos, v.texcoord), tex2Dlod(_CurrPos, v.texcoord), _Lerp);
#else
        float4 particle = tex2Dlod(_CurrPos, v.texcoord);
#endif

        float4 velocity = tex2Dlod(_CurrVel, v.texcoord);

#ifdef COLOR_SPECIES
        o.color = _Colors[(int)particle.w];
#endif

#ifdef COLOR_VELOCITY
        o.color.rgb = abs(velocity.xyz) * _Brightness;
#endif

#ifdef COLOR_SPECIES_MAGNITUDE
        o.color = _Colors[(int)particle.w] * length(velocity.xyz) * _Brightness;
#endif

#ifdef COLOR_CLUSTER
        float cluster = _ClusterAssignments[(uint)(v.texcoord.x * 4096)] * 9.34 + 0.25;
        o.color.r = nrand(float2(cluster * 3.235, cluster * 1.343));
        o.color.g = nrand(float2(cluster * 2.967, cluster * 9.173));
        o.color.b = nrand(float2(cluster * 1.972, cluster * 4.812));
#endif

        velocity.xyz *= velocity.w;

#ifdef FISH_TAIL
        float dir = saturate(-dot(normalize(velocity.xyz), normalize(v.vertex.xyz)) - 0.2);
        v.vertex.xyz -= velocity.xyz * dir * _TrailLength;
#endif

        v.vertex.xyz *= _Size;

#ifdef SQUASH_TAIL
        velocity.xyz *= _TrailLength;
        float velLength = length(velocity.xyz);
        if (velLength < 0.00001) {
          velLength = 1;
        }

        float squash = sqrt(1.0 / (1.0 + velLength));
        v.vertex.xyz *= squash;
        v.vertex.xyz += velocity.xyz * dot(velocity.xyz, v.vertex.xyz) / velLength;
#endif

        if (v.texcoord.x > _ParticleCount) {
          v.vertex.xyz += float3(0, 1000, 0);
        }

        v.vertex.xyz += particle.xyz;
				
				o.vertex = UnityObjectToClipPos(v.vertex);

        half3 lightDir = UnityWorldSpaceLightDir(v.vertex);
        o.lighting = dot(UnityObjectToWorldDir(v.normal), lightDir) * 0.5 + 0.5;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
        half shading = tex2D(_ToonRamp, float2(i.lighting, 0));
        return i.color * shading;
			}
			ENDCG
		}
	}
}
