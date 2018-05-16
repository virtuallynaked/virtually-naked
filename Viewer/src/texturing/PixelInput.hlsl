#include <d3d/VertexOutput.hlsl>

struct OcclusionInfo {
	float amount;
	float3 direction;
};

struct PixelInput {
	float3 eyeDirection;

	float3x3 tbn;
	float2 texcoord;

	float3x3 secondaryToPrimaryTbn;
	float2 secondaryTexcoord;

	float2 occlusion;
	float3 scatteredIllumination;
};

float3 normalizeNonZero(float3 v) {
	float lengthSquared = dot(v, v);
	return lengthSquared == 0 ? v : v * rsqrt(lengthSquared);
}

PixelInput preparePixelInput(VertexOutput vertexOutput, bool isFrontFace) {
	PixelInput input;

	input.eyeDirection = normalize(vertexOutput.positions.objectRelativeEyePosition);
	
	float3 normal = normalize(vertexOutput.normal);
	float3 tangent = normalizeNonZero(vertexOutput.tangent - normal * dot(vertexOutput.tangent, normal));
	float3 secondaryTangent = normalizeNonZero(vertexOutput.secondaryTangent - normal * dot(vertexOutput.secondaryTangent, normal));

	input.texcoord = vertexOutput.texcoord;
	input.secondaryTexcoord = vertexOutput.secondaryTexcoord;

	input.occlusion = saturate(vertexOutput.occlusion);

	if (!isFrontFace) {
		normal *= -1;
		input.occlusion = input.occlusion.yx;
	}

	float3 bitangent = -cross(tangent, normal);
	float3 secondaryBitangent = -cross(secondaryTangent, normal);
	
	input.tbn = float3x3(tangent, bitangent, normal);
	input.secondaryToPrimaryTbn = float3x3(
		dot(tangent, secondaryTangent), dot(tangent, secondaryBitangent), 0,
		dot(bitangent, secondaryTangent), dot(bitangent, secondaryBitangent), 0,
		0, 0, 1);
	
	input.scatteredIllumination = vertexOutput.scatteredIllumination;
	
	return input;
}

static const float3 GeometryNormal = float3(0, 0, 1); //in tangent space

float3 convertToObjectSpaceNormal(PixelInput input, float3 tangentSpaceNormal) {
	return mul(tangentSpaceNormal, input.tbn);
}

float3 convertSecondaryToPrimaryTangentSpace(PixelInput input, float3 secondaryTangentSpaceNormal) {
	return mul(secondaryTangentSpaceNormal, input.secondaryToPrimaryTbn);
}
