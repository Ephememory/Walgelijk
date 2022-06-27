﻿using System;
using System.Collections.Generic;

namespace Walgelijk.SimpleDrawing
{
    public struct CachableTextDrawing
    {
        public string Text;
        public Color Color;
        public Font? Font;
        public float TextBoxWidth;
        public VerticalTextAlign VerticalAlign;
        public HorizontalTextAlign HorizontalAlign;

        public override bool Equals(object? obj)
        {
            return obj is CachableTextDrawing drawing &&
                   Text == drawing.Text &&
                   EqualityComparer<Color>.Default.Equals(Color, drawing.Color) &&
                   EqualityComparer<Font>.Default.Equals(Font, drawing.Font) &&
                   TextBoxWidth == drawing.TextBoxWidth &&
                   VerticalAlign == drawing.VerticalAlign &&
                   HorizontalAlign == drawing.HorizontalAlign;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Text, Color, Font, TextBoxWidth, VerticalAlign, HorizontalAlign);
        }

        public static bool operator ==(CachableTextDrawing left, CachableTextDrawing right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CachableTextDrawing left, CachableTextDrawing right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// A drawing instruction for text
    /// </summary>
    public struct TextDrawing
    {
        /// <summary>
        /// The text
        /// </summary>
        public string? Text;
        /// <summary>
        /// The font. Will fall back to default font if null.
        /// </summary>
        public Font? Font;
        /// <summary>
        /// The width before wrapping
        /// </summary>
        public float TextBoxWidth;
        /// <summary>
        /// Vertical alignment
        /// </summary>
        public VerticalTextAlign VerticalAlign;
        /// <summary>
        /// Horizontal alignment
        /// </summary>
        public HorizontalTextAlign HorizontalAlign;

        public override bool Equals(object? obj)
        {
            return obj is TextDrawing drawing &&
                   Text == drawing.Text &&
                   EqualityComparer<Font?>.Default.Equals(Font, drawing.Font) &&
                   TextBoxWidth == drawing.TextBoxWidth &&
                   VerticalAlign == drawing.VerticalAlign &&
                   HorizontalAlign == drawing.HorizontalAlign;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Text, Font, TextBoxWidth, VerticalAlign, HorizontalAlign);
        }

        public static bool operator ==(TextDrawing left, TextDrawing right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextDrawing left, TextDrawing right)
        {
            return !(left == right);
        }
    }
}