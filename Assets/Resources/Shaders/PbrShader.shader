Shader "My_shader/pbr"
{
	Properties
	{
		_Albedo ("固有色", 2D) = "white" {}
		_Normal("法线", 2D) = "white" {}
		_MetalnessMap("金属度", 2D) = "white" {}
		_RoughnessMap("粗糙度", 2D) = "white" {}
		_AoMap("环境遮蔽", 2D) = "white" {}
		_BumpScale("法线深度", Range(0.0, 10)) = 0 

		_Metalness("金属度", Range(0.0, 1)) = 0.5 
		_Roughness("粗糙度", Range(0.0, 1)) = 1 

		_Specular("高光颜色", Color) = (1,1,1,1)
	}
	SubShader
	{
		
		LOD 100

		Pass
		{
			Tags { 
				"RenderType"="Opaque"
				"LightMode" = "ForwardBase"
			}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				fixed4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed3 normal:NORMAL;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				fixed4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 TtoW0 : TEXCOORD1;  
				float4 TtoW1 : TEXCOORD2;  
				float4 TtoW2 : TEXCOORD3; 
			};

			sampler2D _Albedo;
			sampler2D _Normal;
			sampler2D _MetalnessMap;
			sampler2D _RoughnessMap;
			sampler2D _AoMap;

			fixed _BumpScale;

			fixed _Metalness;
			fixed _Roughness;

			fixed _key;

			fixed3 _Specular;
			samplerCUBE _Cubemap;
			
			fixed4 _Albedo_ST;

			//使用Unity定义的变量
            uniform fixed4 _LightColor0;
			
			v2f vert (appdata v)
			{
				v2f o;
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _Albedo);
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;  
				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);  
				fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);  
				fixed3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;
				//转换矩阵本身也包含信息如：worldpos
				//切线到世界坐标的 转换矩阵
				o.TtoW0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
				o.TtoW1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
				o.TtoW2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
				return o;
			}

			fixed3 fresnelSchlick(float cosTheta, fixed3 F0)
			{
				return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
			}

			fixed DistributionGGX(fixed3 N, fixed3 H, fixed roughness)
			{
			    fixed a      = roughness*roughness;
			    fixed a2     = a*a;
			    fixed NdotH  = max(dot(N, H), 0.0);
			    fixed NdotH2 = NdotH*NdotH;

			    fixed nom   = a2;
			    fixed denom = (NdotH2 * (a2 - 1.0) + 1.0);
			    denom = UNITY_PI * denom * denom;

			    return nom / denom;
			}

			fixed GeometrySchlickGGX(fixed NdotV, fixed roughness)
			{
			    fixed r = (roughness + 1.0);
			    fixed k = (r*r) / 8.0;

			    fixed nom   = NdotV;
			    fixed denom = NdotV * (1.0 - k) + k;

			    return nom / denom;
			}

			fixed GeometrySmith(fixed3 N, fixed3 V, fixed3 L, fixed roughness)
			{
			    fixed NdotV = max(dot(N, V), 0.0);
			    fixed NdotL = max(dot(N, L), 0.0);
			    fixed ggx2  = GeometrySchlickGGX(NdotV, roughness);
			    fixed ggx1  = GeometrySchlickGGX(NdotL, roughness);

			    return ggx1 * ggx2;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 worldPos = float3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);

				fixed4 albedo = tex2D(_Albedo, i.uv);//采样固有色贴图
				fixed4 Metalness = tex2D(_MetalnessMap, i.uv);//采样金属度贴图
				fixed4 Roughness = tex2D(_RoughnessMap, i.uv);
				fixed4 ao = tex2D(_AoMap, i.uv);

				//获得切线坐标下的法线
				fixed3 normal = UnpackNormal(tex2D(_Normal, i.uv));
				//应用缩放，并计算出z分量的值
				normal.xy *= _BumpScale;
				normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
				//将法线转换到世界坐标
				normal = normalize(half3(dot(i.TtoW0.xyz, normal), dot(i.TtoW1.xyz, normal), dot(i.TtoW2.xyz, normal)));

				//normal = normalize(float3(i.TtoW0.z, i.TtoW1.z, i.TtoW2.z));
				 

				fixed3 viewDir = normalize(UnityWorldSpaceViewDir(worldPos));//视角方向

				//fixed3 lightDir = normalize(_WorldSpaceLightPos0.xyz);//灯光方向
				fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));

				fixed3 halfDir = normalize(viewDir + lightDir);//half方向

				fixed3 reflectDir = normalize(reflect(-viewDir,normal));//反射方向
				fixed3 reflection = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0,reflectDir, Roughness *5).rgb;//反射目标

				fixed3 F0 = lerp(fixed3(0.04,0.04,0.04), albedo, Metalness);//金属与非金属的区别
				fixed3 fresnel  = fresnelSchlick(max(dot(normal, viewDir), 0.0), F0);//菲涅尔项

				fixed NDF = DistributionGGX(normal, halfDir, Roughness);//Cook-Torrance 的d项

				fixed G = GeometrySmith(normal, viewDir, lightDir, Roughness);//Cook-Torrance 的g项

				fixed3 specular = NDF * G * fresnel / (4.0 * max(dot(normal, viewDir), 0.0) * max(dot(normal, lightDir), 0.0) + 0.001);//反射部分 ps：+0.001是为了防止除零错误

				specular += lerp(specular,reflection,fresnel);

				fixed3 kD = (1.0 - fresnel) * (1.0 - Metalness);//diffuse部分系数

				float4 sh = float4(ShadeSH9(half4(normal,1)),1.0);
     
    			fixed3 Final = (kD * albedo + specular) * _LightColor0.xyz * ( max(dot(normal, lightDir), 0.0) + 0.0);//反射及diffuse部分整合

    			return   float4( Final,1.0) + 0.03 * sh * albedo * ao;//补个环境反射的光
			}
			ENDCG
		}
		Pass
		{
			Tags { 
				"RenderType"="Opaque"
				"LightMode" = "ForwardAdd"
			}
			Blend One One
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				fixed4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed3 normal:NORMAL;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				fixed4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 TtoW0 : TEXCOORD1;  
				float4 TtoW1 : TEXCOORD2;  
				float4 TtoW2 : TEXCOORD3; 
			};

			sampler2D _Albedo;
			sampler2D _Normal;
			sampler2D _MetalnessMap;
			sampler2D _RoughnessMap;
			sampler2D _AoMap;

			fixed _BumpScale;

			fixed _Metalness;
			fixed _Roughness;

			fixed _key;

			fixed3 _Specular;
			samplerCUBE _Cubemap;
			
			fixed4 _Albedo_ST;

			//使用Unity定义的变量
            uniform fixed4 _LightColor0;
			
			v2f vert (appdata v)
			{
				v2f o;
				
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _Albedo);
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;  
				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);  
				fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);  
				fixed3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;
				//转换矩阵本身也包含信息如：worldpos
				//切线到世界坐标的 转换矩阵
				o.TtoW0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
				o.TtoW1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
				o.TtoW2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
				return o;
			}

			fixed3 fresnelSchlick(float cosTheta, fixed3 F0)
			{
				return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
			}

			fixed DistributionGGX(fixed3 N, fixed3 H, fixed roughness)
			{
			    fixed a      = roughness*roughness;
			    fixed a2     = a*a;
			    fixed NdotH  = max(dot(N, H), 0.0);
			    fixed NdotH2 = NdotH*NdotH;

			    fixed nom   = a2;
			    fixed denom = (NdotH2 * (a2 - 1.0) + 1.0);
			    denom = UNITY_PI * denom * denom;

			    return nom / denom;
			}

			fixed GeometrySchlickGGX(fixed NdotV, fixed roughness)
			{
			    fixed r = (roughness + 1.0);
			    fixed k = (r*r) / 8.0;

			    fixed nom   = NdotV;
			    fixed denom = NdotV * (1.0 - k) + k;

			    return nom / denom;
			}

			fixed GeometrySmith(fixed3 N, fixed3 V, fixed3 L, fixed roughness)
			{
			    fixed NdotV = max(dot(N, V), 0.0);
			    fixed NdotL = max(dot(N, L), 0.0);
			    fixed ggx2  = GeometrySchlickGGX(NdotV, roughness);
			    fixed ggx1  = GeometrySchlickGGX(NdotL, roughness);

			    return ggx1 * ggx2;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 worldPos = float3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);

				fixed4 albedo = tex2D(_Albedo, i.uv);//采样固有色贴图
				fixed4 Metalness = tex2D(_MetalnessMap, i.uv);//采样金属度贴图
				fixed4 Roughness = tex2D(_RoughnessMap, i.uv);
				fixed4 ao = tex2D(_AoMap, i.uv);

				//获得切线坐标下的法线
				fixed3 normal = UnpackNormal(tex2D(_Normal, i.uv));
				//应用缩放，并计算出z分量的值
				normal.xy *= _BumpScale;
				normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
				//将法线转换到世界坐标
				normal = normalize(half3(dot(i.TtoW0.xyz, normal), dot(i.TtoW1.xyz, normal), dot(i.TtoW2.xyz, normal)));

				//normal = normalize(float3(i.TtoW0.z, i.TtoW1.z, i.TtoW2.z));


				fixed3 viewDir = normalize(UnityWorldSpaceViewDir(worldPos));//视角方向

				fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));//灯光方向


				#ifndef USING_LIGHT_MULTI_COMPILE
					fixed atten = 1.0;
				#else
					fixed atten = 1.0/(length(_WorldSpaceLightPos0.xyz - worldPos));
				#endif
				//float3 lightCoord = mul(unity_WorldToLight, float4(worldPos, 1)).xyz;
                //fixed atten = tex2D(_LightTexture0, dot(lightCoord, lightCoord).rr).UNITY_ATTEN_CHANNEL;
				

				fixed3 halfDir = normalize(viewDir + lightDir);//half方向

				fixed3 reflectDir = normalize(reflect(-viewDir,normal));//反射方向
				fixed3 reflection = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0,reflectDir, Roughness *5).rgb;//反射目标

				fixed3 F0 = lerp(fixed3(0.04,0.04,0.04), albedo, Metalness);//金属与非金属的区别
				fixed3 fresnel  = fresnelSchlick(max(dot(normal, viewDir), 0.0), F0);//菲涅尔项

				fixed NDF = DistributionGGX(normal, halfDir, Roughness);//Cook-Torrance 的d项

				fixed G = GeometrySmith(normal, viewDir, lightDir, Roughness);//Cook-Torrance 的g项

				fixed3 specular = NDF * G * fresnel / (4.0 * max(dot(normal, viewDir), 0.0) * max(dot(normal, lightDir), 0.0) + 0.001);//反射部分 ps：+0.001是为了防止除零错误

				specular += lerp(specular,reflection,fresnel);

				fixed3 kD = (1.0 - fresnel) * (1.0 - Metalness);//diffuse部分系数

				//float4 sh = float4(ShadeSH9(half4(normal,1)),1.0);
     
    			fixed3 Final = (kD * albedo + specular) * atten * _LightColor0.xyz * ( max(dot(normal, lightDir), 0.0) + 0.0);//反射及diffuse部分整合

    			return  float4( Final,1.0) + 0.03 * albedo;//补个环境反射的光
			}
			ENDCG
		}
	}
}