Shader "Galaxy/PostProcess" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
    _Stars   ("Stars",   2D) = "white" {}
	}

  CGINCLUDE
  #pragma multi_compile _ BOX_FILTER
  #include "UnityCG.cginc"

  struct appdata {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
  };

  struct v2f {
    float4 vertex : SV_POSITION;
#if BOX_FILTER
    float2 uv[9] : TEXCOORD0;
#else
    float2 uv[1] : TEXCOORD0;
#endif
  };

  sampler2D _Stars;
  sampler2D _MainTex;
  float4 _MainTex_TexelSize;

  float _AdjacentFilter;
  float _DiagonalFilter;

  v2f vert(appdata v) {
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv[0] = v.uv;

#if BOX_FILTER
    float2 dx = float2(_MainTex_TexelSize.x, 0);
    float2 dy = float2(0, _MainTex_TexelSize.y);

    o.uv[1] = v.uv + dx;
    o.uv[2] = v.uv - dx;
    o.uv[3] = v.uv + dy;
    o.uv[4] = v.uv - dy;

    o.uv[5] = v.uv + dx + dy;
    o.uv[6] = v.uv - dx + dy;
    o.uv[7] = v.uv + dx - dy;
    o.uv[8] = v.uv - dx - dy;
#endif

    return o;
  }

  fixed4 getStars(v2f i) {
    fixed4 col = tex2D(_Stars, i.uv[0]);

#if BOX_FILTER
    col = max(col, saturate(tex2D(_MainTex, i.uv[1])) * _AdjacentFilter);
    col = max(col, saturate(tex2D(_MainTex, i.uv[2])) * _AdjacentFilter);
    col = max(col, saturate(tex2D(_MainTex, i.uv[3])) * _AdjacentFilter);
    col = max(col, saturate(tex2D(_MainTex, i.uv[4])) * _AdjacentFilter);

    col = max(col, saturate(tex2D(_MainTex, i.uv[5])) * _DiagonalFilter);
    col = max(col, saturate(tex2D(_MainTex, i.uv[6])) * _DiagonalFilter);
    col = max(col, saturate(tex2D(_MainTex, i.uv[7])) * _DiagonalFilter);
    col = max(col, saturate(tex2D(_MainTex, i.uv[8])) * _DiagonalFilter);
#endif

    return col;
  }

  fixed4 frag_none(v2f i) : SV_Target{
    return getStars(i) + tex2D(_MainTex, i.uv[0]);
  }
  ENDCG

	SubShader {
		Cull Off 
    ZWrite Off 
    ZTest Always

    // Pass 0: No post process
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_none
			ENDCG
		}
	}
}
