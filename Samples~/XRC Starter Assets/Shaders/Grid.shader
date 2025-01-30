Shader "XR Collaboratory/Grid"
// Based on a shader found in Unity's VR template
{
    Properties
    {
        _GridColour ("Grid Colour", color) = (1, 1, 1, 1)
        _BaseColour ("Base Colour", color) = (1, 1, 1, 0)
        _GridSpacing ("Grid Spacing", float) = 1
        _LineThickness ("Line Thickness", float) = .1
        _ODistance ("Start Transparency Distance", float) = 5
        _TDistance ("Full Transparency Distance", float) = 10
        _Scale ("Scale", float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 objectPos : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            fixed4 _GridColour;
            fixed4 _BaseColour;
            float _GridSpacing;
            float _LineThickness;
            float _ODistance;
            float _TDistance;
            float _Scale;
            v2f vert (appdata_full v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Keep vertex in object space for grid
                o.objectPos = v.vertex.xyz;

                // Transform vertex to clip space for rendering
                o.vertex = UnityObjectToClipPos(v.vertex);

                // UV coordinates directly from object space
                o.uv = v.vertex.xz / _GridSpacing;

                // View direction in world space
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);

                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                // Grid pattern calculation from object space UVs
                float2 wrapped = frac(i.uv) - 0.5f;
                float2 range = abs(wrapped);
                float2 speeds = fwidth(i.uv);
                float2 pixelRange = range/speeds;
                float lineWeight = saturate(min(pixelRange.x, pixelRange.y) - _LineThickness);
                half4 param = lerp(_GridColour, _BaseColour, lineWeight);

                // Calculate camera-relative view distance
                float3 worldCamPos = _WorldSpaceCameraPos;
                float3 worldPos = mul(unity_ObjectToWorld, float4(i.objectPos, 1.0)).xyz;
                float viewDist = distance(worldCamPos, worldPos);

                // Get camera's scale from the projection matrix
                // float camScale = abs(UNITY_MATRIX_P[1][1]); // Gets vertical field of view scale

                // Adjust distances inversely with camera scale
                float adjustedODistance = _ODistance * _Scale;
                float adjustedTDistance = _TDistance * _Scale;


                // Calculate fade based on adjusted distances
                // float fade = 1.0 - saturate((viewDist - adjustedODistance) / (adjustedTDistance - adjustedODistance));
                // param.a *= fade;

                 float falloff = saturate((viewDist - adjustedODistance) / (adjustedTDistance - adjustedODistance));
                param.a *= (1.0f - falloff);

                return param;
            }
            ENDCG
        }
    }
}