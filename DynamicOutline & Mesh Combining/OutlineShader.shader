Shader "DynamicOutline/Outline Only" {

	Properties {
		_OutlineColor ("Outline Color", Color) = (1,1,1,1)
		_Thickness ("Outline thickness", Range (0.002, 0.05)) = .005
	}
 
	CGINCLUDE
	#include "UnityCG.cginc"
 
		float _Thickness;
		float4 _OutlineColor;

		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
		};

		struct v2f {
			float4 pos : POSITION;
			float4 color : COLOR;
		};
 
		v2f vert(appdata v) {
			v2f o;
			o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
 
			float3 norm   = mul ((float3x3) UNITY_MATRIX_IT_MV, v.normal);
			float2 offset = TransformViewToProjection(norm.xy);
 
			o.pos.xy += offset * o.pos.z * _Thickness;
			o.color = _OutlineColor;
			return o;
		}
	ENDCG
 
	SubShader {
		Tags { "Queue" = "Transparent" }
 
		Pass {
			Name "BASE"
			Cull Back
			Blend Zero One
 
			SetTexture [_OutlineColor] {
				ConstantColor (0,0,0,0)
				Combine constant
			}
		}
 
		
		Pass {
			Name "OUTLINE"
			Tags { "LightMode" = "Always" }
			Cull Front
			Blend SrcAlpha OneMinusSrcAlpha
 
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
 
			half4 frag(v2f i) :COLOR {
				return i.color;
			}

			ENDCG
		}
	}
	Fallback "Diffuse"
}