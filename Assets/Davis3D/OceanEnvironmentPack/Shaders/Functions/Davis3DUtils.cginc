#ifndef DAVIS3D_UTILS
#define DAVIS3D_UTILS

float4 GradientMap_Multi(float GreyscaleToGradient, float Index, sampler2D Gradient, float NumberOfGradients)
{
    NumberOfGradients = 1.0 / ceil(NumberOfGradients);
    Index = ceil(Index);
    float Section01 = NumberOfGradients * Index;
    Section01 += 0.5 * NumberOfGradients;
    float2 uv = float2(GreyscaleToGradient, Section01);

    return tex2Dlod(Gradient, float4(uv, 0, 0));
}

float3 MS_MorphTargets(float2 uv, float MorphAnimation, sampler2D MorphNormal, sampler2D MorphTexture, float NumberOfMorphTargets)
{
    float s = MorphAnimation * (NumberOfMorphTargets - 1.0);
    float s00 = floor(s);
    float s01 = s00 + 1.0;
    float s02 = frac(s);

    float4 Gradient01 = GradientMap_Multi(uv.r, s01, MorphTexture, NumberOfMorphTargets);
    float4 Gradient02 = GradientMap_Multi(uv.r, s00, MorphTexture, NumberOfMorphTargets);

    float3 vertexOffset = lerp(Gradient02, Gradient01, s02);

    return vertexOffset;
}

#endif