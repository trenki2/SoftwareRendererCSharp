namespace Renderer
{
    public struct NullPixelShader : IPixelShader
    {
        public bool InterpolateZ => false;
        public bool InterpolateW => false;
        public int AVarCount => 0;
        public int PVarCount => 0;

        public void drawBlock(ref TriangleEquations eqn, int x, int y, bool testEdges)
            => PixelShaderHelper<NullPixelShader>.drawBlock(ref this, ref eqn, x, y, testEdges);

        public void drawSpan(ref TriangleEquations eqn, int x, int y, int x2)
            => PixelShaderHelper<NullPixelShader>.drawSpan(ref this, ref eqn, x, y, x2);
        
        public void drawPixel(ref PixelData p)
        {
        }
    };
}