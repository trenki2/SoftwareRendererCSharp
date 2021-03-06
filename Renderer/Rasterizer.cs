﻿using System;
using System.Collections.Generic;

namespace Renderer
{
    public class Rasterizer : IRasterizer
    {
        private int m_minX;
        private int m_maxX;
        private int m_minY;
        private int m_maxY;

        private RasterMode rasterMode;

        private delegate void TriangleFunc(ref RasterizerVertex v0, ref RasterizerVertex v1, ref RasterizerVertex v2);
        private delegate void LineFunc(ref RasterizerVertex v0, ref RasterizerVertex vv1);
        private delegate void PointFunc(ref RasterizerVertex v);

        private TriangleFunc m_triangleFunc;
        private LineFunc m_lineFunc;
        private PointFunc m_pointFunc;

        public Rasterizer()
        {
            SetRasterMode(RasterMode.Span);
            SetScissorRect(0, 0, 0, 0);
            SetPixelShader(new NullPixelShader());
        }

        /// Set the raster mode. The default is RasterMode::Span.
        public void SetRasterMode(RasterMode mode)
        {
            rasterMode = mode;
        }

        /// Set the scissor rectangle.
        public void SetScissorRect(int x, int y, int width, int height)
        {
            m_minX = x;
            m_minY = y;
            m_maxX = x + width;
            m_maxY = y + height;
        }

        /// Set the pixel shader.
        public void SetPixelShader<T>(T shader) where T : struct, IPixelShader
        {
            var impl = new RasterizerImpl<T>(shader, this);
            m_triangleFunc = impl.DrawTriangleModeTemplate;
            m_lineFunc = impl.DrawLineTemplate;
            m_pointFunc = impl.DrawPointTemplate;
        }

        /// Draw a single point.
        public void DrawPoint(ref RasterizerVertex v)
        {
            m_pointFunc(ref v);
        }

        /// Draw a single line.
        public void DrawLine(ref RasterizerVertex v0, ref RasterizerVertex v1)
        {
            m_lineFunc(ref v0, ref v1);
        }

        /// Draw a single triangle.
        public void DrawTriangle(ref RasterizerVertex v0, ref RasterizerVertex v1, ref RasterizerVertex v2)
        {
            m_triangleFunc(ref v0, ref v1, ref v2);
        }

        public void DrawPointList(List<RasterizerVertex> vertices, List<int> indices, int indexCount)
        {
            for (int i = 0; i < indexCount; ++i)
            {
                if (indices[i] == -1)
                    continue;
                RasterizerVertex v = vertices[indices[i]];
                DrawPoint(ref v);
            }
        }

        public void DrawLineList(List<RasterizerVertex> vertices, List<int> indices, int indexCount)
        {
            for (int i = 0; i + 2 <= indexCount; i += 2)
            {
                if (indices[i] == -1)
                    continue;
                RasterizerVertex v0 = vertices[indices[i]];
                RasterizerVertex v1 = vertices[indices[i + 1]];
                DrawLine(ref v0, ref v1);
            }
        }

        public void DrawTriangleList(List<RasterizerVertex> vertices, List<int> indices, int indexCount)
        {
            for (int i = 0; i + 3 <= indexCount; i += 3)
            {
                if (indices[i] == -1)
                    continue;
                RasterizerVertex v0 = vertices[indices[i]];
                RasterizerVertex v1 = vertices[indices[i + 1]];
                RasterizerVertex v2 = vertices[indices[i + 2]];
                DrawTriangle(ref v0, ref v1, ref v2);
            }
        }

        public bool ScissorTest(float x, float y)
        {
            return (x >= m_minX && x < m_maxX && y >= m_minY && y < m_maxY);
        }

        private class RasterizerImpl<T> where T : IPixelShader
        {
            private readonly T shader;
            private readonly Rasterizer rasterizer;

            public RasterizerImpl(T shader, Rasterizer rasterizer)
            {
                this.shader = shader;
                this.rasterizer = rasterizer;
            }

            public unsafe PixelData PixelDataFromVertex(ref RasterizerVertex v)
            {
                PixelData p = new PixelData();
                p.x = (int)v.x;
                p.y = (int)v.y;
                if (shader.InterpolateZ) p.z = v.z;
                if (shader.InterpolateW) { p.w = v.w; p.invw = 1.0f / v.w; }
                for (int i = 0; i < shader.AVarCount; ++i)
                    p.avar[i] = v.avar[i];
                for (int i = 0; i < shader.PVarCount; ++i)
                    p.pvar[i] = v.pvar[i];
                return p;
            }

