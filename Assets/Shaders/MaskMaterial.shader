Shader "MaskMaterial"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1 // 添加这一行，用于控制透明度，默认值为1（完全不透明）
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent"  "Queue" = "Transparent" }
        // Tags { "RenderType" = "Opaque"  "Queue" = "Geometry" }

        Pass
        {
            // ZWrite On
            ZWrite Off
            ZTest Less
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // 声明新添加的_Opacity变量，使其在后续代码中可被识别和使用
            uniform float _Opacity; 

            // 包含必要的头文件
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
                    finalColor = fixed4(c.rgb, 0); // Z轴坐标大于0的部分，透明度保持为0
                }
                else
                {
                    // 根据新的透明度属性来设置透明度，这里将纹理颜色的alpha值乘以_Opacity属性值
                    finalColor = half4(c.rgb, c.a * _Opacity); 
                }
                return finalColor;
            }
            ENDCG
        }
    }
}