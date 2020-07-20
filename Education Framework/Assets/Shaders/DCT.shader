Shader "Unlit/DCT"
{
    Properties
    {
        _width("width", Int) = 1920
        _height("height", Int) = 1080
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            int _width, _height;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                const float pi = 3.141592653589793238462;

                float value = cos(_width * i.uv.x) * 0.5 + 0.5;

                return fixed4(value, value, value, 1.0);
            }
            ENDCG
        }
    }
}
