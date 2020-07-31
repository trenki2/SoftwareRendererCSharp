using System;
using System.Collections.Generic;

namespace Renderer
{
    ///  NullVertexShader does nothing.
    public class NullVertexShader : IVertexShader
    {
        public int AVarCount => 0;
        public int PVarCount => 0;

        public void ProcessVertex(int index, ref RasterizerVertex output)
        {
        }
    }
}