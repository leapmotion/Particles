Shader "SmoothLine/SmoothLineMax_Mask"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
    _Power   ("Power", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

    ColorMask A

    ZTest Off
    ZWrite Off
    Cull Off
    BlendOp Max
    Blend One one

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
      float _Power;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
        return pow(tex2D(_MainTex, i.uv).a, _Power);
			}
			ENDCG
		}
	}
}
