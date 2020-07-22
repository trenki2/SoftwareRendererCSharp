namespace Renderer
{
    public interface IPixelShader
    {
        /// Tells the rasterizer to interpolate the z component.
        bool InterpolateZ { get; }

        /// Tells the rasterizer to interpolate the w component.
        bool InterpolateW { get; }

        /// Tells the rasterizer how many affine vars to interpolate.
        int AVarCount { get; }

        /// Tells the rasterizer how many perspective vars to interpolate.
        int PVarCount { get; }

        /// Draw a block of size BlockSize * BlockSize.
        void drawBlock(ref TriangleEquations eqn, int x, int y, bool testEdges);
        
        /// Draw a span from left to right.
        void drawSpan(ref TriangleEquations eqn, int x, int y, int x2);

        /// This is called per pixel.
        /** Implement this in your derived class to display single pixels. */
        void drawPixel(ref PixelData p);
    }
}