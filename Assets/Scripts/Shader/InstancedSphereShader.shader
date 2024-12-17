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

            // ����ÿ��ʵ���Ľṹ
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

            // �������ݵ�Shader������
            StructuredBuffer<float4> _Positions : register(t0);  // λ������
            StructuredBuffer<float4> _Colors : register(t1);     // ��ɫ����

            float _ParticleRadius;

            // ������ɫ������ʵ��λ�ú���ɫ���ݸ�Ƭ����ɫ��
            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // ��ʵ�������ж�ȡÿ��С���λ����Ϣ
                o.pos.xyz += _Positions[instanceID].xyz;  // ʵ����λ��
                o.color = _Colors[instanceID];            // ʵ������ɫ

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return i.color;  // ����ÿ��С�����ɫ
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
