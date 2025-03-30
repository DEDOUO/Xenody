Shader "Sprites/Outline2"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Main texture Tint", Color) = (1,1,1,1)

        [Header(General Settings)]
        [MaterialToggle] _OutlineEnabled ("Outline Enabled", Float) = 1
        [MaterialToggle] _ConnectedAlpha ("Connected Alpha", Float) = 0
        [HideInInspector] _AlphaThreshold ("Alpha clean", Range (0, 1)) = 0
        _Thickness ("World Space Width", float) = 0.1
        [KeywordEnum(Solid, Gradient, Image)] _OutlineMode("Outline mode", Float) = 0
        [KeywordEnum(Contour, Frame)] _OutlineShape("Outline shape", Float) = 0
        [KeywordEnum(Inside under sprite, Inside over sprite, Outside)] _OutlinePosition("Outline Position (Frame Only)", Float) = 0

        [Header(Solid Settings)]
        _SolidOutline ("Outline Color Base", Color) = (1,1,1,1)

        [Header(Gradient Settings)]
        _GradientOutline1 ("Outline Color 1", Color) = (1,1,1,1)
        _GradientOutline2 ("Outline Color 2", Color) = (1,1,1,1)
        _Weight ("Weight", Range (0, 1)) = 0.5
        _Angle ("Gradient Angle (General gradient Only)", float) = 45

        [Header(Image Settings)]
        _FrameTex ("Frame Texture", 2D) = "white" {}
        _ImageOutline ("Outline Color Base", Color) = (1,1,1,1)
        [KeywordEnum(Stretch, Tile)] _TileMode("Frame mode", Float) = 0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma exclude_renderers d3d11_9x

            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldScale : TEXCOORD1; // 新增世界空间缩放参数
            };

            fixed4 _Color;
            fixed _Thickness;
            fixed _OutlineEnabled;
            fixed _ConnectedAlpha;
            fixed _OutlineShape;
            fixed _OutlinePosition;
            fixed _OutlineMode;

            fixed4 _SolidOutline;

            fixed4 _GradientOutline1;
            fixed4 _GradientOutline2;
            fixed _Weight;
            fixed _AlphaThreshold;
            fixed _Angle;

            fixed4 _ImageOutline;
            fixed _TileMode;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;

                // 计算世界空间缩放
                float3 scale;
                scale.x = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[0].y, unity_ObjectToWorld[0].z));
                scale.y = length(float3(unity_ObjectToWorld[1].x, unity_ObjectToWorld[1].y, unity_ObjectToWorld[1].z));
                OUT.worldScale = scale.xy;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            sampler2D _MainTex;
            sampler2D _FrameTex;
            float4 _MainTex_TexelSize;
            float4 _FrameTex_TexelSize;
            float4 _FrameTex_ST;

            fixed4 SampleSpriteTexture (float2 uv, float2 worldScale)
            {
                float2 offsets;
                if((_OutlinePosition != 2 && _OutlineShape == 1) || _OutlineEnabled == 0)
                {
                    offsets = float2(0, 0);
                }
                else
                {
                    // 根据世界空间缩放调整偏移量
                    float localThicknessX = _Thickness / worldScale.x;
                    float localThicknessY = _Thickness / worldScale.y;
                    offsets = float2(
                        localThicknessX * 2 * _MainTex_TexelSize.x,
                        localThicknessY * 2 * _MainTex_TexelSize.y
                    );
                }

                float2 bigsize = float2(_MainTex_TexelSize.z, _MainTex_TexelSize.w);
                float2 smallsize = float2(_MainTex_TexelSize.z - offsets.x, _MainTex_TexelSize.w - offsets.y);

                float2 uv_changed = float2(
                    uv.x * bigsize.x / smallsize.x - 0.5 * offsets.x / smallsize.x,
                    uv.y * bigsize.y / smallsize.y - 0.5 * offsets.y / smallsize.y
                );

                if(uv_changed.x < 0 || uv_changed.x > 1 || uv_changed.y < 0 || uv_changed.y > 1)
                {
                    return float4(0, 0, 0, 0);
                }

                fixed4 color = tex2D (_MainTex, uv_changed);
                return color;
            }

            bool CheckOriginalSpriteTexture (float2 uv, bool ifZero, float2 worldScale)
            {
                float thicknessX = _Thickness / (worldScale.x * _MainTex_TexelSize.z);
                float thicknessY = _Thickness / (worldScale.y * _MainTex_TexelSize.w);
                
                float alphaThreshold = _AlphaThreshold;
                int steps = 100;
                float angle_step = 6.28319 / steps;

                bool outline = false;
                float alphaCounter = 0;

                // 快速边界检查
                if(!ifZero)
                {
                    outline = 
                        SampleSpriteTexture(uv + float2(0, thicknessY), worldScale).a > alphaThreshold ||
                        SampleSpriteTexture(uv + float2(0, -thicknessY), worldScale).a > alphaThreshold ||
                        SampleSpriteTexture(uv + float2(thicknessX, 0), worldScale).a > alphaThreshold ||
                        SampleSpriteTexture(uv + float2(-thicknessX, 0), worldScale).a > alphaThreshold;
                    if(outline) return true;
                }

                // 详细角度检查
                for(int i = 0; i < steps; i++)
                {
                    float angle = i * angle_step;
                    float2 offset = float2(
                        thicknessX * cos(angle),
                        thicknessY * sin(angle)
                    );
                    
                    fixed4 sampled = SampleSpriteTexture(uv + offset, worldScale);
                    
                    if(ifZero)
                    {
                        if(sampled.a == 0) alphaCounter++;
                        if(alphaCounter >= _AlphaThreshold * 10) return true;
                    }
                    else
                    {
                        if(sampled.a > alphaThreshold) return true;
                    }
                }

                return ifZero ? (alphaCounter >= _AlphaThreshold * 10) : false;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 使用世界空间缩放调整厚度
                float thicknessX = _Thickness / (IN.worldScale.x * _MainTex_TexelSize.z);
                float thicknessY = _Thickness / (IN.worldScale.y * _MainTex_TexelSize.w);

                fixed4 c = SampleSpriteTexture(IN.texcoord, IN.worldScale) * IN.color;
                c.rgb *= c.a;

                if(_OutlineEnabled)
                {
                    bool isOutline = false;
                    
                    if(_OutlineShape == 0) // Contour
                    {
                        if(_OutlinePosition == 2) // Outside
                            isOutline = (c.a == 0) && CheckOriginalSpriteTexture(IN.texcoord, false, IN.worldScale);
                        else // Inside
                            isOutline = (c.a != 0) && CheckOriginalSpriteTexture(IN.texcoord, true, IN.worldScale);
                    }
                    else // Frame
                    {
                        isOutline = 
                            IN.texcoord.y + thicknessY > 1 ||
                            IN.texcoord.y - thicknessY < 0 ||
                            IN.texcoord.x + thicknessX > 1 ||
                            IN.texcoord.x - thicknessX < 0;
                    }

                    if(isOutline)
                    {
                        fixed4 outlineC = fixed4(0,0,0,0);
                        
                        if(_OutlineMode == 0) // Solid
                        {
                            outlineC = _SolidOutline;
                            outlineC.rgb *= outlineC.a;
                        }
                        else if(_OutlineMode == 1) // Gradient
                        {
                            float angleRad = radians(_Angle);
                            float2 dir = float2(cos(angleRad), sin(angleRad));
                            float gradient = dot(IN.texcoord - 0.5, dir) + 0.5;
                            outlineC = lerp(_GradientOutline1, _GradientOutline2, saturate(gradient + _Weight - 0.5));
                            outlineC.rgb *= outlineC.a;
                        }
                        else if(_OutlineMode == 2) // Image
                        {
                            float2 frameUV = IN.texcoord;
                            if(_TileMode == 1)
                            {
                                frameUV = frac(frameUV * _MainTex_TexelSize.zw / _FrameTex_TexelSize.zw);
                            }
                            outlineC = tex2D(_FrameTex, frameUV) * _ImageOutline;
                            outlineC.rgb *= outlineC.a;
                        }

                        if(_ConnectedAlpha) outlineC.a *= _Color.a;
                        return outlineC;
                    }
                }

                return c;
            }
        ENDCG
        }
    }
}