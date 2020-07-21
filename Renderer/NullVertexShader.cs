using System;
using System.Collections.Generic;

namespace Renderer
{
    ///  NullVertexShader does nothing.
    public class NullVertexShader : IVertexShader
    {
        public int AttribCount => 0;

        public unsafe void processVertex(void*[] input, ref RasterizerVertex output)
        {
        }
    }
}