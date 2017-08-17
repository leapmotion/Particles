Shader "Particle Demo/DisplayShader Instanced" {
  Properties {
    _ColorA   ("Color A", 2D) = "white" {}
    _ColorB   ("Color B", 2D) = "white" {}
    _ColorLerp("Color Lerp", Range(0, 1)) = 1
    _Lerp     ("Prev to Curr", Range(0, 1)) = 1
    _ToonRamp ("Toon Ramp", 2D) = "white" {}
    _TailRamp ("Tail Ramp", 2D) = "white" {}
    _Size     ("Size", Range(0, 0.5)) = 0.01
    _TrailLength ("Trail Length", Range(0, 10000)) = 1000
    _Brightness ("Brightness", Float) = 1
  }

  CGINCLUDE
  #include "UnityCG.cginc"
  #pragma multi_compile COLOR_SPECIES COLOR_SPECIES_MAGNITUDE COLOR_VELOCITY
  #pragma multi_compile _ ENABLE_INTERPOLATION
  #pragma multi_compile FISH_TAIL SQUASH_TAIL
  #pragma multi_compile _ COLOR_LERP
  #pragma target 2.0

  struct appdata {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
  };

  struct v2f {
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float3 normal : NORMAL;
    float3 lightDir : TEXCOORD0;
  };

  sampler2D _ParticlePositions;
  sampler2D _ParticlePrevPositions;

  sampler2D _ParticleVelocities;

  sampler2D _TailRamp;
  sampler2D _ToonRamp;

  sampler2D _ColorA;
  sampler2D _ColorB;

  uint _InstanceOffset;

  half _Lerp;
  half _ColorLerp;
  half _Glossiness;
  half _Metallic;

  half _Size;
  half _TrailLength;
  half _Brightness;

  float nrand(float2 n) {
    return frac(sin(dot(n.xy, float2(12.9898, 78.233)))* 43758.5453);
  }

  void sampleParticle(half4 texcoord, out float4 particle, out float4 velocity) {
#ifdef ENABLE_INTERPOLATION
    particle = lerp(tex2Dlod(_ParticlePositions, texcoord), tex2Dlod(_ParticlePrevPositions, texcoord), _Lerp);
#else
    particle = tex2Dlod(_ParticlePositions, texcoord);
#endif

    velocity = tex2Dlod(_ParticleVelocities, texcoord) * 0.01;
  }

  void calculateParticleColor(inout half4 rawColor, half4 velocity) {
#ifdef COLOR_VELOCITY
    rawColor.rgb = abs(velocity.xyz) * _Brightness;
    rawColor.a = 1;
#endif

#ifdef COLOR_SPECIES_MAGNITUDE
    rawColor *= length(velocity.xyz) * _Brightness;
#endif
  }

  void calculateParticleShape(inout float4 vertex, float4 velocity) {
#ifdef FISH_TAIL
    float speed = length(velocity.xyz);
    float3 velDir = speed < 0.00001 ? float3(0, 0, 0) : velocity.xyz / speed;
    float trailLength = _TrailLength * tex2Dlod(_TailRamp, float4(speed * 100, 0, 0, 0)).a / 100;
    float vertFactor = saturate(-dot(velDir, normalize(vertex.xyz)) - 0.2);
    vertex.xyz -= velDir * vertFactor * trailLength;
#endif

    vertex.xyz *= _Size;

#ifdef SQUASH_TAIL
    velocity.xyz *= _TrailLength;
    float velLength = length(velocity.xyz);
    if (velLength < 0.00001) {
      velLength = 1;
    }

    float squash = sqrt(1.0 / (1.0 + velLength));
    vertex.xyz *= squash;
    vertex.xyz += velocity.xyz * dot(velocity.xyz, vertex.xyz) / velLength;
#endif
  }

  int _Threshold;

  v2f vert(appdata v) {
    float4 texcoord = v.texcoord;

#ifdef COLOR_LERP
    half4 colorA = tex2Dlod(_ColorA, v.texcoord);
    half4 colorB = tex2Dlod(_ColorB, v.texcoord);
    half4 color = lerp(colorA, colorB, _ColorLerp);
#else
    half4 color = tex2Dlod(_ColorA, v.texcoord);
#endif

    float4 particle, velocity;
    sampleParticle(texcoord, particle, velocity);

    calculateParticleColor(color, velocity);

    calculateParticleShape(v.vertex, velocity);
      
    v.vertex.xyz *= color.a;
    v.vertex.xyz += particle.xyz;

    v2f o;
    o.position = UnityObjectToClipPos(v.vertex);
    o.normal = v.normal;
    o.color = color;
    o.lightDir = UnityWorldSpaceLightDir(v.vertex);
    return o;
  }

  fixed4 frag(v2f i) : SV_Target {
    half NdotL = dot(i.normal, i.lightDir);

    NdotL = tex2D(_ToonRamp, float2(NdotL * 0.5 + 0.5, 0));

    return i.color *NdotL;
  }
  ENDCG

  SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200
    
    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
    }
  }
  FallBack Off
}
