#define SAMPLE_COLOR_TEX(name) (name * name##Tex.Sample(anisotropicSampler, input.texcoord).rgb)
#define SAMPLE_FLOAT_TEX(name) (name * name##Tex.Sample(anisotropicSampler, input.texcoord).r)
#define SAMPLE_NORMAL_TEX(name) (name * (2 * name##Tex.Sample(anisotropicSampler, input.texcoord).rgb - 1))
#define SAMPLE_BUMP_TEX(name) (normalFromBumpGradient(name, name##Tex.Sample(anisotropicSampler, input.texcoord).rg))

float meanValue(float3 color) {
	return (color.r + color.g + color.b) / 3;
}

float maxValue(float3 color) {
	return max(color.r, max(color.g, color.b));
}

float reflectivityFromIOR(float ior) {
	float sqrtReflectivity = (1 - ior) / (1 + ior);
	float reflectivity = sqrtReflectivity * sqrtReflectivity;
	return reflectivity;
}

float3 combineNormals(float3 normal1, float3 normal2) {
	float2 xy = normal1.xy + normal2.xy;
	float z = sqrt(1 - dot(xy, xy));
	return float3(xy, z);
}

float3 normalFromBumpGradient(float strength, float2 bumpGradientTexValue) {
	float2 bumpGradient = 2 * strength * (bumpGradientTexValue * 2 - 1);
	bumpGradient.y *= -1;
	float z = sqrt(1 - dot(bumpGradient, bumpGradient));
	return float3(bumpGradient, z);
}

//apply a premultiplied-alpha layer to an opaque base
float3 applyLayer(float3 base, float4 layer) {
	return base * (1 - layer.a) + layer.rgb;
}

//apply a premultiplied-alpha layer to a premultiplied-alpha base
//This obeys the following identities:
//  apply[0, l] == l
//  apply[b, 0] == b
//  apply[b, apply[l1, l2]] == apply[apply[b, l1], l2]
float4 applyLayer(float4 base, float4 layer) {
	return base * (1 - layer.a) + layer;
}
