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

        void drawBlock(ref TriangleEquations eqn, int x, int y, bool TestEdges);
        void drawSpan(ref TriangleEquations eqn, int x, int y, int x2);

        /// This is called per pixel.
        /** Implement this in your derived class to display single pixels. */
        void drawPixel(ref PixelData p);
    }

    /// Pixel shader base class.
    /** Derive your own pixel shaders from this class and redefine the static
	  variables to match your pixel shader requirements. */
    public abstract class PixelShaderBase : IPixelShader
    {
        /// Tells the rasterizer to interpolate the z component.
        public bool InterpolateZ { get; set; } = false;

        /// Tells the rasterizer to interpolate the w component.
        public bool InterpolateW { get; set; } = false;

        /// Tells the rasterizer how many affine vars to interpolate.
        public int AVarCount { get; set; } = 0;

        /// Tells the rasterizer how many perspective vars to interpolate.
        public int PVarCount { get; set; } = 0;

        public void drawBlock(ref TriangleEquations eqn, int x, int y, bool TestEdges)
        {
            float xf = x + 0.5f;
            float yf = y + 0.5f;

            PixelData po = new PixelData();
            po.init(ref eqn, xf, yf, AVarCount, PVarCount, InterpolateZ, InterpolateW);

            EdgeData eo = new EdgeData();
            if (TestEdges)
                eo.init(ref eqn, xf, yf);

            for (int yy = y; yy < y + Constants.BlockSize; yy++)
            {
                PixelData pi = copyPixelData(ref po);

                EdgeData ei = new EdgeData();
                if (TestEdges)
                    ei = eo;

                for (int xx = x; xx < x + Constants.BlockSize; xx++)
                {
                    if (!TestEdges || ei.test(ref eqn))
                    {
                        pi.x = xx;
                        pi.y = yy;
                        drawPixel(ref pi);
                    }

                    pi.stepX(ref eqn, AVarCount, PVarCount, InterpolateZ, InterpolateW);
                    if (TestEdges)
                        ei.stepX(ref eqn);
                }

                po.stepY(ref eqn, AVarCount, PVarCount, InterpolateZ, InterpolateW);
                if (TestEdges)
                    eo.stepY(ref eqn);
            }
        }

        public void drawSpan(ref TriangleEquations eqn, int x, int y, int x2)
        {
            float xf = x + 0.5f;
            float yf = y + 0.5f;

            PixelData p = new PixelData();
            p.y = y;
            p.init(ref eqn, xf, yf, AVarCount, PVarCount, InterpolateZ, InterpolateW);

            while (x < x2)
            {
                p.x = x;
                drawPixel(ref p);
                p.stepX(ref eqn, AVarCount, PVarCount, InterpolateZ, InterpolateW);
                x++;
            }
        }

        public abstract void drawPixel(ref PixelData p);

        protected unsafe PixelData copyPixelData(ref PixelData po)
        {
            PixelData pi = new PixelData(); ;
            if (InterpolateZ) pi.z = po.z;
            if (InterpolateW) { pi.w = po.w; pi.invw = po.invw; }
            for (int i = 0; i < AVarCount; ++i)
                pi.avar[i] = po.avar[i];
            for (int i = 0; i < PVarCount; ++i)
            {
                pi.pvarTemp[i] = po.pvarTemp[i];
                pi.pvar[i] = po.pvar[i];
            }
            return pi;
        }
    };
}