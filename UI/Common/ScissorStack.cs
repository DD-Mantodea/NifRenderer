using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace NifRenderer.UI.Common
{
    public readonly struct ScissorStack(GraphicsDevice device)
    {
        private readonly Stack<Rectangle> _stack = [];

        private readonly GraphicsDevice _device = device;

        public readonly Rectangle Current => _stack.Count > 0 ? _stack.Peek() : _device.Viewport.Bounds;

        public void Push(Rectangle rect)
        {
            var clipped = Rectangle.Intersect(Current, rect);

            _stack.Push(clipped);

            _device.ScissorRectangle = clipped;
        }

        public void Pop()
        {
            if (_stack.Count == 0)
                throw new InvalidOperationException("ScissorStack is empty");

            _stack.Pop();

            _device.ScissorRectangle = Current;
        }
    }
}