            public void DrawPointTemplate(ref RasterizerVertex v)
            {
                // Check scissor rect
                if (!rasterizer.ScissorTest(v.x, v.y))
                    return;

                PixelData p = PixelDataFromVertex(ref v);
                shader.DrawPixel(ref p);
            }

            public void DrawLineTemplate(ref RasterizerVertex v0, ref RasterizerVertex v1)
            {
                int adx = Math.Abs((int)v1.x - (int)v0.x);
                int ady = Math.Abs((int)v1.y - (int)v0.y);
                int steps = Math.Max(adx, ady);

                RasterizerVertex step = ComputeVertexStep(ref v0, ref v1, steps);

                RasterizerVertex v = v0;
                while (steps-- > 0)
                {
                    PixelData p = PixelDataFromVertex(ref v);

                    if (rasterizer.ScissorTest(v.x, v.y))
                        shader.DrawPixel(ref p);

                    StepVertex(ref v, ref step);
                }
            }

            public unsafe void StepVertex(ref RasterizerVertex v, ref RasterizerVertex step)
            {
                v.x += step.x;
                v.y += step.y;
                if (shader.InterpolateZ) v.z += step.z;
                if (shader.InterpolateW) v.w += step.w;
                for (int i = 0; i < shader.AVarCount; ++i)
                    v.avar[i] += step.avar[i];
                for (int i = 0; i < shader.PVarCount; ++i)
                    v.pvar[i] += step.pvar[i];
            }

            public unsafe RasterizerVertex ComputeVertexStep(ref RasterizerVertex v0, ref RasterizerVertex v1, int adx)
            {
                RasterizerVertex step = new RasterizerVertex();
                step.x = (v1.x - v0.x) / adx;
                step.y = (v1.y - v0.y) / adx;
                if (shader.InterpolateZ) step.z = (v1.z - v0.z) / adx;
                if (shader.InterpolateW) step.w = (v1.w - v0.w) / adx;
                for (int i = 0; i < shader.AVarCount; ++i)
                    step.avar[i] = (v1.avar[i] - v0.avar[i]) / adx;
                for (int i = 0; i < shader.PVarCount; ++i)
                    step.pvar[i] = (v1.pvar[i] - v0.pvar[i]) / adx;
                return step;
            }

