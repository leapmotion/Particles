Shader "Unlit/GroupsharedDebug"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
  }
  SubShader
  {
    Tags { "RenderType" = "Opaque" }
    LOD 100

    Pass
    {
      CGPROGRAM
      #pragma target 5.0
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      StructuredBuffer<float4> _Positions;

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (uint id : SV_VertexID)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(_Positions[id].xyz);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return float4(0, 1, 1, 1);
			}
			ENDCG
		}
	}
}
