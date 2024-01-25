Shader "Hidden/AddShader"
{
    /*
        In Unity, each image post-processing step is called an "image effect." 
        Each image effect consists of a C# script and a shader file that specifies a fragment shaders, 
        which computes the pixels of the output image.
    */
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _Sample;

            fixed4 frag (v2f i) : SV_Target
            {
                //return tex2D(_MainTex, i.uv); // this line is the resutlt without AA, used for comparison 
                return float4(tex2D(_MainTex, i.uv).rgb, 1.0f / (_Sample + 1.0f));
            }
            ENDCG
        }
    }
}
