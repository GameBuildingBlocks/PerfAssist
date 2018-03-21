// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/GraphIt"
{
	Properties
	{
        _Color ("Main Color", Color) = (1,1,1,1)
	}
	
	SubShader
	{
        Pass
		{
			Tags { "Queue" = "Overlay" }
			Blend SrcAlpha OneMinusSrcAlpha
			Lighting Off
			ZWrite Off
			ZTest Always


            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			
            #include "UnityCG.cginc"
			
			fixed4 _Color;
		
            struct v2f {
                float4 pos : SV_POSITION;
				float4 color : COLOR;
            };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos (v.vertex);
				o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
