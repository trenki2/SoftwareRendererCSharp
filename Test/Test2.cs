using ObjLoader.Loader.Loaders;
using Renderer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Test
{
    public class Test2
    {
        private struct VertexData
        {
            public Vector3 position;
            public Vector2 texcoord;
        }

        private struct PixelShader : IPixelShader
        {
            public bool InterpolateZ => false;
            public bool InterpolateW => false;
            public int AVarCount => 0;
            public int PVarCount => 2;

            public Bitmap Screen { get; set; }
            public Bitmap Texture { get; set; }

            public void drawBlock(ref TriangleEquations eqn, int x, int y, bool testEdges)
                => PixelShaderHelper<PixelShader>.drawBlock(ref this, ref eqn, x, y, testEdges);

            public void drawSpan(ref TriangleEquations eqn, int x, int y, int x2)
                => PixelShaderHelper<PixelShader>.drawSpan(ref this, ref eqn, x, y, x2);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void drawPixel(ref PixelData p)
            {
                // TODO: check and update depth buffer with p.z;

                int tx = Math.Max(0, (int)(p.pvar[0] * Texture.Width)) % Texture.Width;
                int ty = Math.Max(0, (int)(p.pvar[1] * Texture.Height)) % Texture.Height;
                Screen.SetPixel(p.x, p.y, Texture.GetPixel(tx, ty));
            }
        }

        private class VertexShader : IVertexShader
        {
            public int AVarCount => 0;
            public int PVarCount => 2;

            public List<VertexData> Data { get; set; }
            public Matrix4x4 ModelViewProjectionMatrix { get; set; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void processVertex(int index, ref RasterizerVertex o)
            {
                var v = Data[index];
                var pos = Vector4.Transform(new Vector4(v.position, 1.0f), ModelViewProjectionMatrix);

                o.x = pos.X;
                o.y = pos.Y;
                o.z = pos.Z;
                o.w = pos.W;
                o.pvar[0] = v.texcoord.X;
                o.pvar[1] = v.texcoord.Y;
            }
        }

        public Bitmap Run()
        {
            var screen = new Bitmap(640, 480, PixelFormat.Format32bppArgb);
            var texture = new Bitmap("Data/box.png");

            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create(new IgnoreMaterial());
            var box = objLoader.Load(File.OpenRead("Data/box.obj"));

            (var idata, var vdata) = CreateVertexArray(box);

            var lookAt = Matrix4x4.CreateLookAt(new Vector3(3.0f, 2.0f, 5.0f), new Vector3(0.0f), new Vector3(0.0f, 1.0f, 0.0f));
            var fieldOfView = (float)(60.0 * Math.PI / 180.0);
            var aspect = 4.0f / 3.0f;
            var perspective = Matrix4x4.CreatePerspectiveFieldOfView(fieldOfView, aspect, 0.1f, 10.0f);

            var r = new Rasterizer();
            var v = new VertexProcessor(r);

            r.setRasterMode(RasterMode.Span);
            r.setScissorRect(0, 0, 640, 480);

            v.setViewport(0, 0, 640, 480);
            v.setCullMode(CullMode.CW);

            var pixelShader = new PixelShader();
            pixelShader.Screen = screen;
            pixelShader.Texture = texture;
            r.setPixelShader(pixelShader);

            var vertexShader = new VertexShader();
            vertexShader.Data = vdata;
            vertexShader.ModelViewProjectionMatrix = lookAt * perspective;
            v.setVertexShader(vertexShader);

            v.drawElements(DrawMode.Triangle, idata.Count, idata);

            return screen;
        }

        private static (List<int> indices, List<VertexData> vertices) CreateVertexArray(LoadResult box)
        {
            var idata = new List<int>();
            var vdata = new List<VertexData>();

            foreach (var group in box.Groups)
            {
                foreach (var face in group.Faces)
                {
                    for (int i = 0; i < face.Count; i++)
                    {
                        Debug.Assert(face.Count == 4);

                        var fv = face[i];

                        var vertex = new VertexData
                        {
                            position = new Vector3(
                                box.Vertices[fv.VertexIndex - 1].X,
                                box.Vertices[fv.VertexIndex - 1].Y,
                                box.Vertices[fv.VertexIndex - 1].Z),

                            texcoord = new Vector2(
                                box.Textures[fv.TextureIndex - 1].X,
                                box.Textures[fv.TextureIndex - 1].Y)
                        };

                        vdata.Add(vertex);
                    }

                    var offset = vdata.Count - 4;

                    idata.Add(offset + 0);
                    idata.Add(offset + 1);
                    idata.Add(offset + 2);

                    idata.Add(offset + 0);
                    idata.Add(offset + 2);
                    idata.Add(offset + 3);
                }
            }

            return (idata, vdata);
        }

        private class IgnoreMaterial : IMaterialStreamProvider
        {
            public Stream Open(string materialFilePath)
            {
                return null;
            }
        }
    }
}