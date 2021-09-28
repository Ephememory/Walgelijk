﻿namespace Walgelijk
{
    /// <summary>
    /// Disposes of the vertex buffers in shape components, excluding the Quad component because it uses a shared primitive
    /// </summary>
    public struct ShapeRendererDisposer : IComponentDisposer
    {
        public bool CanDisposeOf(object obj) => obj is ShapeComponent and not QuadShapeComponent;

        public bool DisposeOf(object obj)
        {
            var o = obj as ShapeComponent;

            Game.Main.Window.Graphics.Delete(o.VertexBuffer);

            return true;
        }
    }
}