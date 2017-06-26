Shader "Custom/Cel Shade Gray Particles" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
    _Velocity ("Velocity", 2D) = "white" {}
    _Size     ("Size", Range(0, 0.5)) = 0.01
    _TrailLength ("Trail Length", Range(0, 10000)) = 1000
    _Brightness ("Brightness", Float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
    Pass {
      CGPROGRAM
        #pragma multi_compile COLOR_SPECIES COLOR_SPECIES_MAGNITUDE COLOR_VELOCITY
        //#pragma surface surf Standard vertex:vert noforwardadd
        #pragma vertex vert
        #pragma fragment frag
        #pragma fragmentoption ARB_precision_hint_fastest
        #pragma target 2.0

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        sampler2D _Velocity;

        struct vertInput {
          float4 pos : POSITION;
          float4 normal : NORMAL;
          float4 texcoord : TEXCOORD0;
          float4 color : COLOR;
        };

        struct fragInput {
          float4 pos : SV_POSITION;
          float4 color : TEXCOORD0;
          float litAmount : TEXCOORD1;
        };

        float4 _Colors[32];
        float _Size;
        float _TrailLength;
        float _Brightness;

        fragInput vert(in vertInput v) {
          float4 particle = tex2Dlod(_MainTex, v.texcoord);
          float4 velocity = tex2Dlod(_Velocity, v.texcoord);
          velocity.xyz *= velocity.w;

          float dir = saturate(-dot(normalize(velocity.xyz), normalize(v.pos.xyz)) - 0.2);
          v.pos.xyz -= velocity.xyz * dir * _TrailLength * (1/max(_Size, 0.001)) * 0.001;

          v.pos.xyz *= _Size;
          v.pos.xyz += particle.xyz;

          #ifdef COLOR_SPECIES
                v.color = _Colors[(int)particle.w];
          #endif
          
          #ifdef COLOR_VELOCITY
                v.color.rgb = abs(velocity.xyz) * _Brightness;
          #endif
          
          #ifdef COLOR_SPECIES_MAGNITUDE
                v.color = _Colors[(int)particle.w] * length(velocity.xyz) * _Brightness;
          #endif


          // Frag data for cel-shader.

          fragInput f;

          f.pos = UnityObjectToClipPos(v.pos);

          //half3 lightDir = half3(0.5566811, 0.6451192, 0.5233808);
          half3 lightDir = UnityWorldSpaceLightDir(v.pos);
          half litAmount = max(0, (dot(UnityObjectToWorldDir(v.normal), lightDir) + 0.7) / 2);
          f.litAmount = litAmount;

          f.color = v.color;

          return f;
        }

        #include "CelShading.cginc"
        #define CEL_SHADE_STEPS 4
        fixed4 frag(in fragInput fragIn) : SV_Target{
          return celShadedColor(CEL_SHADE_STEPS, fragIn.litAmount, fragIn.color);
        }
      ENDCG
    }
	}

}
