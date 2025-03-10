Shader "MaskMaterial"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1
        _RenderQueue ("Render Queue", Int) = 3000 // 添加渲染队列作为可调节的参数，默认值为 3000（Transparent 队列）
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" } // 恢复默认的 Queue 标签

        Pass
        {
            ZWrite Off
            ZTest Less
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            uniform float _Opacity;
            uniform int _RenderQueue; // 声明_RenderQueue 变量，使其在后续代码中可被识别和使用
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 c = tex2D(_MainTex, i.uv);
                float z = i.worldPos.z;
                half4 finalColor;
                if (z > 0)
                {
                    finalColor = fixed4(c.rgb * _Color.rgb, 0);
                    // finalColor = fixed4(_Color.rgb, 0);
                }
                else
                {
                    finalColor = half4(c.rgb * _Color.rgb, c.a * _Opacity);
                    // finalColor = half4(_Color.rgb, c.a * _Opacity);
                }
                return finalColor;
            }
            ENDCG
        }
    }
}