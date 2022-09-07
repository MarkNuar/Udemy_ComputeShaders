Shader "ImageEffect/StarGlow"
{
    Properties
    {
        [HideInInspector]
        _MainTex("Texture", 2D) = "white" {}
        _BrightnessSettings("(Threshold, Intensity, Attenuation, -)", Vector) = (0.8, 1.0, 0.95, 0.0)
    }
    SubShader
    {
        CGINCLUDE

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4    _MainTex_ST;
        float4    _MainTex_TexelSize;
        float4    _BrightnessSettings;

        #define BRIGHTNESS_THRESHOLD _BrightnessSettings.x
        #define INTENSITY            _BrightnessSettings.y
        #define ATTENUATION          _BrightnessSettings.z

        ENDCG

        // STEP:0
        // Debug.
        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            fixed4 frag(v2f_img input) : SV_Target
            {
                return tex2D(_MainTex, input.uv);
            }

            ENDCG
        }
        
        // STEP 1
        Pass{
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            fixed4 frag(v2f_img input) : SV_Target
            {
                float4 color = tex2D(_MainTex, input.uv);
                return max(color - BRIGHTNESS_THRESHOLD, 0) * INTENSITY;
            }
            
            ENDCG 
        }
        
        // STEP 2 
        Pass{
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            int _Iteration;
            float2 _Offset;

            struct v2f_starglow
            {
                float4 pos      : SV_POSITION;
                half2 uv        : TEXCOORD0;
                half power      : TEXCOORD1;
                half2 offset    : TEXCOORD2;
            };

            v2f_starglow vert (appdata_img v)
            {
                v2f_starglow o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.power = pow(4, _Iteration - 1);
                o.offset = _MainTex_TexelSize.xy * _Offset * o.power;

                return o;
            }

            float4 frag(v2f_starglow input) : SV_Target
            {
                half4 color = 0;
                half2 uv = input.uv;

                for(int j = 0; j < 4; j++)
                {
                    color += saturate(tex2D(_MainTex, uv) * pow(ATTENUATION, input.power * j));
                    uv += input.offset;
                }

                return color;
            }
            ENDCG 
        }
        
        // STEP 3
        Pass
        {
            // destination = source * srcFactor + destination * destFactor 
            Blend OneMinusDstColor One 
            
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            fixed4 frag(v2f_img input) : SV_Target
            {
                return tex2D(_MainTex, input.uv);
            }
            
            ENDCG 
        }
        
        // STEP 4
        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            sampler2D _CompositeTex;
            float4 _CompositeColor;
            
            fixed4 frag(v2f_img input) : SV_Target
            {
                float4 mainColor = tex2D(_MainTex, input.uv);

                float4 compositeColor = tex2D(_CompositeTex, input.uv);

                compositeColor.rgb = (compositeColor.r + compositeColor.g + compositeColor.b) * 0.3333 * _CompositeColor;

                return saturate(mainColor + compositeColor);
            }
            
            ENDCG 
        }
    }
}