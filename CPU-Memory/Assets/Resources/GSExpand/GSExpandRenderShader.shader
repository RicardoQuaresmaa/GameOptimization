Shader "Custom/BufferShader"
{
	SubShader
	{
		Pass
		{
			ZTest Less
			Cull Back
			ZWrite Off
			BlendOp Add
			Blend SrcAlpha OneMinusSrcAlpha

			Fog	{ Mode off }

			CGPROGRAM

			#include "UnityCG.cginc"
			#pragma target 5.0
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			StructuredBuffer<float4> gPosition;

			// ---

			struct VS_OUTPUT
			{
				float4 svPosition : SV_POSITION;
			};

            VS_OUTPUT vert(uint vID : SV_VertexID)
			{
                VS_OUTPUT output;

				output.svPosition = float4(gPosition[vID].xyz, 1);

				return output;
			}

			// ---

			struct GS_OUTPUT 
			{
				float4 svPosition : SV_POSITION;
				float2 uv : UV;
			};

			[maxvertexcount(4)]
			void geom(point VS_OUTPUT input[1], inout TriangleStream<GS_OUTPUT> TriStream)
			{
				float3 lensRight = UNITY_MATRIX_IT_MV[0].xyz;
				float3 lensUp = UNITY_MATRIX_IT_MV[1].xyz;
				float3 lensForward = UNITY_MATRIX_IT_MV[2].xyz;

				float3 pPosition = input[0].svPosition.xyz;
                float2 pScale = float2(0.1f, 0.1f);

				float3 pForward = normalize(_WorldSpaceCameraPos - pPosition);
				float3 pRight = cross(pForward, lensUp);
				float3 pUp = cross(pRight, pForward);

                GS_OUTPUT output;
				for (int vID = 0; vID < 4; ++vID)
				{
					float x = vID == 0 || vID == 1;
					float y = vID == 0 || vID == 2;

					float3 vPosition = pPosition + pRight * ((x * 2.f - 1.f) * pScale.x) + pUp * ((y * 2.f - 1.f) * pScale.y);

					output.svPosition = UnityObjectToClipPos(float4(vPosition, 1));
                    output.uv = float2(x, 1.0f - y);

					TriStream.Append(output);
				}

				TriStream.RestartStrip();
			}

			// ---

			float4 frag(GS_OUTPUT input) : SV_TARGET0
			{
				float x = input.uv.x - 0.5f;
				float y = input.uv.y - 0.5f;
				float r = sqrt(x * x + y * y);
				float factor = max(1.f - r * 2.f, 0.f); //[1,0]

				float cosFactor = -cos(3.14159265f / 2.f * (factor + 1.f));

				return float4(0, 1, 0, cosFactor);
			}

			ENDCG
		}
	}
}
