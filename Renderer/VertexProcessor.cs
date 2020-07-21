using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Renderer
{
    /// Process vertices and pass them to a rasterizer.
    public class VertexProcessor
    {
        /// Constructor.
        public VertexProcessor(IRasterizer rasterizer)
        {
            m_rasterizer = rasterizer;
        }

        /// Change the rasterizer where the primitives are sent.
        private void setRasterizer(IRasterizer rasterizer)
        {
            m_rasterizer = rasterizer;
        }

        /// Set the viewport.
        /** Top-Left is (0, 0) */
        private void setViewport(int x, int y, int width, int height)
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
        private void setDepthRange(float n, float f)
        {
            m_depthRange.n = n;
            m_depthRange.f = f;
        }

        /// Set the cull mode.
        /** Default is CullMode::CW to cull clockwise triangles. */
        public void setCullMode(CullMode mode)
        {
            m_cullMode = mode;
        }

        /// Set the vertex shader.
        public unsafe void setVertexShader(IVertexShader shader)
        {
            Debug.Assert(shader.AttribCount <= Constants.MaxVertexAttribs);
            m_attribCount = shader.AttribCount;
            m_processVertexFunc = shader.processVertex;
        }

        /// Set a vertex attrib pointer.
        public unsafe void setVertexAttribPointer(int index, int stride, void* buffer)
        {
            Debug.Assert(index < Constants.MaxVertexAttribs);
            m_attributes[index].buffer = buffer;
            m_attributes[index].stride = stride;
        }

        /// Draw a number of points, lines or triangles.
        public unsafe void drawElements(DrawMode mode, int count, List<int> indices)
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
                    void*[] vIn = new void*[m_attribCount];
                    initVertexInput(vIn, index);

                    int outputIndex2 = m_verticesOut.Count;

                    RasterizerVertex vOut = new RasterizerVertex();
                    processVertex(vIn, ref vOut);

                    m_indicesOut.Add(outputIndex2);
                    m_verticesOut.Add(vOut);

                    m_vCache.set(index, outputIndex2);
                }

                if (primitiveCount(mode) >= 1024)
                {
                    processPrimitives(mode);
                    m_verticesOut.Clear();
                    m_indicesOut.Clear();
                    m_vCache.clear();
                }
            }

            processPrimitives(mode);
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

        private ClipMask clipMask(RasterizerVertex v)
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

        private unsafe void* attribPointer(int attribIndex, int elementIndex)
        {
            ref Attribute attrib = ref m_attributes[attribIndex];
            return (char*)attrib.buffer + attrib.stride * elementIndex;
        }

        private unsafe void processVertex(void*[] input, ref RasterizerVertex output)
        {
            m_processVertexFunc(input, ref output);
        }

        private unsafe void initVertexInput(void*[] input, int index)
        {
            for (int i = 0; i < m_attribCount; ++i)
                input[i] = attribPointer(i, index);
        }

        private void clipPoints()
        {
            m_clipMask.Clear();
            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask.Add(0);

            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask[i] = clipMask(m_verticesOut[i]);

            for (int i = 0; i < m_indicesOut.Count; i++)
            {
                if (m_clipMask[m_indicesOut[i]] > 0)
                    m_indicesOut[i] = -1;
            }
        }
        private void clipLines()
        {
            m_clipMask.Clear();
            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask.Add(0);

            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask[i] = clipMask(m_verticesOut[i]);

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
                    RasterizerVertex newV = Helper.interpolateVertex(v0, v1, lineClipper.t0, m_attribCount);
                    m_verticesOut.Add(newV);
                    m_indicesOut[i] = m_verticesOut.Count - 1;
                }

                if (m_clipMask[index1] > 0)
                {
                    RasterizerVertex newV = Helper.interpolateVertex(v0, v1, lineClipper.t1, m_attribCount);
                    m_verticesOut.Add(newV);
                    m_indicesOut[i + 1] = m_verticesOut.Count - 1;
                }
            }
        }
        private void clipTriangles()
        {
            m_clipMask.Clear();
            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask.Add(0);

            for (int i = 0; i < m_verticesOut.Count; i++)
                m_clipMask[i] = clipMask(m_verticesOut[i]);

            int n = m_indicesOut.Count;

            for (int i = 0; i < n; i += 3)
            {
                int i0 = m_indicesOut[i];
                int i1 = m_indicesOut[i + 1];
                int i2 = m_indicesOut[i + 2];

                ClipMask clipMask = m_clipMask[i0] | m_clipMask[i1] | m_clipMask[i2];

                polyClipper.init(m_verticesOut, i0, i1, i2, m_attribCount);

                if ((clipMask & ClipMask.PosX) == ClipMask.PosX) polyClipper.clipToPlane(-1, 0, 0, 1);
                if ((clipMask & ClipMask.NegX) == ClipMask.NegX) polyClipper.clipToPlane(1, 0, 0, 1);
                if ((clipMask & ClipMask.PosY) == ClipMask.PosY) polyClipper.clipToPlane(0, -1, 0, 1);
                if ((clipMask & ClipMask.NegY) == ClipMask.NegY) polyClipper.clipToPlane(0, 1, 0, 1);
                if ((clipMask & ClipMask.PosZ) == ClipMask.PosZ) polyClipper.clipToPlane(0, 0, -1, 1);
                if ((clipMask & ClipMask.NegZ) == ClipMask.NegZ) polyClipper.clipToPlane(0, 0, 1, 1);

                if (polyClipper.fullyClipped())
                {
                    m_indicesOut[i] = -1;
                    m_indicesOut[i + 1] = -1;
                    m_indicesOut[i + 2] = -1;
                    continue;
                }

                List<int> indices = polyClipper.indices();

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

        private void clipPrimitives(DrawMode mode)
        {
            switch (mode)
            {
                case DrawMode.Point:
                    clipPoints();
                    break;

                case DrawMode.Line:
                    clipLines();
                    break;

                case DrawMode.Triangle:
                    clipTriangles();
                    break;
            }
        }

        private void processPrimitives(DrawMode mode)
        {
            clipPrimitives(mode);
            transformVertices();
            drawPrimitives(mode);
        }

        private int primitiveCount(DrawMode mode)
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

        private void drawPrimitives(DrawMode mode)
        {
            switch (mode)
            {
                case DrawMode.Triangle:
                    cullTriangles();
                    m_rasterizer.drawTriangleList(m_verticesOut, m_indicesOut, m_indicesOut.Count);
                    break;

                case DrawMode.Line:
                    m_rasterizer.drawLineList(m_verticesOut, m_indicesOut, m_indicesOut.Count);
                    break;

                case DrawMode.Point:
                    m_rasterizer.drawPointList(m_verticesOut, m_indicesOut, m_indicesOut.Count);
                    break;
            }
        }

        private void cullTriangles()
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

        private void transformVertices()
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

        private unsafe delegate void ProcessVertexFunc(void*[] input, ref RasterizerVertex output);
        ProcessVertexFunc m_processVertexFunc;

	private int m_attribCount;

        private unsafe struct Attribute
        {
            public void* buffer;
            public int stride;
        }

        private Attribute[] m_attributes = new Attribute[Constants.MaxVertexAttribs];

        // Some temporary variables for speed
        private PolyClipper polyClipper;

        private List<RasterizerVertex> m_verticesOut;
        private List<int> m_indicesOut;
        private List<ClipMask> m_clipMask;
        private List<bool> m_alreadyProcessed;

        private VertexCache m_vCache = new VertexCache();
    }
}