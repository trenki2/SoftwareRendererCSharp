using System;
using System.Collections.Generic;

namespace Renderer
{
    /// Process vertices and pass them to a rasterizer.
    public class VertexProcessor
    {
        private struct Viewport
        {
            public int x, y, width, height;
            public float px, py, ox, oy;
        }

        private struct DepthRange
        {
            public float n;
            public float f;
        }

        private Viewport m_viewport;
        private DepthRange m_depthRange;

        private CullMode m_cullMode;
        private IRasterizer m_rasterizer;

        private readonly PolyClipper polyClipper = new PolyClipper();
        private readonly List<RasterizerVertex> m_verticesOut = new List<RasterizerVertex>();
        private readonly List<int> m_indicesOut = new List<int>();
        private readonly List<ClipMask> m_clipMask = new List<ClipMask>();
        private readonly List<bool> m_alreadyProcessed = new List<bool>();
        private readonly VertexCache m_vCache = new VertexCache();

        private IVertexShader m_shader;

        /// Constructor.
        public VertexProcessor(IRasterizer rasterizer)
        {
            m_rasterizer = rasterizer;
            m_shader = new NullVertexShader();
        }

        /// Change the rasterizer where the primitives are sent.
        public void SetRasterizer(IRasterizer rasterizer)
        {
            m_rasterizer = rasterizer;
        }

        /// Set the viewport.
        /** Top-Left is (0, 0) */
        public void SetViewport(int x, int y, int width, int height)
        {
            m_viewport.x = x;
            m_viewport.y = y;
            m_viewport.width = width;
            m_viewport.height = height;

            m_viewport.px = width / 2.0f;
            m_viewport.py = height / 2.0f;
            m_viewport.ox = (x + m_viewport.px);
            m_viewport.oy = (y + m_viewport.py);
        }

        /// Set the depth range.
        /** Default is (0, 1) */
        public void SetDepthRange(float n, float f)
        {
            m_depthRange.n = n;
            m_depthRange.f = f;
        }

        /// Set the cull mode.
        /** Default is CullMode::CW to cull clockwise triangles. */
        public void SetCullMode(CullMode mode)
        {
            m_cullMode = mode;
        }

        /// Set the vertex shader.
        public void SetVertexShader(IVertexShader shader)
        {
            m_shader = shader;
        }

        /// Draw a number of points, lines or triangles.
        public void DrawElements(DrawMode mode, int count, List<int> indices)
        {
            m_verticesOut.Clear();
            m_indicesOut.Clear();

            // TODO: Max 1024 primitives per batch.
            m_vCache.clear();

            for (int i = 0; i < count; i++)
            {
                int index = indices[i];
                int outputIndex = m_vCache.lookup(index);

                if (outputIndex != -1)
                {
                    m_indicesOut.Add(outputIndex);
                }
                else
                {
                    RasterizerVertex vOut = new RasterizerVertex();
                    ProcessVertex(index, ref vOut);

                    outputIndex = m_verticesOut.Count;
                    m_indicesOut.Add(outputIndex);
                    m_verticesOut.Add(vOut);

                    m_vCache.set(index, outputIndex);
                }

                if (PrimitiveCount(mode) >= 1024)
                {
                    ProcessPrimitives(mode);
                    m_verticesOut.Clear();
                    m_indicesOut.Clear();
                    m_vCache.clear();
                }
            }

            ProcessPrimitives(mode);
        }

        [Flags]
        private enum ClipMask
        {
            PosX = 0x01,
            NegX = 0x02,
            PosY = 0x04,
            NegY = 0x08,
            PosZ = 0x10,
            NegZ = 0x20
        }

        private ClipMask GetClipMask(RasterizerVertex v)
        {
            ClipMask mask = 0;
            if (v.w - v.x < 0) mask |= ClipMask.PosX;
            if (v.x + v.w < 0) mask |= ClipMask.NegX;
            if (v.w - v.y < 0) mask |= ClipMask.PosY;
            if (v.y + v.w < 0) mask |= ClipMask.NegY;
            if (v.w - v.z < 0) mask |= ClipMask.PosZ;
            if (v.z + v.w < 0) mask |= ClipMask.NegZ;
            return mask;
        }

        private void ProcessVertex(int index, ref RasterizerVertex output)
        {
            m_shader.ProcessVertex(index, ref output);
        }

