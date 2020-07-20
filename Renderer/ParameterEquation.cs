namespace Renderer
{
    public struct ParameterEquation
    {
        public float a;
        public float b;
        public float c;

        public void init(
            float p0,
            float p1,
            float p2,
            ref EdgeEquation e0,
            ref EdgeEquation e1,
            ref EdgeEquation e2,
            float factor)
        {
            a = factor * (p0 * e0.a + p1 * e1.a + p2 * e2.a);
            b = factor * (p0 * e0.b + p1 * e1.b + p2 * e2.b);
            c = factor * (p0 * e0.c + p1 * e1.c + p2 * e2.c);
        }

        // Evaluate the parameter equation for the given point.
        public float evaluate(float x, float y)
        {
            return a * x + b * y + c;
        }

        // Step parameter value v in x direction.
        public float stepX(float v)
        {
            return v + a;
        }

        // Step parameter value v in x direction.
        public float stepX(float v, float stepSize)
        {
            return v + a * stepSize;
        }

        // Step parameter value v in y direction.
        public float stepY(float v)
        {
            return v + b;
        }

        // Step parameter value v in y direction.
        public float stepY(float v, float stepSize)
        {
            return v + b * stepSize;
        }
    }
}