namespace Renderer
{
    public unsafe struct TriangleEquations
    {
        public float area2;

        public EdgeEquation e0;
        public EdgeEquation e1;
        public EdgeEquation e2;

        public ParameterEquation z;
        public ParameterEquation invw;
        public ParameterEquation[] avar;
        public ParameterEquation[] pvar;

        public TriangleEquations(ref RasterizerVertex v0, ref RasterizerVertex v1, ref RasterizerVertex v2, int aVarCount, int pVarCount)
        {
            e0 = new EdgeEquation();
            e1 = new EdgeEquation();
            e2 = new EdgeEquation();

            z = new ParameterEquation();
            invw = new ParameterEquation();
            avar = new ParameterEquation[aVarCount];
            pvar = new ParameterEquation[pVarCount];

            e0.init(ref v1, ref v2);
            e1.init(ref v2, ref v0);
            e2.init(ref v0, ref v1);

            area2 = e0.c + e1.c + e2.c;

            // Cull backfacing triangles.
            if (area2 <= 0)
                return;

            float factor = 1.0f / area2;
            z.init(v0.z, v1.z, v2.z, ref e0, ref e1, ref e2, factor);

            float invw0 = 1.0f / v0.w;
            float invw1 = 1.0f / v1.w;
            float invw2 = 1.0f / v2.w;

            invw.init(invw0, invw1, invw2, ref e0, ref e1, ref e2, factor);
            for (int i = 0; i < aVarCount; ++i)
                avar[i].init(v0.avar[i], v1.avar[i], v2.avar[i], ref e0, ref e1, ref e2, factor);
            for (int i = 0; i < pVarCount; ++i)
                pvar[i].init(v0.pvar[i] * invw0, v1.pvar[i] * invw1, v2.pvar[i] * invw2, ref e0, ref e1, ref e2, factor);
        }
    }
}