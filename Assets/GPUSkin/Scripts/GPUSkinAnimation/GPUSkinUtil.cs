using System;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinUtil
{
    public static float Fmod(float x, float y)
    {
        return x - (int)(x / y) * y;
    }

    public static Color Float2ToColor(float v1, float v2)
    {
        ushort rg = Mathf.FloatToHalf(v1);
        ushort ba = Mathf.FloatToHalf(v2);
        return new Color((rg >> 8 & 0x00ff) / 255f, (rg & 0x00ff) / 255f, (ba >> 8 & 0x00ff) / 255f, (ba & 0x00ff) / 255f);
    }

    public static float Decode2half(float x, float y)
    {
        float high = x * 255;
        float sign = Mathf.Floor(high / 128);
        high -= sign * 128;
        sign = 1 - 2 * sign;
        float exp = Mathf.Floor(high / 4) - 15;
        float mantissa = y * 255 + high % 2 * 256 + Mathf.Floor(high / 2 % 2) * 512;
        return sign * Mathf.Pow(2, exp) * (1 + mantissa / 1024);
    }

    private static bool bSwitchGpuSkinFloatTexture = false;

    public static Texture2D CreateTexture2D(GPUSkinAnimationData animData, out Color[] pixels)
    {
        pixels = null;
        if (animData == null)
        {
            return null;
        }

        if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
        {
            if (!bSwitchGpuSkinFloatTexture)
            {
                Shader.EnableKeyword("GPUSKIN_SUPPORT_FLOAT_TEXTURE");
                Shader.DisableKeyword("GPUSKIN_NOSUPPORT_FLOAT_TEXTURE");
                bSwitchGpuSkinFloatTexture = true;
            }

            Texture2D texture = new Texture2D(animData.textureWidth, animData.textureHeight, TextureFormat.RGBAHalf, false, true);
            texture.name = "GPUSkinTextureMatrix";
            texture.filterMode = FilterMode.Point;

            pixels = texture.GetPixels();
            int pixelIndex = 0;
            int clipCount = animData.clips.Count;
            for (int clipIndex = 0; clipIndex < clipCount; ++clipIndex)
            {
                GPUSkinClip clip = animData.clips[clipIndex];
                var frames = clip.frames;
                int numFrames = frames.Length;
                for (int frameIndex = 0; frameIndex < numFrames; ++frameIndex)
                {
                    GPUSkinFrame frame = frames[frameIndex];
                    Matrix4x4[] matrices = frame.matrices;
                    int numMatrices = matrices.Length;
                    for (int matrixIndex = 0; matrixIndex < numMatrices; ++matrixIndex)
                    {
                        Matrix4x4 matrix = matrices[matrixIndex];
                        pixels[pixelIndex++] = new Color(matrix.m00, matrix.m01, matrix.m02, matrix.m03);
                        pixels[pixelIndex++] = new Color(matrix.m10, matrix.m11, matrix.m12, matrix.m13);
                        pixels[pixelIndex++] = new Color(matrix.m20, matrix.m21, matrix.m22, matrix.m23);
                    }
                }
            }
            texture.SetPixels(pixels, 0);
            texture.Apply(false);

            return texture;
        }
        else if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBA32))
        {
            if (!bSwitchGpuSkinFloatTexture)
            {
                Shader.EnableKeyword("GPUSKIN_NOSUPPORT_FLOAT_TEXTURE");
                Shader.DisableKeyword("GPUSKIN_SUPPORT_FLOAT_TEXTURE");
                bSwitchGpuSkinFloatTexture = true;
            }

            Texture2D texture = new Texture2D(animData.textureWidth * 2, animData.textureHeight, TextureFormat.RGBA32, false, true);
            texture.name = "GPUSkinTextureMatrix";
            texture.filterMode = FilterMode.Point;

            pixels = texture.GetPixels();
            int pixelIndex = 0;
            int clipCount = animData.clips.Count;
            for (int clipIndex = 0; clipIndex < clipCount; ++clipIndex)
            {
                GPUSkinClip clip = animData.clips[clipIndex];
                var frames = clip.frames;
                int numFrames = frames.Length;
                for (int frameIndex = 0; frameIndex < numFrames; ++frameIndex)
                {
                    GPUSkinFrame frame = frames[frameIndex];
                    Matrix4x4[] matrices = frame.matrices;
                    int numMatrices = matrices.Length;
                    for (int matrixIndex = 0; matrixIndex < numMatrices; ++matrixIndex)
                    {
                        Matrix4x4 matrix = matrices[matrixIndex];
                        pixels[pixelIndex++] = Float2ToColor(matrix.m00, matrix.m01);
                        pixels[pixelIndex++] = Float2ToColor(matrix.m02, matrix.m03);

                        pixels[pixelIndex++] = Float2ToColor(matrix.m10, matrix.m11);
                        pixels[pixelIndex++] = Float2ToColor(matrix.m12, matrix.m13);

                        pixels[pixelIndex++] = Float2ToColor(matrix.m20, matrix.m21);
                        pixels[pixelIndex++] = Float2ToColor(matrix.m22, matrix.m23);
                    }
                }
            }
            texture.SetPixels(pixels, 0);
            texture.Apply(false);

            return texture;
        }

        return null;
    }
}
