Shader "LeapMotion/GraphicRenderer/Unlit/DynamicUnlit HandProximity SubtleNormals" {
  Properties {
    _Color   ("Color", Color) = (1,1,1,1)
    _MainTex ("Texture", 2D) = "white" {}
  }
  SubShader {
    Tags {"Queue"="Geometry" "RenderType"="Opaque" }

    Cull Off

    Pass {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag

      #pragma shader_feature _ GRAPHIC_RENDERER_CYLINDRICAL GRAPHIC_RENDERER_SPHERICAL
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_NORMALS
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_UV_0
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_UV_1
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_UV_2
      #pragma shader_feature _ GRAPHIC_RENDERER_VERTEX_COLORS
      #pragma shader_feature _ GRAPHIC_RENDERER_TINTING
      #pragma shader_feature _ GRAPHIC_RENDERER_BLEND_SHAPES
      #pragma shader_feature _ GRAPHIC_RENDERER_ENABLE_CUSTOM_CHANNELS

      #include "Assets/LeapMotion/Modules/GraphicRenderer/Resources/DynamicRenderer.cginc"
      #include "UnityCG.cginc"

      sampler2D _MainTex;

      // Hand proximity
      #include "Assets/AppModules/Shader Hand Data/Resources/HandData.cginc"

      // Define our own custom v2f for hand proximity.
      // This takes up the UV2 slot.
      struct custom_v2f {
        V2F_GRAPHICAL
        float3 vertex_world : TEXCOORD1;
      };
      
      custom_v2f vert (appdata_graphic_dynamic v) {
        BEGIN_V2F(v);

        custom_v2f o;
        APPLY_DYNAMIC_GRAPHICS(v, o);

        // Hand proximity
        //
        // After APPLY_DYNAMIC_GRAPHICS (check out DynamicRenderer.cginc to see precisely
        // what it does), v.vertex will contain the final world position of the vertex
        // after it has been interpreted in space relative to its anchor and has had any
        // curvature-warping applied to it.
        // o.vertex would contain that world-space v.vertex, projected into clip space.
        // Here we store this world vertex position for the pixel shader.
        o.vertex_world = v.vertex;

        return o;
      }

      float sqrDistToGlowAmount(float sqrDist) {
        float thickness = 0.007;
        float range = 0.005;
        float min = thickness * thickness;
        float max = (thickness + range) * (thickness + range);
        return Leap_Map(sqrDist, min, max, 1, 0);
      }
      
      fixed4 frag (custom_v2f i) : SV_Target {
        fixed4 color = fixed4(1,1,1,1);

#ifdef GRAPHIC_RENDERER_VERTEX_NORMALS

        // Fake, subtle "lighting" based on object normals.
        float litAmount = dot(normalize(i.normal.xyz), normalize(float3(1, 1.3, 0)));
        color = litAmount * 0.25 + color;

        // original color calculation from dynamic shader:
        //color *= abs(dot(normalize(i.normal.xyz), float3(0, 0, 1)));
#endif

#ifdef GRAPHIC_RENDERER_VERTEX_UV_0
        color *= tex2D(_MainTex, i.uv_0);
#endif

#ifdef GRAPHICS_HAVE_COLOR
        color *= i.color;
#endif

        // Hand proximity
        float sqrDistToHand = Leap_SqrDistToHand(i.vertex_world);
        float glowAmount = sqrDistToGlowAmount(sqrDistToHand);
        float4 glowColor = float4(0.39, 0.50, 0.85, 0);
        color += glowAmount * glowColor;

        return color;
      }
      ENDCG
    }
  }
}