            public void DrawTriangleBlockTemplate(ref RasterizerVertex v0, ref RasterizerVertex v1, ref RasterizerVertex v2)
            {
                // Compute triangle equations.
                TriangleEquations eqn = new TriangleEquations(ref v0, ref v1, ref v2, shader.AVarCount, shader.PVarCount);

                // Check if triangle is backfacing.
                if (eqn.area2 <= 0)
                    return;

                // Compute triangle bounding box.
                int minX = (int)Math.Min(Math.Min(v0.x, v1.x), v2.x);
                int maxX = (int)Math.Max(Math.Max(v0.x, v1.x), v2.x);
                int minY = (int)Math.Min(Math.Min(v0.y, v1.y), v2.y);
                int maxY = (int)Math.Max(Math.Max(v0.y, v1.y), v2.y);

                // Clip to scissor rect.
                minX = Math.Max(minX, rasterizer.m_minX);
                maxX = Math.Min(maxX, rasterizer.m_maxX);
                minY = Math.Max(minY, rasterizer.m_minY);
                maxY = Math.Min(maxY, rasterizer.m_maxY);

                // Round to block grid.
                minX &= ~(Constants.BlockSize - 1);
                maxX &= ~(Constants.BlockSize - 1);
                minY &= ~(Constants.BlockSize - 1);
                maxY &= ~(Constants.BlockSize - 1);

                float s = Constants.BlockSize - 1;

                int stepsX = (maxX - minX) / Constants.BlockSize + 1;
                int stepsY = (maxY - minY) / Constants.BlockSize + 1;

                for (int i = 0; i < stepsX * stepsY; ++i)
                {
                    int sx = i % stepsX;
                    int sy = i / stepsX;

                    // Add 0.5 to sample at pixel centers.
                    int x = minX + sx * Constants.BlockSize;
                    int y = minY + sy * Constants.BlockSize;

                    float xf = x + 0.5f;
                    float yf = y + 0.5f;

                    // Test if block is inside or outside triangle or touches it.
                    EdgeData e00 = new EdgeData(); e00.init(ref eqn, xf, yf);
                    EdgeData e01 = e00; e01.stepY(ref eqn, s);
                    EdgeData e10 = e00; e10.stepX(ref eqn, s);
                    EdgeData e11 = e01; e11.stepX(ref eqn, s);

                    bool e00_0 = eqn.e0.test(e00.ev0), e00_1 = eqn.e1.test(e00.ev1), e00_2 = eqn.e2.test(e00.ev2), e00_all = e00_0 && e00_1 && e00_2;
                    bool e01_0 = eqn.e0.test(e01.ev0), e01_1 = eqn.e1.test(e01.ev1), e01_2 = eqn.e2.test(e01.ev2), e01_all = e01_0 && e01_1 && e01_2;
                    bool e10_0 = eqn.e0.test(e10.ev0), e10_1 = eqn.e1.test(e10.ev1), e10_2 = eqn.e2.test(e10.ev2), e10_all = e10_0 && e10_1 && e10_2;
                    bool e11_0 = eqn.e0.test(e11.ev0), e11_1 = eqn.e1.test(e11.ev1), e11_2 = eqn.e2.test(e11.ev2), e11_all = e11_0 && e11_1 && e11_2;

                    int result = (e00_all ? 1 : 0) + (e01_all ? 1 : 0) + (e10_all ? 1 : 0) + (e11_all ? 1 : 0);

                    // Potentially all out.
                    if (result == 0)
                    {
                        // Test for special case.
                        bool e00Same = e00_0 == e00_1 == e00_2;
                        bool e01Same = e01_0 == e01_1 == e01_2;
                        bool e10Same = e10_0 == e10_1 == e10_2;
                        bool e11Same = e11_0 == e11_1 == e11_2;

                        if (!e00Same || !e01Same || !e10Same || !e11Same)
                            shader.DrawBlock(ref eqn, x, y, true);
                    }
                    else if (result == 4)
                    {
                        // Fully Covered.
                        shader.DrawBlock(ref eqn, x, y, false);
                    }
                    else
                    {
                        // Partially Covered.
                        shader.DrawBlock(ref eqn, x, y, true);
                    }
                }
            }

            public unsafe void DrawTriangleSpanTemplate(ref RasterizerVertex v0, ref RasterizerVertex v1, ref RasterizerVertex v2)
            {
                // Compute triangle equations.
                TriangleEquations eqn = new TriangleEquations(ref v0, ref v1, ref v2, shader.AVarCount, shader.PVarCount);

                // Check if triangle is backfacing.
                if (eqn.area2 <= 0)
                    return;

                ref RasterizerVertex t = ref v0;
                ref RasterizerVertex m = ref v1;
                ref RasterizerVertex b = ref v2;

                // Sort vertices from top to bottom.
                if (t.y > m.y) { var temp = t; t = m; m = temp; }
                if (m.y > b.y) { var temp = m; m = b; b = temp; }
                if (t.y > m.y) { var temp = t; t = m; m = temp; }

                float dy = (b.y - t.y);
                float iy = (m.y - t.y);

                if (m.y == t.y)
                {
                    ref RasterizerVertex l = ref m, r = ref t;
                    if (l.x > r.x) { var temp = l; l = r; r = temp; }
                    DrawTopFlatTriangle(ref eqn, ref l, ref r, ref b);
                }
                else if (m.y == b.y)
                {
                    ref RasterizerVertex l = ref m, r = ref b;
                    if (l.x > r.x) { var temp = l; l = r; r = temp; }
                    DrawBottomFlatTriangle(ref eqn, ref t, ref l, ref r);
                }
                else
                {
                    RasterizerVertex v4 = new RasterizerVertex();
                    v4.y = m.y;
                    v4.x = t.x + ((b.x - t.x) / dy) * iy;
                    if (shader.InterpolateZ) v4.z = t.z + ((b.z - t.z) / dy) * iy;
                    if (shader.InterpolateW) v4.w = t.w + ((b.w - t.w) / dy) * iy;
                    for (int i = 0; i < shader.AVarCount; ++i)
                        v4.avar[i] = t.avar[i] + ((b.avar[i] - t.avar[i]) / dy) * iy;

                    ref RasterizerVertex l = ref m, r = ref v4;

                    if (l.x > r.x) { var temp = l; l = r; r = temp; }

                    DrawBottomFlatTriangle(ref eqn, ref t, ref l, ref r);
                    DrawTopFlatTriangle(ref eqn, ref l, ref r, ref b);
                }
            }

