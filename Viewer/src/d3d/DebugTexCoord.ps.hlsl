#include "VertexOutput.hlsl"

float4 main(VertexOutput output) : SV_Target {
	return float4(output.texcoord, 1 - 0.5 * output.texcoord[0] - 0.5 * output.texcoord[1], 1);
}