        private void ClipPoints()
        {
            m_clipMask.Clear();
            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask.Add(0);

            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask[i] = GetClipMask(m_verticesOut[i]);

            for (int i = 0; i < m_indicesOut.Count; i++)
            {
                if (m_clipMask[m_indicesOut[i]] > 0)
                    m_indicesOut[i] = -1;
            }
        }

        private void ClipLines()
        {
            m_clipMask.Clear();
            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask.Add(0);

            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask[i] = GetClipMask(m_verticesOut[i]);

            for (int i = 0; i < m_indicesOut.Count; i += 2)
            {
                int index0 = m_indicesOut[i];
                int index1 = m_indicesOut[i + 1];

                RasterizerVertex v0 = m_verticesOut[index0];
                RasterizerVertex v1 = m_verticesOut[index1];

                ClipMask clipMask = m_clipMask[index0] | m_clipMask[index1];

                LineClipper lineClipper = new LineClipper(v0, v1);

                if ((clipMask & ClipMask.PosX) == ClipMask.PosX) lineClipper.clipToPlane(-1, 0, 0, 1);
                if ((clipMask & ClipMask.NegX) == ClipMask.NegX) lineClipper.clipToPlane(1, 0, 0, 1);
                if ((clipMask & ClipMask.PosY) == ClipMask.PosY) lineClipper.clipToPlane(0, -1, 0, 1);
                if ((clipMask & ClipMask.NegY) == ClipMask.NegY) lineClipper.clipToPlane(0, 1, 0, 1);
                if ((clipMask & ClipMask.PosZ) == ClipMask.PosZ) lineClipper.clipToPlane(0, 0, -1, 1);
                if ((clipMask & ClipMask.NegZ) == ClipMask.NegZ) lineClipper.clipToPlane(0, 0, 1, 1);

                if (lineClipper.fullyClipped)
                {
                    m_indicesOut[i] = -1;
                    m_indicesOut[i + 1] = -1;
                    continue;
                }

                if (m_clipMask[index0] > 0)
                {
                    RasterizerVertex newV = Helper.InterpolateVertex(v0, v1, lineClipper.t0, m_shader.AVarCount, m_shader.PVarCount);
                    m_verticesOut.Add(newV);
                    m_indicesOut[i] = m_verticesOut.Count - 1;
                }

                if (m_clipMask[index1] > 0)
                {
                    RasterizerVertex newV = Helper.InterpolateVertex(v0, v1, lineClipper.t1, m_shader.AVarCount, m_shader.PVarCount);
                    m_verticesOut.Add(newV);
                    m_indicesOut[i + 1] = m_verticesOut.Count - 1;
                }
            }
        }

        private void ClipTriangles()
        {
            m_clipMask.Clear();
            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask.Add(0);

            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask[i] = GetClipMask(m_verticesOut[i]);

            int n = m_indicesOut.Count;

            for (int i = 0; i < n; i += 3)
            {
                int i0 = m_indicesOut[i];
                int i1 = m_indicesOut[i + 1];
                int i2 = m_indicesOut[i + 2];

                ClipMask clipMask = m_clipMask[i0] | m_clipMask[i1] | m_clipMask[i2];

                polyClipper.Init(m_verticesOut, i0, i1, i2, m_shader.AVarCount, m_shader.PVarCount);

                if ((clipMask & ClipMask.PosX) == ClipMask.PosX) polyClipper.ClipToPlane(-1, 0, 0, 1);
                if ((clipMask & ClipMask.NegX) == ClipMask.NegX) polyClipper.ClipToPlane(1, 0, 0, 1);
                if ((clipMask & ClipMask.PosY) == ClipMask.PosY) polyClipper.ClipToPlane(0, -1, 0, 1);
                if ((clipMask & ClipMask.NegY) == ClipMask.NegY) polyClipper.ClipToPlane(0, 1, 0, 1);
                if ((clipMask & ClipMask.PosZ) == ClipMask.PosZ) polyClipper.ClipToPlane(0, 0, -1, 1);
                if ((clipMask & ClipMask.NegZ) == ClipMask.NegZ) polyClipper.ClipToPlane(0, 0, 1, 1);

                if (polyClipper.IsFullyClipped())
                {
                    m_indicesOut[i] = -1;
                    m_indicesOut[i + 1] = -1;
                    m_indicesOut[i + 2] = -1;
                    continue;
                }

                List<int> indices = polyClipper.GetIndices();

                m_indicesOut[i] = indices[0];
                m_indicesOut[i + 1] = indices[1];
                m_indicesOut[i + 2] = indices[2];
                for (int idx = 3; idx < indices.Count; ++idx)
                {
                    m_indicesOut.Add(indices[0]);
                    m_indicesOut.Add(indices[idx - 1]);
                    m_indicesOut.Add(indices[idx]);
                }
            }
        }

