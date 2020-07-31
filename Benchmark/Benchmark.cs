using Renderer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace Benchmark
{
    public class Benchmark
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

            public int[] Buffer { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DrawBlock(ref TriangleEquations eqn, int x, int y, bool testEdges)
                => PixelShaderHelper<PixelShader>.DrawBlock(ref this, ref eqn, x, y, testEdges);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DrawSpan(ref TriangleEquations eqn, int x, int y, int x2)
                => PixelShaderHelper<PixelShader>.DrawSpan(ref this, ref eqn, x, y, x2);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DrawPixel(ref PixelData p)
            {
                Buffer[p.x + Width * p.y] = 1;
            }
        }

        private class VertexShader : IVertexShader
        {
            public int AVarCount => 3;
            public int PVarCount => 0;

            public List<VertexData> VertexData { get; set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void ProcessVertex(int index, ref RasterizerVertex output)
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

        public void Run()
        {
            var r = new Rasterizer();
            var v = new VertexProcessor(r);

            var pixelShader = new PixelShader();
            var vertexShader = new VertexShader();

            r.SetScissorRect(0, 0, 640, 480);
            v.SetViewport(0, 0, 640, 480);
            v.SetCullMode(CullMode.None);

            var indices = new List<int>();
            var vertices = new List<VertexData>();

            var random = new Random(0);

            for (int i = 0; i < 4096 * 10; i++)
            {
                VertexData CreateVertex()
                {
                    var vertex = new VertexData();
                    vertex.x = (float)random.NextDouble();
                    vertex.y = (float)random.NextDouble();
                    vertex.z = (float)random.NextDouble();
                    vertex.r = (float)random.NextDouble();
                    vertex.g = (float)random.NextDouble();
                    vertex.b = (float)random.NextDouble();
                    return vertex;
                }

                var offset = vertices.Count;

                vertices.Add(CreateVertex());
                vertices.Add(CreateVertex());
                vertices.Add(CreateVertex());

                indices.Add(offset + 0);
                indices.Add(offset + 1);
                indices.Add(offset + 2);
            }

            pixelShader.Buffer = new int[640 * 480];
            pixelShader.Width = 640;
            pixelShader.Height = 480;
            vertexShader.VertexData = vertices;

            r.SetPixelShader(pixelShader);
            v.SetVertexShader(vertexShader);

            var sw = Stopwatch.StartNew();
            v.DrawElements(DrawMode.Triangle, indices.Count, indices);
            Console.WriteLine(sw.ElapsedMilliseconds);
        }
    }
}
