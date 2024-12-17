Shader "Custom/InstancedSphereShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" { }
        _Color ("Color", Color) = (1,1,1,1)
        _ParticleRadius ("Particle Radius", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : POSITION;
                float4 color : COLOR;
            };

            // parma Shader array
            StructuredBuffer<float4> _Positions : register(t0); 
            StructuredBuffer<float4> _Colors : register(t1);

            float _ParticleRadius;

            // Vertex shader: Passes the instance position and color to the fragment shader
            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Read the position information for each ball from an array of instances
                o.pos.xyz += _Positions[instanceID].xyz;
                o.color = _Colors[instanceID];

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return i.color;  // return the color of the particle
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
