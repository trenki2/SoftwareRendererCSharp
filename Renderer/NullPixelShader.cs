namespace Renderer
{
    public struct NullPixelShader : IPixelShader
    {
        public bool InterpolateZ => false;
        public bool InterpolateW => false;
        public int AVarCount => 0;
        public int PVarCount => 0;

        public void DrawBlock(ref TriangleEquations eqn, int x, int y, bool testEdges)
            => PixelShaderHelper<NullPixelShader>.DrawBlock(ref this, ref eqn, x, y, testEdges);

        public void DrawSpan(ref TriangleEquations eqn, int x, int y, int x2)
            => PixelShaderHelper<NullPixelShader>.DrawSpan(ref this, ref eqn, x, y, x2);
        
        public void DrawPixel(ref PixelData p)
        {
        }
    };
}