            public void DrawBottomFlatTriangle(ref TriangleEquations eqn, ref RasterizerVertex v0, ref RasterizerVertex v1, ref RasterizerVertex v2)
            {
                float invslope1 = (v1.x - v0.x) / (v1.y - v0.y);
                float invslope2 = (v2.x - v0.x) / (v2.y - v0.y);

                //float curx1 = v0.x;
                //float curx2 = v0.x;

                for (int scanlineY = (int)(v0.y + 0.5f); scanlineY < (int)(v1.y + 0.5f); scanlineY++)
                {
                    float dy = (scanlineY - v0.y) + 0.5f;
                    float curx1 = v0.x + invslope1 * dy + 0.5f;
                    float curx2 = v0.x + invslope2 * dy + 0.5f;

                    // Clip to scissor rect
                    int xl = Math.Max(rasterizer.m_minX, (int)curx1);
                    int xr = Math.Min(rasterizer.m_maxX, (int)curx2);

                    shader.DrawSpan(ref eqn, xl, scanlineY, xr);

                    // curx1 += invslope1;
                    // curx2 += invslope2;
                }
            }

            public void DrawTopFlatTriangle(ref TriangleEquations eqn, ref RasterizerVertex v0, ref RasterizerVertex v1, ref RasterizerVertex v2)
            {
                float invslope1 = (v2.x - v0.x) / (v2.y - v0.y);
                float invslope2 = (v2.x - v1.x) / (v2.y - v1.y);

                // float curx1 = v2.x;
                // float curx2 = v2.x;

                for (int scanlineY = (int)(v2.y - 0.5f); scanlineY > (int)(v0.y - 0.5f); scanlineY--)
                {
                    float dy = (scanlineY - v2.y) + 0.5f;
                    float curx1 = v2.x + invslope1 * dy + 0.5f;
                    float curx2 = v2.x + invslope2 * dy + 0.5f;

                    // Clip to scissor rect
                    int xl = Math.Max(rasterizer.m_minX, (int)curx1);
                    int xr = Math.Min(rasterizer.m_maxX, (int)curx2);

                    shader.DrawSpan(ref eqn, xl, scanlineY, xr);
                    // curx1 -= invslope1;
                    // curx2 -= invslope2;
                }
            }

            public void DrawTriangleAdaptiveTemplate(ref RasterizerVertex v0, ref RasterizerVertex v1, ref RasterizerVertex v2)
            {
                // Compute triangle bounding box.
                float minX = (float)Math.Min(Math.Min(v0.x, v1.x), v2.x);
                float maxX = (float)Math.Max(Math.Max(v0.x, v1.x), v2.x);
                float minY = (float)Math.Min(Math.Min(v0.y, v1.y), v2.y);
                float maxY = (float)Math.Max(Math.Max(v0.y, v1.y), v2.y);

                float orient = (maxX - minX) / (maxY - minY);

                if (orient > 0.4 && orient < 1.6)

                    DrawTriangleBlockTemplate(ref v0, ref v1, ref v2);
                else
                    DrawTriangleSpanTemplate(ref v0, ref v1, ref v2);
            }

            public void DrawTriangleModeTemplate(ref RasterizerVertex v0, ref RasterizerVertex v1, ref RasterizerVertex v2)
            {
                switch (rasterizer.rasterMode)
                {
                    case RasterMode.Span:
                        DrawTriangleSpanTemplate(ref v0, ref v1, ref v2);
                        break;

                    case RasterMode.Block:
                        DrawTriangleBlockTemplate(ref v0, ref v1, ref v2);
                        break;

                    case RasterMode.Adaptive:
                        DrawTriangleAdaptiveTemplate(ref v0, ref v1, ref v2);
                        break;
                }
            }
        }
    }
}