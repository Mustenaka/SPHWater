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

            // 定义每个实例的结构
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

            // 参数传递到Shader的数组
            StructuredBuffer<float4> _Positions : register(t0);  // 位置数组
            StructuredBuffer<float4> _Colors : register(t1);     // 颜色数组

            float _ParticleRadius;

            // 顶点着色器：将实例位置和颜色传递给片段着色器
            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // 从实例数组中读取每个小球的位置信息
                o.pos.xyz += _Positions[instanceID].xyz;  // 实例的位移
                o.color = _Colors[instanceID];            // 实例的颜色

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return i.color;  // 返回每个小球的颜色
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
