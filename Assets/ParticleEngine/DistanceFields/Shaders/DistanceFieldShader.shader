// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/DistanceFieldShader"
{
  Properties
  {
    _MainTex ("Texture", 2D) = "white" {}
  }
  SubShader
  {
    Tags { "RenderType"="Opaque" }
    LOD 100

    ZWrite Off
    Cull Off
    Blend SrcAlpha OneMinusSrcAlpha

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      // make fog work
      #pragma multi_compile_fog
      
      #include "UnityCG.cginc"

      // Provided by our script
      uniform float4x4 _FrustumCornersES;
      uniform sampler2D _MainTex;
      uniform float4 _MainTex_TexelSize;
      uniform float4x4 _CameraInvViewMatrix;
      uniform float4 _CameraWS;

      // Input to vertex shader
      struct appdata {
        // Remember, the z value here contains the index of _FrustumCornersES to use
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };

      // Output of vertex shader / input to fragment shader
      struct v2f {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 ray : TEXCOORD1;
      };

      v2f vert (appdata v) {
        v2f o;
    
        // Index passed via custom blit function in RaymarchGeneric.cs
        half index = v.vertex.z;
        v.vertex.z = 0.1;
    
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv.xy;
    
        #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0)
            o.uv.y = 1 - o.uv.y;
        #endif

        // Get the eyespace view ray (normalized)
        o.ray = _FrustumCornersES[(int)index].xyz;

        // Transform the ray from eyespace to worldspace
        // Note: _CameraInvViewMatrix was provided by the script
        o.ray = mul(_CameraInvViewMatrix, o.ray);
        return o;
      }

      float opUnion(float d1, float d2)
      {
        return min(d1, d2);
      }

      float opSubtraction(float d1, float d2)
      {
        return max(-d1, d2);
      }

      float opIntersection(float d1, float d2)
      {
        return max(d1, d2);
      }

      // Torus
      // t.x: diameter
      // t.y: thickness
      // Adapted from: http://iquilezles.org/www/articles/distfunctions/distfunctions.htm
      float sdTorus(float3 p, float2 t)
      {
        t.x *= 0.1;
        t.y *= 0.1;
        float2 q = float2(length(p.xz) - t.x, p.y);
        return length(q) - t.y;
      }

      float sdSphere(float3 p, float s)
      {
        return length(p) - s;
      }

      float sdBox(float3 p, float3 b)
      {
        float3 d = abs(p) - b;
        return min(max(d.x, max(d.y, d.z)), 0.0) + length(max(d, 0.0));
      }

      float3 opCheapBend(float3 p)
      {
        float d = 2;
        float c = cos(d*p.x);
        float s = sin(d*p.x);
        float2x2  m = float2x2(c, -s, s, c);
        return float3(mul(m, p.xy), p.z);
      }

      float smin(float a, float b, float k)
      {
        float h = clamp(0.5 + 0.5*(b - a) / k, 0.0, 1.0);
        return lerp(b, a, h) - k*h*(1.0 - h);
      }

      float3 _LeftSpherePos;
      float _LeftSphereRadius;
      float3 _LeftPalmPos;
      float _LeftPalmRadius;

      float3 _RightSpherePos;
      float _RightSphereRadius;
      float3 _RightPalmPos;
      float _RightPalmRadius;

      // This is the distance field function.  The distance field represents the closest distance to the surface
      // of any object we put in the scene.  If the given point (point p) is inside of an object, we return a
      // negative answer.
      float map(float3 p) {
        //return sdTorus(p, float2(1, 0.2));

        float3 lsp = p - _LeftSpherePos;
        float3 lpp = p - _LeftPalmPos;
        float3 rsp = p - _RightSpherePos;
        float3 rpp = p - _RightPalmPos;

        float leftBig = sdSphere(lsp, _LeftSphereRadius);
        float rightBig = sdSphere(rsp, _RightSphereRadius);
        float leftSmall = leftBig + 0.005f;
        float rightSmall = rightBig + 0.005f;

        float leftShell = opSubtraction(leftSmall, leftBig);
        float rightShell = opSubtraction(rightSmall, rightBig);

        //float big = smin(opSubtraction(leftSmall, leftBig), opSubtraction(rightSmall, rightBig), 0.1);

        //float leftSmall = sdSphere(lsp, _LeftSphereRadius - 0.005);
        //float rightSmall = sdSphere(rsp, _RightSphereRadius - 0.005);
        //float small = smin(leftSmall, rightSmall, 0.1);

        float leftRad = sdSphere(lpp, _LeftPalmRadius);
        float rightRad = sdSphere(rpp, _RightPalmRadius);
        //float rad = smin(leftRad, rightRad, 0.01);

        float finLeft = opIntersection(leftRad, leftShell);
        float finRight = opIntersection(rightRad, rightShell);

        float final = smin(finLeft, finRight, 0.2);
        //float final = opIntersection(shell, rad);

        return final;


        //p = opCheapBend(p);
        //return sdBox(p, float3(0.2, 0.02, 0.2));




      }

      // Raymarch along given ray
      // ro: ray origin
      // rd: ray direction
      fixed4 raymarch(float3 ro, float3 rd, float s, out float t) {
        const int maxstep = 64;
        t = 0; // current distance traveled along ray

        for (int i = 0; i < maxstep; ++i) {
          float3 p = ro + rd * t; // World space position of sample
          float2 d = map(p);      // Sample of distance field (see map())

                                  // If the sample <= 0, we have hit something (see map()).
          if (d.x < 0.001 || t > 10) {
            // Simply return the number of steps taken, mapped to a color ramp.
            float perf = (float)i / maxstep;
            return fixed4(perf, 0, 0, 0.95);//   fixed4(tex2D(_ColorRamp, float2(perf, 0)).xyz, 1);
          }

          t += d;
        }

        // By this point the loop guard (i < maxstep) is false.  Therefore
        // we have reached maxstep steps.
        return fixed4(1, 0, 0, 1);// fixed4(tex2D(_ColorRamp, float2(1, 0)).xyz, 1);
      }

      fixed4 frag(v2f i, out float depth : SV_DEPTH) : SV_Target {
        // ray direction
        float3 rd = normalize(i.ray.xyz);
        // ray origin (camera position)
        float3 ro = _CameraWS;

        fixed3 col = tex2D(_MainTex,i.uv); // Color of the scene before this shader was run
        fixed4 add = raymarch(ro, rd, 0, depth);

        float3 rayEnd = ro + rd * depth;

        depth = (1.0 / depth - _ZBufferParams.w) / _ZBufferParams.z;

        // Returns final color using alpha blending
        return fixed4(col*(1.0 - add.w) + add.xyz * add.w,1.0);
      }
      ENDCG
    }
  }
}
