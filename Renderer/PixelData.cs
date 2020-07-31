using System.Runtime.CompilerServices;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Init(ref TriangleEquations eqn, float x, float y, int aVarCount, int pVarCount, bool interpolateZ, bool interpolateW)
        {
            if (interpolateZ)
                z = eqn.z.Evaluate(x, y);

            if (interpolateW || pVarCount > 0)
            {
                invw = eqn.invw.Evaluate(x, y);
                w = 1.0f / invw;
            }

            for (int i = 0; i < aVarCount; ++i)
                avar[i] = eqn.avar[i].Evaluate(x, y);

            for (int i = 0; i < pVarCount; ++i)
            {
                pvarTemp[i] = eqn.pvar[i].Evaluate(x, y);
                pvar[i] = pvarTemp[i] * w;
            }
        }

        // Step all the pixel data in the x direction.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StepX(ref TriangleEquations eqn, int aVarCount, int pVarCount, bool interpolateZ, bool interpolateW)
        {
            if (interpolateZ)
                z = eqn.z.StepX(z);

            if (interpolateW || pVarCount > 0)
            {
                invw = eqn.invw.StepX(invw);
                w = 1.0f / invw;
            }

            for (int i = 0; i < aVarCount; ++i)
                avar[i] = eqn.avar[i].StepX(avar[i]);

            for (int i = 0; i < pVarCount; ++i)
            {
                pvarTemp[i] = eqn.pvar[i].StepX(pvarTemp[i]);
                pvar[i] = pvarTemp[i] * w;
            }
        }

        // Step all the pixel data in the y direction.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StepY(ref TriangleEquations eqn, int aVarCount, int pVarCount, bool interpolateZ, bool interpolateW)
        {
            if (interpolateZ)
                z = eqn.z.StepY(z);

            if (interpolateW || pVarCount > 0)
            {
                invw = eqn.invw.StepY(invw);
                w = 1.0f / invw;
            }

            for (int i = 0; i < aVarCount; ++i)
                avar[i] = eqn.avar[i].StepY(avar[i]);

            for (int i = 0; i < pVarCount; ++i)
            {
                pvarTemp[i] = eqn.pvar[i].StepY(pvarTemp[i]);
                pvar[i] = pvarTemp[i] * w;
            }
        }
    }
}