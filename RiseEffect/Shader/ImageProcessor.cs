using OpenTK.Graphics.OpenGL;
using System.Drawing;
using SDPixelFormat = System.Drawing.Imaging.PixelFormat;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using OpenTK.Windowing.Desktop;
using System.Drawing.Imaging;
using OpenTK.Mathematics;

namespace RiseEffect.Shader
{
    public class ImageProcessor
    {
        private readonly IFilter _filter;
        private readonly Shader _shader;
        private int _vertexArrayObject;

        public ImageProcessor(IFilter filter, int width, int height)
        {
            var win = new GameWindow(new GameWindowSettings(),
                new NativeWindowSettings() {
                    StartVisible = false,
                    Size = new OpenTK.Mathematics.Vector2i(width, height),
                });
            win.MakeCurrent();
            win.MaximumSize = new Vector2i(width, height);
            win.Size = (Vector2i)win.MaximumSize;

            GL.ClearColor(0.5f, 0.5f, 0.5f, 1.0f);

            float[] _vertices =
            {
                // Position         Texture coordinates
                 1.0f,  1.0f, 0.0f, 1.0f, 1.0f, // top right
                 1.0f, -1.0f, 0.0f, 1.0f, 0.0f, // bottom right
                -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, // bottom left
                -1.0f,  1.0f, 0.0f, 0.0f, 1.0f  // top left
            };

            uint[] _indices =
            {
                0, 1, 3,
                1, 2, 3
            };

            int _elementBufferObject;

            int _vertexBufferObject;

            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            _elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            _shader = filter.InitializeFilter();
            _shader.SetInt("texture1", 0);
            _shader.Use();

            var vertexLocation = _shader.GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            var texCoordLocation = _shader.GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            _filter = filter;
        }

        public Bitmap ProcessImage(Bitmap image)
        {
            GL.Viewport(new Box2i(new Vector2i(0, 0), new Vector2i(image.Width, image.Height)));
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.BindVertexArray(_vertexArrayObject);

            var texture = Texture.LoadFromBitmap(image, OpenTK.Graphics.OpenGL4.TextureUnit.Texture0);
            texture.Use(OpenTK.Graphics.OpenGL4.TextureUnit.Texture0);

            _filter.UseFilter();
            _shader.Use();

            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);

            GL.Flush();
            GL.Finish();

            var result = new Bitmap(image.Width, image.Height, SDPixelFormat.Format24bppRgb);
            var mem = result.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.WriteOnly, SDPixelFormat.Format24bppRgb);
            GL.PixelStore(PixelStoreParameter.PackRowLength, mem.Stride / 3);
            GL.ReadPixels(0, 0, image.Width, image.Height, PixelFormat.Bgr, PixelType.UnsignedByte, mem.Scan0);
            result.UnlockBits(mem);
            texture.Delete();
            return result;
        }
    }
}