        private void ClipPrimitives(DrawMode mode)
        {
            switch (mode)
            {
                case DrawMode.Point:
                    ClipPoints();
                    break;

                case DrawMode.Line:
                    ClipLines();
                    break;

                case DrawMode.Triangle:
                    ClipTriangles();
                    break;
            }
        }

        private void ProcessPrimitives(DrawMode mode)
        {
            ClipPrimitives(mode);
            TransformVertices();
            DrawPrimitives(mode);
        }

        private int PrimitiveCount(DrawMode mode)
        {
            int factor = 1;

            switch (mode)
            {
                case DrawMode.Point: factor = 1; break;
                case DrawMode.Line: factor = 2; break;
                case DrawMode.Triangle: factor = 3; break;
            }

            return m_indicesOut.Count / factor;
        }

        private void DrawPrimitives(DrawMode mode)
        {
            switch (mode)
            {
                case DrawMode.Triangle:
                    CullTriangles();
                    m_rasterizer.DrawTriangleList(m_verticesOut, m_indicesOut, m_indicesOut.Count);
                    break;

                case DrawMode.Line:
                    m_rasterizer.DrawLineList(m_verticesOut, m_indicesOut, m_indicesOut.Count);
                    break;

                case DrawMode.Point:
                    m_rasterizer.DrawPointList(m_verticesOut, m_indicesOut, m_indicesOut.Count);
                    break;
            }
        }

        private void CullTriangles()
        {
            for (int i = 0; i + 3 <= m_indicesOut.Count; i += 3)
            {
                if (m_indicesOut[i] == -1)
                    continue;

                RasterizerVertex v0 = m_verticesOut[m_indicesOut[i]];
                RasterizerVertex v1 = m_verticesOut[m_indicesOut[i + 1]];
                RasterizerVertex v2 = m_verticesOut[m_indicesOut[i + 2]];

                float facing = (v0.x - v1.x) * (v2.y - v1.y) - (v2.x - v1.x) * (v0.y - v1.y);

                if (facing < 0)
                {
                    if (m_cullMode == CullMode.CW)
                        m_indicesOut[i] = m_indicesOut[i + 1] = m_indicesOut[i + 2] = -1;
                }
                else
                {
                    if (m_cullMode == CullMode.CCW)
                        m_indicesOut[i] = m_indicesOut[i + 1] = m_indicesOut[i + 2] = -1;
                    else

                    {
                        int temp = m_indicesOut[i];
                        m_indicesOut[i] = m_indicesOut[i + 2];
                        m_indicesOut[i + 2] = temp;
                    }
                }
            }
        }

        private void TransformVertices()
        {
            m_alreadyProcessed.Clear();
            for (int i = 0; i < m_verticesOut.Count; i++)
                m_alreadyProcessed.Add(false);

            for (int i = 0; i < m_indicesOut.Count; i++)
            {
                int index = m_indicesOut[i];

                if (index == -1)
                    continue;

                if (m_alreadyProcessed[index])
                    continue;

                RasterizerVertex vOut = m_verticesOut[index];

                // Perspective divide
                float invW = 1.0f / vOut.w;
                vOut.x *= invW;
                vOut.y *= invW;
                vOut.z *= invW;

                // Viewport transform
                vOut.x = (m_viewport.px * vOut.x + m_viewport.ox);
                vOut.y = (m_viewport.py * -vOut.y + m_viewport.oy);
                vOut.z = 0.5f * (m_depthRange.f - m_depthRange.n) * vOut.z + 0.5f * (m_depthRange.n + m_depthRange.f);

                m_verticesOut[index] = vOut;
                m_alreadyProcessed[index] = true;
            }
        }
    }
}