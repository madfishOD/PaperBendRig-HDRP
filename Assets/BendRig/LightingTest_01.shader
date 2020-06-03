Shader "LightingTest_01"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

		_Normalpositiondeviation("Normal position deviation", Range(0.01 , 1)) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
		Cull Off
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
		#define PI 3.1415926535897932384626433832795

        sampler2D _MainTex;
		uniform float _Normalpositiondeviation;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

		//Script-driven values
		float3 _startPt;
		float3 _endPt;

		float3 _center;
		float _radius;
		float _offset;

		float4x4 _BendMatrix;
		float4x4 _InvBendMatrix;

		float4 Bend(float4 position)
		{
			float4 worldPos = mul(unity_ObjectToWorld, position);
			float4 vertex = mul(_BendMatrix, worldPos);

			float xPos = vertex.x + _offset;
			float circleLength = (2.0 * PI * _radius);
			float t = (xPos) / circleLength;
			float angle = PI + (1.0 - t) * 2.0 * PI;

			float3 dir = normalize(_center - vertex.xyz);

			float3 pos = _center + float3(sin(angle) * _radius, cos(angle) * _radius, vertex.z);
			pos += dir * vertex.y;

			float ln = length(_endPt - _startPt);
			float inv = 1 - saturate((xPos - vertex.x) / ln);
			float gradient = saturate((xPos - vertex.x - circleLength) / ln) + inv;

			float3 bentPos = lerp(pos, vertex.xyz, gradient);

			worldPos = mul(_InvBendMatrix, bentPos);
			return mul(unity_WorldToObject, worldPos);
		}

		void vert(inout appdata_full v) {

			v.vertex = Bend(v.vertex);
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
