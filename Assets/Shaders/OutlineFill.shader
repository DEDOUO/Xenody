Shader "Custom/Outline Fill" {
    Properties {
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 0
        _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("Outline Width", Range(0, 10)) = 2
    }

    SubShader {
        Tags {
            "Queue" = "Transparent+110"
            "RenderType" = "Transparent"
            "DisableBatching" = "True"
        }

        Pass {
            Name "Fill"
            Cull Off
            ZTest [_ZTest]
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            Stencil {
                Ref 1
                Comp NotEqual
            }

            CGPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 smoothNormal : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            uniform fixed4 _OutlineColor;
            uniform float _OutlineWidth;
            uniform float4x4 _WorldToViewMatrix;  // 添加世界到视图矩阵

            v2f vert(appdata input) {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 normal = any(input.smoothNormal) ? input.smoothNormal : input.normal;
                float4 worldPos = mul(unity_ObjectToWorld, input.vertex);  // 获取世界空间位置
                float3 viewPos = mul(_WorldToViewMatrix, worldPos).xyz;  // 转换到视图空间
                float3 viewNormal = normalize(mul((float3x3)_WorldToViewMatrix, normal));  // 转换法线到视图空间

                // 根据世界空间宽度计算偏移量
                float3 offset = viewNormal * _OutlineWidth;
                float4 offsetPos = float4(viewPos + offset, 1.0);

                output.position = UnityViewToClipPos(offsetPos);
                output.color = _OutlineColor;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target {
                return input.color;
            }
            ENDCG
        }
    }
}