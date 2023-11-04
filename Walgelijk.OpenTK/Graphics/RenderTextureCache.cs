﻿using OpenTK.Graphics.OpenGL4;
using System;

namespace Walgelijk.OpenTK;

internal class RenderTextureCache : Cache<RenderTexture, RenderTextureHandles>
{
    protected override RenderTextureHandles CreateNew(RenderTexture raw)
    {
        var framebufferID = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferID);

        int[] ids = new int[1];

        // generate framebuffertexture 
        {
            ids[0] = GPUObjects.TextureCache.Load(raw).Handle;
            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                ids[0],
                0);
        }

        if (raw.Flags.HasFlag(RenderTextureFlags.Depth))
        {
            Array.Resize(ref ids, 2);
            ids[1] = GL.GenTexture(); // we will bypass the texture cache because it is completely unnecessary here
            GL.BindTexture(TextureTarget.Texture2D, ids[1]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, raw.Width, raw.Height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);

            var maxFilter = (int)TypeConverter.Convert(raw.FilterMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, maxFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, maxFilter);

            GL.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D,
                ids[1],
                0);

            raw.DepthBuffer = new PseudoTexture(ids[1], raw.Width, raw.Height);
        }

        var result = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (result != FramebufferErrorCode.FramebufferComplete)
        {
            Logger.Error($"Could not create RenderTexture: {result}");
            return new RenderTextureHandles(-1, null!, raw);
        }

        return new RenderTextureHandles(framebufferID, ids, raw);
    }

    protected override void DisposeOf(RenderTextureHandles loaded)
    {
        loaded.RenderTexture.DepthBuffer = null;
        GL.DeleteFramebuffer(loaded.FramebufferID);
        GPUObjects.TextureCache.Unload(loaded.RenderTexture);
        GPUObjects.RenderTargetDictionary.Delete(loaded.RenderTexture);
        for (int i = 0; i < loaded.TextureIds.Length; i++)
        {
            GL.DeleteTexture(loaded.TextureIds[i]);
        }
    }
}
