Shader "Hidden/AddShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha

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
				float2 uv_MainTex : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Size;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.uv_MainTex = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{	
				float3 lum = float3(0.2125, 0.7154, 0.0721);
				float mc00 = dot(tex2D(_MainTex, i.uv_MainTex - fixed2(1, 1) / _Size).rgb, lum);
				float mc10 = dot(tex2D(_MainTex, i.uv_MainTex - fixed2(0, 1) / _Size).rgb, lum);
				float mc20 = dot(tex2D(_MainTex, i.uv_MainTex - fixed2(-1, 1) / _Size).rgb, lum);
				float mc01 = dot(tex2D(_MainTex, i.uv_MainTex - fixed2(1, 0) / _Size).rgb, lum);
				float mc11mc = dot(tex2D(_MainTex, i.uv_MainTex).rgb, lum);
				float mc21 = dot(tex2D(_MainTex, i.uv_MainTex - fixed2(-1, 0) / _Size).rgb, lum);
				float mc02 = dot(tex2D(_MainTex, i.uv_MainTex - fixed2(1, -1) / _Size).rgb, lum);
				float mc12 = dot(tex2D(_MainTex, i.uv_MainTex - fixed2(0, -1) / _Size).rgb, lum);
				float mc22 = dot(tex2D(_MainTex, i.uv_MainTex - fixed2(-1, -1) / _Size).rgb, lum);
				float GX = -1 * mc00 + mc20 + -2 * mc01 + 2 * mc21 - mc02 + mc22;
				float GY = mc00 + 2 * mc10 + mc20 - mc02 - 2 * mc12 - mc22;
				float G = abs(GX) + abs(GY);
				float4 c = 0;
				c = length(float2(GX, GY));
				/*
				*this part is about blur edge
				*/
				float4 cc = float4(tex2D(_MainTex, i.uv));
				if (c.x < 0.2)
				{
					return cc;
				}else{
					 
					//roated
					float4 c0 = tex2D(_MainTex, i.uv_MainTex + fixed2(0.2 / 2, 0.8) / _Size);
					float4 c1 = tex2D(_MainTex, i.uv_MainTex + fixed2(0.8 / 2, -0.2) / _Size);
					float4 c2 = tex2D(_MainTex, i.uv_MainTex + fixed2(-0.2 / 2, -0.8) / _Size);
					float4 c3 = tex2D(_MainTex, i.uv_MainTex + fixed2(-0.8 / 2, 0.2) / _Size);

					//float2 n = float2(GX, GY);
					//n *= 1 / _Size / c.x;
					////random
					//float2 randUV = 0;
					//randUV = rand(float2(n.x, n.y));
					//float4 c0 = tex2D(_MainTex, i.uv_MainTex + float2(randUV.x / 2, randUV.y) / _Size);
					//randUV = rand(float2(-n.x, n.y));
					//float4 c1 = tex2D(_MainTex, i.uv_MainTex + float2(randUV.x / 2, randUV.y) / _Size);
					//randUV = rand(float2(n.x, -n.y));
					//float4 c2 = tex2D(_MainTex, i.uv_MainTex + float2(randUV.x / 2, randUV.y) / _Size);
					//randUV = rand(float2(-n.x, -n.y));
					//float4 c3 = tex2D(_MainTex, i.uv_MainTex + float2(randUV.x / 2, randUV.y) / _Size);

					////Gird

					//float4 c0 = tex2D(_MainTex, i.uv_MainTex + fixed2(0.5, 1) / _Size);
					//float4 c1 = tex2D(_MainTex, i.uv_MainTex + fixed2(-0.5, 1) / _Size);
					//float4 c2 = tex2D(_MainTex, i.uv_MainTex + fixed2(0.5, -1) / _Size);
					//float4 c3 = tex2D(_MainTex, i.uv_MainTex + fixed2(-0.5, -1) / _Size);
					cc = (cc + c0 + c1 + c2 + c3) *0.2;
					return cc;
				}
				 
			}
			ENDCG
		}
	}
}
