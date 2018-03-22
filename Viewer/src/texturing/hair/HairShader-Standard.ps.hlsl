#include "HairShader.hlsl"

[earlydepthstencil]
float4 main(VertexOutput vertexOutput, bool isFrontFace : SV_IsFrontFace) : SV_Target {
	Result result = calculateResult(vertexOutput, isFrontFace);
	return float4(result.color * result.opacity, result.opacity);
}
