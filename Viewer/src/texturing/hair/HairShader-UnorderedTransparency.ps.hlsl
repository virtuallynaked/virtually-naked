#include "HairShader.hlsl"

struct PSOut {
	float4 accumTarget : SV_Target0;
	float4 revealageTarget : SV_Target1;
};

[earlydepthstencil]
PSOut main(VertexOutput vertexOutput, bool isFrontFace : SV_IsFrontFace) {
	Result result = calculateResult(vertexOutput, isFrontFace);

	float weight = result.visibility * result.visibility;

	PSOut psOut;
	psOut.accumTarget = weight * float4(result.color * result.opacity, result.opacity);
	psOut.revealageTarget = float4(0, 0, 0, result.opacity);
	return psOut;
}
