Shader "VoxShader/Standard"{

	Properties{
		_MainTex ("Main Tex",2D) = "white"{}	
		_Specular ("Specular",Color) = (1,1,1,1)
		_Gloss ("Gloss",Range(8.0,256)) = 20
		_Step ("Step",Float) = 0.0625
	}

	SubShader{
		Pass{
			Tags{"LightMode" = "ForwardBase"}

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "Lighting.cginc"	

			sampler2D _MainTex;
			fixed4 _Specular;
			float _Gloss;
			float _Step;

			struct a2v{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
			};
			struct v2f{
				float4 pos: SV_POSITION;
				float3 worldNormal:TEXCOORD0;
				float3 worldPos :TEXCOORD1;
				float3 modelPos:TEXCOORD2;
				float2 uv : TEXCOORD3;
			};

			v2f vert(a2v v){
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);

				o.worldPos = mul(unity_ObjectToWorld,v.vertex).xyz;
				o.uv = v.texcoord.xy;

				o.modelPos = v.vertex;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target{
				fixed3 worldNormal = normalize(i.worldNormal);
				fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));

				i.modelPos %= 1;
				float2 offsetUV = float2(0,0);
				if(i.modelPos.x == 0)
				{
					offsetUV = float2(i.modelPos.y,i.modelPos.z);
				}
				else if(i.modelPos.y == 0){
					offsetUV = float2(i.modelPos.z,i.modelPos.x);
				}
				else{
					offsetUV = float2(i.modelPos.x,i.modelPos.y);
				}
				float2 realUV = (i.uv - i.uv % _Step) + offsetUV * _Step;

				fixed3 albedo = tex2D(_MainTex,realUV).rgb;

				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albedo;

				fixed3 diffuse = _LightColor0.rgb * albedo * max(0,dot(worldNormal,worldLightDir));

				fixed3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				fixed3 halfDir = normalize(worldLightDir + viewDir);
				fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0,dot(worldNormal,halfDir)),_Gloss);

				return fixed4(ambient+diffuse+specular,1.0);
			}

			ENDCG
		}
	}
}