using System.Collections.Generic;

namespace Renderer
{
    /// Base class for vertex shaders.
    /** Derive your own vertex shaders from this class and redefine AttribCount etc. */
    public interface IVertexShader
    {
        /// Number of vertex attribute pointers this vertex shader uses.
        int AttribCount { get; }

        /// Process a single vertex.
        /** Implement this in your own vertex shader. */
        unsafe void processVertex(void*[] input, ref RasterizerVertex output);
    };
}