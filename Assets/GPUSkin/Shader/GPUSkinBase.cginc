#ifndef GPU_SKIN_BASE
#define GPU_SKIN_BASE

sampler2D _GPUSkin_TextureMatrix;
float3 _GPUSkin_TextureSize_NumPixelsPerFrame;

UNITY_INSTANCING_BUFFER_START(GPUSkinProperties)
UNITY_DEFINE_INSTANCED_PROP(float2, _GPUSkin_FrameIndex_PixelSegmentation)
UNITY_DEFINE_INSTANCED_PROP(float3, _GPUSkin_FrameIndex_PixelSegmentation_Blend_CrossFade)
UNITY_DEFINE_INSTANCED_PROP(float4x4, _GPUSkin_RootMotion)
UNITY_INSTANCING_BUFFER_END(GPUSkinProperties)

#define CrossFadeBlend UNITY_ACCESS_INSTANCED_PROP(GPUSkinProperties, _GPUSkin_FrameIndex_PixelSegmentation_Blend_CrossFade).z
#define RootMotion UNITY_ACCESS_INSTANCED_PROP(GPUSkinProperties, _GPUSkin_RootMotion)

#define skin_blend(pos0, pos1, crossFadeBlend) pos1.xyz + (pos0.xyz - pos1.xyz) * crossFadeBlend

inline float getFrameStartIndex()
{
	float2 frameIndex_segment = UNITY_ACCESS_INSTANCED_PROP(GPUSkinProperties, _GPUSkin_FrameIndex_PixelSegmentation);
	float segment = frameIndex_segment.y;
	float frameIndex = frameIndex_segment.x;
	float frameStartIndex = segment + frameIndex * _GPUSkin_TextureSize_NumPixelsPerFrame.z;
	return frameStartIndex;
}

inline float getFrameStartIndex_crossFade()
{
	float3 frameIndex_segment = UNITY_ACCESS_INSTANCED_PROP(GPUSkinProperties, _GPUSkin_FrameIndex_PixelSegmentation_Blend_CrossFade);
	float segment = frameIndex_segment.y;
	float frameIndex = frameIndex_segment.x;
	float frameStartIndex = segment + frameIndex * _GPUSkin_TextureSize_NumPixelsPerFrame.z;
	return frameStartIndex;
}

#if defined(GPUSKIN_SUPPORT_FLOAT_TEXTURE)
inline float4 indexToUV(float index)
{
	int row = (int)(index / _GPUSkin_TextureSize_NumPixelsPerFrame.x);
	float col = index - row * _GPUSkin_TextureSize_NumPixelsPerFrame.x;
	return float4(col / _GPUSkin_TextureSize_NumPixelsPerFrame.x, row / _GPUSkin_TextureSize_NumPixelsPerFrame.y, 0, 0);
}

inline float4x4 getMatrix(int frameStartIndex, float boneIndex)
{
	float matStartIndex = frameStartIndex + boneIndex * 3;
	float4 row0 = tex2Dlod(_GPUSkin_TextureMatrix, indexToUV(matStartIndex));
	float4 row1 = tex2Dlod(_GPUSkin_TextureMatrix, indexToUV(matStartIndex + 1));
	float4 row2 = tex2Dlod(_GPUSkin_TextureMatrix, indexToUV(matStartIndex + 2));
	float4 row3 = float4(0, 0, 0, 1);

	float4x4 mat = float4x4(row0, row1, row2, row3);
	return mat;
}
#endif

#if defined(GPUSKIN_NOSUPPORT_FLOAT_TEXTURE)
float decode2half(float2 c) {
	float high = c.x * 255;
	float sign = floor(high / 128);
	high -= sign * 128;
	sign = 1 - 2 * sign;
	float exp = floor(high / 4) - 15;
	float mantissa = c.y * 255 + high % 2 * 256 + floor(high / 2 % 2) * 512;
	return sign * pow(2, exp) * (1 + mantissa / 1024);
}

float2 rgbaToFloat2(float4 rgba) {
	return float2(decode2half(rgba.xy), decode2half(rgba.zw));
}

inline float4 indexToUV(float index)
{
	float width = _GPUSkin_TextureSize_NumPixelsPerFrame.x * 2;
	float height = _GPUSkin_TextureSize_NumPixelsPerFrame.y;

	int row = (int)(index / width);
	float col = index - row * width;
	return float4(col / width, row / height, 0, 0);
}

inline float4x4 getMatrix(int frameStartIndex, float boneIndex)
{
	float matStartIndex = (frameStartIndex + boneIndex * 3) * 2;

	float2 r0 = rgbaToFloat2(tex2Dlod(_GPUSkin_TextureMatrix, indexToUV(matStartIndex + 0)).xyzw);
	float2 r1 = rgbaToFloat2(tex2Dlod(_GPUSkin_TextureMatrix, indexToUV(matStartIndex + 1)).xyzw);

	float2 r2 = rgbaToFloat2(tex2Dlod(_GPUSkin_TextureMatrix, indexToUV(matStartIndex + 2)).xyzw);
	float2 r3 = rgbaToFloat2(tex2Dlod(_GPUSkin_TextureMatrix, indexToUV(matStartIndex + 3)).xyzw);

	float2 r4 = rgbaToFloat2(tex2Dlod(_GPUSkin_TextureMatrix, indexToUV(matStartIndex + 4)).xyzw);
	float2 r5 = rgbaToFloat2(tex2Dlod(_GPUSkin_TextureMatrix, indexToUV(matStartIndex + 5)).xyzw);

	float4x4 mat = float4x4(float4(r0, r1), float4(r2, r3), float4(r4, r5), float4(0, 0, 0, 1));
	return mat;
}
#endif

inline float4 skin4(float4 vertex, float4 uv2, float4 uv3)
{
	float frameStartIndex = getFrameStartIndex();
	float4x4 mat0 = getMatrix(frameStartIndex, uv2.x); 
	float4x4 mat1 = getMatrix(frameStartIndex, uv2.z); 
	float4x4 mat2 = getMatrix(frameStartIndex, uv3.x); 
	float4x4 mat3 = getMatrix(frameStartIndex, uv3.z);

	float4x4 root = RootMotion;

	float4 pos = mul(root, mul(mat0, vertex)) * uv2.y 
			   + mul(root, mul(mat1, vertex)) * uv2.w 
			   + mul(root, mul(mat2, vertex)) * uv3.y
			   + mul(root, mul(mat3, vertex)) * uv3.w;

	float crossFadeBlend = CrossFadeBlend;
	if (crossFadeBlend < 1)
	{
		float frameStartIndex_crossFade = getFrameStartIndex_crossFade();
		float4x4 mat0_crossFade = getMatrix(frameStartIndex_crossFade, uv2.x);
		float4x4 mat1_crossFade = getMatrix(frameStartIndex_crossFade, uv2.z);
		float4x4 mat2_crossFade = getMatrix(frameStartIndex_crossFade, uv3.x);
		float4x4 mat3_crossFade = getMatrix(frameStartIndex_crossFade, uv3.z);

		float4 pos1 = mul(mat0_crossFade, vertex) * uv2.y
					+ mul(mat1_crossFade, vertex) * uv2.w
					+ mul(mat2_crossFade, vertex) * uv3.y
					+ mul(mat3_crossFade, vertex) * uv3.w;

		pos = float4(skin_blend(pos, pos1, crossFadeBlend), 1);
	}

	return pos;
}

#endif 