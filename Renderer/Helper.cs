﻿namespace Renderer
{
    internal class Helper
    {
        public static unsafe RasterizerVertex interpolateVertex(RasterizerVertex v0, RasterizerVertex v1, float t, int attribCount)
        {
            RasterizerVertex result;

            result.x = v0.x * (1.0f - t) + v1.x * t;
            result.y = v0.y * (1.0f - t) + v1.y * t;
            result.z = v0.z * (1.0f - t) + v1.z * t;
            result.w = v0.w * (1.0f - t) + v1.w * t;
            for (int i = 0; i < attribCount; ++i)
                result.avar[i] = v0.avar[i] * (1.0f - t) + v1.avar[i] * t;

            return result;
        }
    }
}