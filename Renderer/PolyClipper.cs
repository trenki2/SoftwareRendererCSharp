using System;
using System.Collections.Generic;

namespace Renderer
{
    public class PolyClipper
    {
        private int m_avarCount;
        private int m_pvarCount;
        private List<int> m_indicesIn;
        private List<int> m_indicesOut;
        private List<RasterizerVertex> m_vertices;

        public PolyClipper()
        {
            m_indicesIn = new List<int>();
            m_indicesOut = new List<int>();
        }

        public void Init(List<RasterizerVertex> vertices, int i1, int i2, int i3, int avarCount, int pvarCount)
        {
            m_avarCount = avarCount;
            m_pvarCount = pvarCount;
            m_vertices = vertices;

            m_indicesIn.Clear();
            m_indicesOut.Clear();

            m_indicesIn.Add(i1);
            m_indicesIn.Add(i2);
            m_indicesIn.Add(i3);
        }

        // Clip the poly to the plane given by the formula a * x + b * y + c * z + d * w.
        public void ClipToPlane(float a, float b, float c, float d)
        {
            if (IsFullyClipped())
                return;

            m_indicesOut.Clear();

            int idxPrev = m_indicesIn[0];
            m_indicesIn.Add(idxPrev);

            RasterizerVertex vPrev = m_vertices[idxPrev];
            float dpPrev = a * vPrev.x + b * vPrev.y + c * vPrev.z + d * vPrev.w;

            for (int i = 1; i < m_indicesIn.Count; ++i)
            {
                int idx = m_indicesIn[i];
                RasterizerVertex v = m_vertices[idx];
                float dp = a * v.x + b * v.y + c * v.z + d * v.w;

                if (dpPrev >= 0)
                    m_indicesOut.Add(idxPrev);

                if (Math.Sign(dp) != Math.Sign(dpPrev))
                {
                    float t = dp < 0 ? dpPrev / (dpPrev - dp) : -dpPrev / (dp - dpPrev);

                    RasterizerVertex vOut = Helper.InterpolateVertex(m_vertices[idxPrev], m_vertices[idx], t, m_avarCount, m_pvarCount);
                    m_vertices.Add(vOut);
                    m_indicesOut.Add(m_vertices.Count - 1);
                }

                idxPrev = idx;
                dpPrev = dp;
            }

            var temp = m_indicesIn;
            m_indicesIn = m_indicesOut;
            m_indicesOut = temp;
        }

        public List<int> GetIndices()
        {
            return m_indicesIn;
        }

        public bool IsFullyClipped()
        {
            return m_indicesIn.Count < 3;
        }
    }
}