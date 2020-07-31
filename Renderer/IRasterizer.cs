using System.Collections.Generic;

namespace Renderer
{
    /// Interface for the rasterizer used by the VertexProcessor.
    public interface IRasterizer
    {
        /// Draw a list of points.
        /** Points with indices == -1 will be ignored. */
        void DrawPointList(List<RasterizerVertex> vertices, List<int> indices, int indexCount);

        /// Draw a list if lines.
        /** Lines  with indices == -1 will be ignored. */
        void DrawLineList(List<RasterizerVertex> vertices, List<int> indices, int indexCount);

        /// Draw a list of triangles.
        /** Triangles  with indices == -1 will be ignored. */
        void DrawTriangleList(List<RasterizerVertex> vertices, List<int> indices, int indexCount);
    };
}