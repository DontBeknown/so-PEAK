Shader "Custom/BlendedSkybox"
{
    Properties
    {
        [NoScaleOffset] _Skybox1 ("Skybox 1 (HDR)", Cube) = "white" {}
        [NoScaleOffset] _Skybox2 ("Skybox 2 (HDR)", Cube) = "white" {}
        _Blend ("Blend", Range(0, 1)) = 0.5
        _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation1 ("Rotation 1", Range(0, 360)) = 0
        _Rotation2 ("Rotation 2", Range(0, 360)) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue"="Background" 
            "RenderType"="Background" 
            "PreviewType"="Skybox" 
        }
        
        Cull Off
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            
            samplerCUBE _Skybox1;
            samplerCUBE _Skybox2;
            half _Blend;
            half _Exposure;
            float _Rotation1;
            float _Rotation2;
            
            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            // Rotation matrix for Y-axis
            float3 RotateAroundYInDegrees(float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.vertex.xyz;
                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                // Rotate texture coordinates for each skybox
                float3 texcoord1 = RotateAroundYInDegrees(i.texcoord, _Rotation1);
                float3 texcoord2 = RotateAroundYInDegrees(i.texcoord, _Rotation2);
                
                // Sample both skyboxes
                half4 color1 = texCUBE(_Skybox1, texcoord1);
                half4 color2 = texCUBE(_Skybox2, texcoord2);
                
                // Decode HDR if necessary
                #ifdef UNITY_COLORSPACE_GAMMA
                color1.rgb = pow(color1.rgb, 2.2);
                color2.rgb = pow(color2.rgb, 2.2);
                #endif
                
                // Blend between the two skyboxes
                half4 finalColor = lerp(color1, color2, _Blend);
                
                // Apply exposure
                finalColor.rgb *= _Exposure;
                
                // Output
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback Off
}
