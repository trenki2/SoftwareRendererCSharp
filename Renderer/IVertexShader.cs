using System.Collections.Generic;

namespace Renderer
{
    /// Base class for vertex shaders.
    /** Derive your own vertex shaders from this class and redefine AttribCount etc. */
    public interface IVertexShader
    {
        /// Number of affine output variables.
        int AVarCount { get; }

        /// Number of perspective correct output variables.
        int PVarCount { get; }

        /// Process a single vertex.
        /** Implement this in your own vertex shader. */
        void processVertex(int index, ref RasterizerVertex output);
    };
}