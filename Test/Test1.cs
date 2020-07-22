using Renderer;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace Test
{
    public class Test1
    {
        private struct VertexData
        {
            public float x, y, z;
            public float r, g, b;
        }

        private struct PixelShader : IPixelShader
        {
            public bool InterpolateZ => false;
            public bool InterpolateW => false;
            public int AVarCount => 3;
            public int PVarCount => 0;

            public Bitmap Bitmap { get; set; }

            public void drawBlock(ref TriangleEquations eqn, int x, int y, bool testEdges)
                => PixelShaderHelper<PixelShader>.drawBlock(ref this, ref eqn, x, y, testEdges);

            public void drawSpan(ref TriangleEquations eqn, int x, int y, int x2)
                => PixelShaderHelper<PixelShader>.drawSpan(ref this, ref eqn, x, y, x2);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void drawPixel(ref PixelData p)
            {
                Bitmap.SetPixel(p.x, p.y, Color.FromArgb(
                    255,
                    (int)(p.avar[0] * 255),
                    (int)(p.avar[1] * 255),
                    (int)(p.avar[2] * 255))
                );
            }
        }

        private class VertexShader : IVertexShader
        {
            public int AVarCount => 3;
            public int PVarCount => 0;

            public List<VertexData> VertexData { get; set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void processVertex(int index, ref RasterizerVertex output)
            {
                var data = VertexData[index];
                output.x = data.x;
                output.y = data.y;
                output.z = data.z;
                output.w = 1.0f;
                output.avar[0] = data.r;
                output.avar[1] = data.g;
                output.avar[2] = data.b;
            }
        }

        public Bitmap Run()
        {
            var bitmap = new Bitmap(640, 480, PixelFormat.Format32bppArgb);

            var r = new Rasterizer();
            var v = new VertexProcessor(r);

            var pixelShader = new PixelShader();
            var vertexShader = new VertexShader();

            r.setScissorRect(0, 0, 640, 480);
            v.setViewport(100, 100, 640 - 200, 480 - 200);
            v.setCullMode(CullMode.None);

            var indices = new List<int> { 0, 1, 2 };
            var vertices = new List<VertexData>();

            var vertex = new VertexData();
            vertex.x = 0.0f;
            vertex.y = 0.5f;
            vertex.z = 0.0f;
            vertex.r = 1.0f;
            vertex.g = 0.0f;
            vertex.b = 0.0f;
            vertices.Add(vertex);

            vertex = new VertexData();
            vertex.x = -1.5f;
            vertex.y = -0.5f;
            vertex.z = 0.0f;
            vertex.r = 0.0f;
            vertex.g = 1.0f;
            vertex.b = 0.0f;
            vertices.Add(vertex);

            vertex = new VertexData();
            vertex.x = 1.5f;
            vertex.y = -0.5f;
            vertex.z = 0.0f;
            vertex.r = 0.0f;
            vertex.g = 0.0f;
            vertex.b = 1.0f;
            vertices.Add(vertex);

            pixelShader.Bitmap = bitmap;
            vertexShader.VertexData = vertices;

            r.setPixelShader(pixelShader);
            v.setVertexShader(vertexShader);

            v.drawElements(DrawMode.Triangle, 3, indices);

            return bitmap;
        }
    }
}