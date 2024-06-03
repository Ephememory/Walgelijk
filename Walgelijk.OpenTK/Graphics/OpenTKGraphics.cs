using OpenTK.Graphics.OpenGL4;
using SkiaSharp;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Walgelijk.OpenTK;

public class OpenTKGraphics : IGraphics
{
    private RenderTarget currentTarget;
    private DrawBounds drawBounds;
    private bool drawBoundEnabledCache;
    private BlendMode currentBlendMode;
    private StencilState stencil;
    internal bool StencilUpdated = false;

    public DrawBounds DrawBounds
    {
        get => drawBounds;
        set
        {
            drawBounds = value;
            if (currentTarget != null)
                SetDrawbounds(value);
        }
    }

    public RenderTarget CurrentTarget
    {
        get => currentTarget;

        set
        {
            if (currentTarget != value)
                SetDrawbounds(drawBounds);

            currentTarget = value;

            if (currentTarget is RenderTexture rt)
            {
                var handles = GPUObjects.RenderTextureCache.Load(rt);
                GPUObjects.RenderTargetDictionary.Set(rt, handles.FramebufferID);
            }

            var id = GPUObjects.RenderTargetDictionary.Get(currentTarget);
            if (id == -1)
                Logger.Error("Attempt to set non-existent render target");
            else
            {
                var size = value.Size;
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, id);
                GL.Viewport(0, 0, (int)size.X, (int)size.Y);
            }
        }
    }

    public StencilState Stencil
    {
        get => stencil;
        set
        {
            StencilUpdated = true;// stencil != Stencil;
            stencil = value;
        }
    }

    public void Clear(Color color)
    {
        GL.ClearColor(color.R, color.G, color.B, color.A);
        GL.ClearDepth(1);
        GL.ClearStencil(0);
        GL.StencilMask(0xFF);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    }

    public void Draw<TVertex>(VertexBuffer<TVertex> vertexBuffer, Material material = null) where TVertex : struct
    {
        if (currentTarget == null)
            return;

        material ??= Material.DefaultTextured;

        UpdateStencilState();
        PrepareVertexBuffer(vertexBuffer, material);
        GL.DrawElements(TypeConverter.Convert(vertexBuffer.PrimitiveType), vertexBuffer.AmountOfIndicesToRender ?? vertexBuffer.IndexCount, DrawElementsType.UnsignedInt, 0);
    }

    private void UpdateStencilState()
    {
        if (StencilUpdated)
        {
            StencilUpdated = false;
            GL.ColorMask(true, true, true, true);

            if (Stencil.Enabled)
            {
                if (Stencil.ShouldClear)
                {
                    GL.ClearStencil(0);
                    GL.StencilMask(0xFF);
                    GL.Clear(ClearBufferMask.StencilBufferBit);
                }

                GL.Enable(EnableCap.StencilTest);
                switch (Stencil.AccessMode)
                {
                    case StencilAccessMode.Write:
                        {
                            GL.Disable(EnableCap.DepthTest);
                            GL.StencilMask(0xFF);

                            GL.ColorMask(false, false, false, false);
                            GL.StencilOp(StencilOp.Replace, StencilOp.Replace, StencilOp.Replace);
                            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
                        }
                        break;

                    case StencilAccessMode.NoWrite:
                        {
                            GL.StencilMask(0x00);

                            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
                            switch (stencil.TestMode)
                            {
                                case StencilTestMode.Inside:
                                    GL.StencilFunc(StencilFunction.Equal, 1, 0xFF);
                                    break;
                                case StencilTestMode.Outside:
                                    GL.StencilFunc(StencilFunction.Notequal, 1, 0xFF);
                                    break;
                            }
                        }
                        break;
                }
            }
            else
                GL.Disable(EnableCap.StencilTest);
        }
    }

    public void DrawInstanced<TVertex>(VertexBuffer<TVertex> vertexBuffer, int instanceCount, Material material = null) where TVertex : struct
    {
        if (currentTarget == null)
            return;

        UpdateStencilState();
        PrepareVertexBuffer(vertexBuffer, material);
        GL.DrawElementsInstanced(TypeConverter.Convert(vertexBuffer.PrimitiveType), vertexBuffer.AmountOfIndicesToRender ?? vertexBuffer.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, instanceCount);
    }

    public void SetUniform<T>(Material material, string uniformName, T data)
    {
        ShaderManager.Instance.SetUniform(material, uniformName, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrepareVertexBuffer<TVertex>(VertexBuffer<TVertex> vertexBuffer, Material material) where TVertex : struct
    {
        SetMaterial(material);
        SetTransformationMatrixUniforms(material);
        var cache = GPUObjects.VertexBufferCache.Load<TVertex>();

        VertexBufferCacheHandles handles = cache.Load(vertexBuffer);

        GL.BindVertexArray(handles.VAO);

        if (vertexBuffer.HasChanged)
            cache.UpdateBuffer(vertexBuffer, handles);

        if (vertexBuffer.ExtraDataHasChanged)
            cache.UpdateExtraData(vertexBuffer, handles);
    }

    private void SetTransformationMatrixUniforms(Material material)
    {
        ShaderManager.Instance.SetUniform(material, ShaderDefaults.ViewMatrixUniform, CurrentTarget.ViewMatrix);
        ShaderManager.Instance.SetUniform(material, ShaderDefaults.ProjectionMatrixUniform, CurrentTarget.ProjectionMatrix);
        ShaderManager.Instance.SetUniform(material, ShaderDefaults.ModelMatrixUniform, CurrentTarget.ModelMatrix);
    }

    private void SetMaterial(Material material)
    {
        if (currentBlendMode != material.BlendMode)
        {
            currentBlendMode = material.BlendMode;
            GLUtilities.SetBlendMode(material.BlendMode);
        }

        if (material.StencilState.HasValue)
            Stencil = material.StencilState.Value;

        var loadedShader = GPUObjects.MaterialCache.Load(material);
        int prog = loadedShader.ProgramHandle;

        GPUObjects.MaterialTextureCache.ActivateTexturesFor(loadedShader);
        GL.UseProgram(prog);

        if (material.BackfaceCulling)
        {
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
        }
        else
            GL.Disable(EnableCap.CullFace);

        if (CurrentTarget.Flags.HasFlag(RenderTargetFlags.DepthStencil) && material.DepthTested)
            GL.Enable(EnableCap.DepthTest);
        else
            GL.Disable(EnableCap.DepthTest);
    }

    private void SetDrawbounds(DrawBounds bounds)
    {
        if (!bounds.Enabled)
        {
            if (drawBoundEnabledCache)
                GL.Disable(EnableCap.ScissorTest);

            drawBoundEnabledCache = false;
            return;
        }

        bounds.Size.X = MathF.Max(0, bounds.Size.X);
        bounds.Size.Y = MathF.Max(0, bounds.Size.Y);

        int x = (int)MathF.Round(bounds.Position.X);
        int y = (int)MathF.Round(CurrentTarget.Size.Y - bounds.Position.Y - bounds.Size.Y);
        int w = (int)MathF.Round(bounds.Size.X);
        int h = (int)MathF.Round(bounds.Size.Y);

        GL.Scissor(x, y, w, h);
        if (!drawBoundEnabledCache)
            GL.Enable(EnableCap.ScissorTest);

        drawBoundEnabledCache = true;
    }

    public void Delete(object obj)
    {
        if (obj == null)
            return;

        switch (obj)
        {
            case RenderTexture rt:
                GPUObjects.RenderTextureCache.Unload(rt);
                break;
            case IReadableTexture texture:
                GPUObjects.TextureCache.Unload(texture);
                break;
            case Material mat:
                GPUObjects.MaterialCache.Unload(mat);
                break;
            case Shader shader:
                GPUObjects.ShaderCache.Unload(shader);
                break;
            default:
                Logger.Error("Attempt to delete unsupported object from GPU");
                break;
        }
    }

    public void Blit(RenderTexture source, RenderTexture destination)
    {
        var sourceLoaded = GPUObjects.RenderTextureCache.Load(source);
        var destinationLoaded = GPUObjects.RenderTextureCache.Load(destination);
        //var destinationLoaded = GPUObjects.RenderTargetDictionary.Get(destination);

        GL.BlitNamedFramebuffer(
            sourceLoaded.FramebufferID,
            destinationLoaded.FramebufferID,
            0, 0, source.Width, source.Height,
            0, 0, (int)destination.Size.X, (int)destination.Size.Y,
            ClearBufferMask.ColorBufferBit,
            BlitFramebufferFilter.Linear
            );
    }

    public bool TryGetId(RenderTexture rt, out int frameBufferId, out int[] textureId)
    {
        textureId = new int[] { -1 };
        frameBufferId = -1;
        if (GPUObjects.RenderTextureCache.Has(rt))
        {
            var l = GPUObjects.RenderTextureCache.Load(rt);
            frameBufferId = l.FramebufferID;
            textureId = l.TextureIds;
            return true;
        }
        return false;
    }

    public bool TryGetId(IReadableTexture texture, out int textureId)
    {
        textureId = -1;
        if (GPUObjects.TextureCache.Has(texture))
        {
            textureId = GPUObjects.TextureCache.Load(texture).Handle;
            return true;
        }
        return false;
    }

    public int TryGetId<TVertex>(VertexBuffer<TVertex> vb, out int vertexBufferId, out int indexBufferId, out int vertexArrayId, ref int[] vertexAttributeIds) where TVertex : struct
    {
        vertexArrayId = vertexBufferId = indexBufferId = -1;
        var vbCache = GPUObjects.VertexBufferCache.Load<TVertex>();
        if (vbCache.Has(vb))
        {
            var l = vbCache.Load(vb);
            int extraVboLength = Math.Min(l.ExtraVBO.Length, vertexAttributeIds.Length);
            for (int i = 0; i < extraVboLength; i++)
                vertexAttributeIds[i] = l.ExtraVBO[i];
            indexBufferId = l.VAO;
            vertexArrayId = l.VAO;
            vertexBufferId = l.VBO;
            return extraVboLength;
        }
        return -1;
    }

    public void SaveTexture(FileStream output, IReadableTexture texture)
    {
        switch (texture)
        {
            case Texture tex:
                {
                    if (TryGetId(tex, out var id))
                    {
                        using var img = TextureToImage(texture.HDR, texture.Width, texture.Height, id);
                        using (var data = img.Encode(SKEncodedImageFormat.Png, 80))
                        {
                            data.SaveTo(output);
                        }
                    }
                }
                break;
                // TODO: Re-add support for RenderTexture. Removed during SixLabor->SkiaSharp conversion.
        }

        static SKImage BuildImage<T>(int width, int height, T[] data)
        {
            var info = new SKImageInfo
            {
                Width = width,
                Height = height,
                ColorType = SKColorType.RgbaF32,
            };

            var image = SKImage.Create(info);
            var pixmap = image.PeekPixels();
            Span<Color> cols = pixmap.GetPixelSpan<Color>();

            Func<int, Color> toColor = data switch
            {
                byte[] b => i => new Color(b[i], b[i + 1], b[i + 2], b[i + 3]),
                float[] f => i => new Color(f[i], f[i + 1], f[i + 2], f[i + 3]),
                _ => throw new Exception("Attempt to save a texture with an invalid format: this error is so severe that you should stop programming forever."),
            };

            int i = 0;
            for (int yy = 0; yy < image.Height; yy++)
            {
                int y = (image.Height - 1 - yy);
                for (int x = 0; x < image.Width; x++)
                {
                    cols[i] = toColor(i);
                    i += image.Info.BytesPerPixel;
                }
            }

            return image;
        }

        static SKImage TextureToImage(bool hdr, int w, int h, int id)
        {
            int s = w * h * 4;

            if (hdr)
            {
                var data = new float[s];
                GL.BindTexture(TextureTarget.Texture2D, id);
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.Float, data);
                return BuildImage(w, h, data);
            }
            else
            {
                var data = new byte[s];
                GL.BindTexture(TextureTarget.Texture2D, id);
                GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                return BuildImage(w, h, data);
            }
        }
    }

    public bool TryGetId(Material mat, out int id)
    {
        id = -1;
        if (GPUObjects.MaterialCache.Has(mat))
        {
            id = GPUObjects.MaterialCache.Load(mat).ProgramHandle;
            return true;
        }
        return false;
    }

    public void Upload(object obj)
    {
        switch (obj)
        {
            case RenderTexture rt:
                GPUObjects.RenderTextureCache.Load(rt);
                break;
            case IReadableTexture texture:
                GPUObjects.TextureCache.Load(texture);
                break;
            case Material mat:
                GPUObjects.MaterialCache.Load(mat);
                break;
            default:
                Logger.Error("Attempt to upload unsupported object to GPU");
                break;
        }
    }

    public void Delete<TVertex>(VertexBuffer<TVertex> vb) where TVertex : struct
    {
        var cache = GPUObjects.VertexBufferCache.Load<TVertex>();
        cache.Load(vb);
    }

    public void Upload<TVertex>(VertexBuffer<TVertex> vb) where TVertex : struct
    {
        var cache = GPUObjects.VertexBufferCache.Load<TVertex>();
        cache.Unload(vb);
    }
}
