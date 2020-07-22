using System.Runtime.CompilerServices;

namespace Renderer
{
    public struct EdgeEquation
    {
        public float a;
        public float b;
        public float c;
        public bool tie;

        public void init(ref RasterizerVertex v0, ref RasterizerVertex v1)
        {
            a = v0.y - v1.y;
            b = v1.x - v0.x;
            c = -(a * (v0.x + v1.x) + b * (v0.y + v1.y)) / 2;
            tie = a != 0 ? a > 0 : b > 0;
        }

        // Evaluate the edge equation for the given point.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float evaluate(float x, float y)
        {
            return a * x + b * y + c;
        }

        // Test if the given point is inside the edge.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool test(float x, float y)
        {
            return test(evaluate(x, y));
        }

        // Test for a given evaluated value.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool test(float v)
        {
            return (v > 0 || (v == 0 && tie));
        }

        // Step the equation value v to the x direction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float stepX(float v)
        {
            return v + a;
        }

        // Step the equation value v to the x direction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float stepX(float v, float stepSize)
        {
            return v + a * stepSize;
        }

        // Step the equation value v to the y direction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float stepY(float v)
        {
            return v + b;
        }

        // Step the equation value vto the y direction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float stepY(float v, float stepSize)
        {
            return v + b * stepSize;
        }
    }
}