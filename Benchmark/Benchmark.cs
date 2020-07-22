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

        private class PixelShader : PixelShaderBase
        {
            public PixelShader()
            {
                AVarCount = 3;
            }

            public int[][] Screen { get; set; }
            public Bitmap Bitmap { get; set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe override void drawPixel(ref PixelData p)
            {
                Screen[p.x][p.y] = 1;

                //Bitmap.SetPixel(p.x, p.y, Color.FromArgb(
                //    255,
                //    (int)(p.avar[0] * 255),
                //    (int)(p.avar[1] * 255),
                //    (int)(p.avar[2] * 255))
                //);
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

        public void Run()
        {
            var screen = new int[640][];
            for (int i = 0; i < 640; i++)
                screen[i] = new int[480];

            var r = new Rasterizer();
            var v = new VertexProcessor(r);

            var pixelShader = new PixelShader();
            var vertexShader = new VertexShader();

            r.setScissorRect(0, 0, 640, 480);
            v.setViewport(0, 0, 640, 480);
            v.setCullMode(CullMode.None);

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

            pixelShader.Screen = screen;
            vertexShader.VertexData = vertices;

            r.setPixelShader(pixelShader);
            v.setVertexShader(vertexShader);

            var sw = Stopwatch.StartNew();
            v.drawElements(DrawMode.Triangle, indices.Count, indices);
            Console.WriteLine(sw.ElapsedMilliseconds);
        }
    }
}
