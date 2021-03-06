﻿using System.Runtime.CompilerServices;

namespace Renderer
{
    // Helper class that pixel shaders can use to implement drawBlock and drawSpan
    public static class PixelShaderHelper<T> where T : IPixelShader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawBlock(ref T shader, ref TriangleEquations eqn, int x, int y, bool TestEdges)
        {
            float xf = x + 0.5f;
            float yf = y + 0.5f;

            PixelData po = new PixelData();
            po.Init(ref eqn, xf, yf, shader.AVarCount, shader.PVarCount, shader.InterpolateZ, shader.InterpolateW);

            EdgeData eo = new EdgeData();
            if (TestEdges)
                eo.init(ref eqn, xf, yf);

            for (int yy = y; yy < y + Constants.BlockSize; yy++)
            {
                PixelData pi = CopyPixelData(ref shader, ref po);

                EdgeData ei = new EdgeData();
                if (TestEdges)
                    ei = eo;

                for (int xx = x; xx < x + Constants.BlockSize; xx++)
                {
                    if (!TestEdges || ei.test(ref eqn))
                    {
                        pi.x = xx;
                        pi.y = yy;
                        shader.DrawPixel(ref pi);
                    }

                    pi.StepX(ref eqn, shader.AVarCount, shader.PVarCount, shader.InterpolateZ, shader.InterpolateW);
                    if (TestEdges)
                        ei.stepX(ref eqn);
                }

                po.StepY(ref eqn, shader.AVarCount, shader.PVarCount, shader.InterpolateZ, shader.InterpolateW);
                if (TestEdges)
                    eo.stepY(ref eqn);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawSpan(ref T shader, ref TriangleEquations eqn, int x, int y, int x2)
        {
            float xf = x + 0.5f;
            float yf = y + 0.5f;

            PixelData p = new PixelData();
            p.y = y;
            p.Init(ref eqn, xf, yf, shader.AVarCount, shader.PVarCount, shader.InterpolateZ, shader.InterpolateW);

            while (x < x2)
            {
                p.x = x;
                shader.DrawPixel(ref p);
                p.StepX(ref eqn, shader.AVarCount, shader.PVarCount, shader.InterpolateZ, shader.InterpolateW);
                x++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe PixelData CopyPixelData(ref T shader, ref PixelData po)
        {
            PixelData pi = new PixelData();
            if (shader.InterpolateZ) pi.z = po.z;
            if (shader.InterpolateW) { pi.w = po.w; pi.invw = po.invw; }
            for (int i = 0; i < shader.AVarCount; ++i)
                pi.avar[i] = po.avar[i];
            for (int i = 0; i < shader.PVarCount; ++i)
            {
                pi.pvarTemp[i] = po.pvarTemp[i];
                pi.pvar[i] = po.pvar[i];
            }
            return pi;
        }
    };
}