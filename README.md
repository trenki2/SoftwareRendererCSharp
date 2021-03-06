# C# Software Renderer/Rasterizer

This project implements a C# software renderer/rasterizer with vertex- and
pixel shader support. It is a direct source code port from my C++ Software Renderer/Rasterizer.

For best performance you should compile for x64. Still the C# code is much slower than the C++ code.

## Features
* Affine and perspective correct per vertex parameter interpolation.
* Vertex and pixel shaders written in C#.

## Resources

* [Triangle Rasterization](http://www.cs.unc.edu/~blloyd/comp770/Lecture08.pdf)
* [Accelerated Half-Space Triangle Rasterization](https://www.researchgate.net/publication/286441992_Accelerated_Half-Space_Triangle_Rasterization)

## Example

```csharp

// PixelShader must be a struct for better performance
public struct PixelShader : IPixelShader
{
  public bool InterpolateZ => false;
  public bool InterpolateW => false;
  public int AVarCount => 3;
  public int PVarCount => 0;

  public Bitmap Screen { get; set; }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void DrawBlock(ref TriangleEquations eqn, int x, int y, bool testEdges)
    => PixelShaderHelper<PixelShader>.drawBlock(ref this, ref eqn, x, y, testEdges);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void DrawSpan(ref TriangleEquations eqn, int x, int y, int x2)
    => PixelShaderHelper<PixelShader>.drawSpan(ref this, ref eqn, x, y, x2);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void DrawPixel(ref PixelData p)
  {
    // This is for demonstration only. It is slow. In real code you 
    // would have to write to a color buffer directly.
    Screen.SetPixel(p.x, p.y, Color.FromArgb(
      255,
      (int)(p.avar[0] * 255),
      (int)(p.avar[1] * 255),
      (int)(p.avar[2] * 255))
    );
  }
}

public struct VertexShader : IVertexShader
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

// Use the renderer
var r = new Rasterizer();
var v = new VertexProcessor(r);

r.SetScissorRect(0, 0, 640, 480);
v.SetViewport(0, 0, 640, 480);
v.SetCullMode(CullMode.None);

var indices = new List<int>();
var vertices = new List<VertexData>();

// Populate indices and vertices
...

// Must set pixel shader every time its parameters are 
// updated since it is a struct and will be copied.
var pixelShader = new PixelShader();
pixelShader.Screen = new Bitmap(640, 480);
r.SetPixelShader(pixelShader);

var vertexShader = new VertexShader();
vertexShader.VertexData = vertices;
v.SetVertexShader(vertexShader);

v.DrawElements(DrawMode.Triangle, indexData.size(), indexData);

```

## License

This code is licensed under the MIT License (see [LICENSE](LICENSE)).