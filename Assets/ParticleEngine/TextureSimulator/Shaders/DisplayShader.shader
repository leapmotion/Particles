Shader "Custom/DisplayShader" {
  Properties {
    _Lerp     ("Prev to Curr", Range(0, 1)) = 1
    _ToonRamp ("Toon Ramp", 2D) = "white" {}
    _Size     ("Size", Range(0, 0.5)) = 0.01
    _TrailLength ("Trail Length", Range(0, 10000)) = 1000
    _Brightness ("Brightness", Float) = 1
  }
  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200
    
    CGPROGRAM
    #pragma multi_compile COLOR_SPECIES COLOR_SPECIES_MAGNITUDE COLOR_VELOCITY COLOR_CLUSTER
    #pragma multi_compile _ ENABLE_INTERPOLATION
    #pragma multi_compile FISH_TAIL SQUASH_TAIL
    #pragma surface surf CelShadingForward vertex:vert noforwardadd
    #pragma target 2.0

    sampler2D _PrevPos;
    sampler2D _CurrPos;
    sampler2D _PrevVel;
    sampler2D _CurrVel;

    sampler2D _ToonRamp;

#ifdef SHADER_API_D3D11
    StructuredBuffer<uint> _ClusterAssignments;
#endif

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

    half4 LightingCelShadingForward(SurfaceOutput  s, half3 lightDir, half atten) {
      half NdotL = dot(s.Normal, lightDir);

      NdotL = tex2D(_ToonRamp, float2(NdotL * 0.5 + 0.5, 0));

      half4 c;
      c.rgb = s.Albedo * _LightColor0.rgb * (NdotL * atten * 2);
      c.a = s.Alpha;
      return c;
    }

    void vert(inout appdata_full v) {
#ifdef ENABLE_INTERPOLATION
      float4 particle = lerp(tex2Dlod(_PrevPos, v.texcoord), tex2Dlod(_CurrPos, v.texcoord), _Lerp);
#else
      float4 particle = tex2Dlod(_CurrPos, v.texcoord);
#endif

      float4 velocity = tex2Dlod(_CurrVel, v.texcoord);

#ifdef COLOR_SPECIES
      v.color = _Colors[(int)particle.w];
#endif

#ifdef COLOR_VELOCITY
      v.color.rgb = abs(velocity.xyz) * _Brightness;
#endif

#ifdef COLOR_SPECIES_MAGNITUDE
      v.color = _Colors[(int)particle.w] * length(velocity.xyz) * _Brightness;
#endif

#ifdef COLOR_CLUSTER
      v.color.a = 1;
#ifdef SHADER_API_D3D11
      float cluster = _ClusterAssignments[(uint)(v.texcoord.x * 4096)] * 9.34 + 0.25;
      v.color.r = nrand(float2(cluster * 3.235, cluster * 1.343));
      v.color.g = nrand(float2(cluster * 2.967, cluster * 9.173));
      v.color.b = nrand(float2(cluster * 1.972, cluster * 4.812));
#else
      v.color.r = 1;
      v.color.g = 0;
      v.color.b = 0;
#endif
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
    }

    void surf (Input IN, inout SurfaceOutput  o) {
      o.Albedo = IN.color.rgb;
    }
    ENDCG
  }
  FallBack "Diffuse"
}
