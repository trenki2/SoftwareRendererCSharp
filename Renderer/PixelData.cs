namespace Renderer
{
    /// PixelData passed to the pixel shader for display.
    public unsafe struct PixelData
    {
        public int x; ///< The x coordinate.
		public int y; ///< The y coordinate.

        public float z; ///< The interpolated z value.
		public float w; ///< The interpolated w value.
		public float invw; ///< The interpolated 1 / w value.

        /// Affine variables.
        public fixed float avar[Constants.MaxAVars];

        /// Perspective variables.
        public fixed float pvar[Constants.MaxPVars];

        // Used internally.
        public fixed float pvarTemp[Constants.MaxPVars];

        // Initialize pixel data for the given pixel coordinates.
        public void init(ref TriangleEquations eqn, float x, float y, int aVarCount, int pVarCount, bool interpolateZ, bool interpolateW)
        {
            if (interpolateZ)
                z = eqn.z.evaluate(x, y);

            if (interpolateW || pVarCount > 0)
            {
                invw = eqn.invw.evaluate(x, y);
                w = 1.0f / invw;
            }

            for (int i = 0; i < aVarCount; ++i)
                avar[i] = eqn.avar[i].evaluate(x, y);

            for (int i = 0; i < pVarCount; ++i)
            {
                pvarTemp[i] = eqn.pvar[i].evaluate(x, y);
                pvar[i] = pvarTemp[i] * w;
            }
        }

        // Step all the pixel data in the x direction.
        public void stepX(ref TriangleEquations eqn, int aVarCount, int pVarCount, bool interpolateZ, bool interpolateW)
        {
            if (interpolateZ)
                z = eqn.z.stepX(z);

            if (interpolateW || pVarCount > 0)
            {
                invw = eqn.invw.stepX(invw);
                w = 1.0f / invw;
            }

            for (int i = 0; i < aVarCount; ++i)
                avar[i] = eqn.avar[i].stepX(avar[i]);

            for (int i = 0; i < pVarCount; ++i)
            {
                pvarTemp[i] = eqn.pvar[i].stepX(pvarTemp[i]);
                pvar[i] = pvarTemp[i] * w;
            }
        }

        // Step all the pixel data in the y direction.
        public void stepY(ref TriangleEquations eqn, int aVarCount, int pVarCount, bool interpolateZ, bool interpolateW)
        {
            if (interpolateZ)
                z = eqn.z.stepY(z);

            if (interpolateW || pVarCount > 0)
            {
                invw = eqn.invw.stepY(invw);
                w = 1.0f / invw;
            }

            for (int i = 0; i < aVarCount; ++i)
                avar[i] = eqn.avar[i].stepY(avar[i]);

            for (int i = 0; i < pVarCount; ++i)
            {
                pvarTemp[i] = eqn.pvar[i].stepY(pvarTemp[i]);
                pvar[i] = pvarTemp[i] * w;
            }
        }
    }
}