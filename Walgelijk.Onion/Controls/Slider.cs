﻿using System.Numerics;
using System.Runtime.CompilerServices;
using Walgelijk.SimpleDrawing;

namespace Walgelijk.Onion.Controls;

public readonly struct Slider : IControl
{
    private readonly Direction direction;
    private readonly MinMax<float> range;
    private readonly float step;
    private readonly string? labelFormat;

    public Slider(Direction direction, MinMax<float> range, float step, string? labelFormat = null)
    {
        this.direction = direction;
        this.range = range;
        this.step = step;
        this.labelFormat = labelFormat;
    }

    private static readonly OptionalControlState<float> states = new();

    public static bool Float(ref float value, Direction dir, MinMax<float> range, float step = 0, string? label = null, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Hash(nameof(Slider).GetHashCode(), (int)dir, identity, site), new Slider(dir, range, step, label));
        instance.RenderFocusBox = false;
        Onion.Tree.End();
        var r = states.HasIncomingChange(instance.Identity);
        states.UpdateFor(instance.Identity, ref value);

        return r;
    }

    public static bool Int(ref int value, Direction dir, MinMax<int> range, int step = 1, string? label = null, int identity = 0, [CallerLineNumber] int site = 0)
    {
        float vv = value;
        var rr = new MinMax<float>(range.Min, range.Max);
        bool r;
        if (r = Float(ref vv, dir, rr, Math.Max(1, step), label, identity, site))
            value = (int)vv;
        return r;
    }

    public void OnAdd(in ControlParams p)
    {
    }

    public void OnStart(in ControlParams p)
    {
    }

    public void OnProcess(in ControlParams p)
    {
        ControlUtils.ProcessButtonLike(p);

        if (p.Input.CtrlHeld)
            p.Instance.CaptureFlags |= CaptureFlags.Scroll;
        else
            p.Instance.CaptureFlags &= ~CaptureFlags.Scroll;

        var v = 0f;

        float d;
        if (p.Input.CtrlHeld && p.Instance.HasScroll && Math.Abs(d = p.Input.ScrollDelta.Y) > 0)
        {
            var s = Onion.Configuration.ScrollSensitivity;

            if (step < float.Epsilon)
            {
                switch (direction)
                {
                    case Direction.Horizontal:
                        s /= p.Instance.Rects.ComputedGlobal.Width;
                        break;
                    case Direction.Vertical:
                        s /= p.Instance.Rects.ComputedGlobal.Height;
                        break;
                }
                s *= Math.Abs(range.Max - range.Min);
            }
            else
                s = step - float.Epsilon;

            v = Utilities.Clamp(states[p.Identity] + (d > 0 ? s : -s), range.Min, range.Max);
        }
        else if (p.Instance.IsActive)
        {
            var m = p.Input.MousePosition;
            var r = p.Instance.Rects.ComputedGlobal;
            switch (direction)
            {
                case Direction.Horizontal:
                    v = Utilities.Clamp(Utilities.MapRange(r.MinX, r.MaxX, range.Min, range.Max, m.X), range.Min, range.Max);
                    break;
                case Direction.Vertical:
                    v = Utilities.Clamp(Utilities.MapRange(r.MaxY, r.MinY, range.Min, range.Max, m.Y), range.Min, range.Max);
                    break;
            }
        }
        else return;

        states[p.Identity] = step > float.Epsilon ? Utilities.Snap(v, step) : v;
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, Layout.LayoutQueue layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        var t = node.GetAnimationTime();
        var anim = instance.Animations;

        var fg = p.Theme.Foreground[instance.State];
        Draw.Colour = fg.Color;
        Draw.Texture = fg.Texture;
        Draw.OutlineColour = p.Theme.OutlineColour[instance.State];
        Draw.OutlineWidth = p.Theme.OutlineWidth[instance.State];

        anim.AnimateRect(ref instance.Rects.Rendered, t);
        anim.AnimateColour(ref Draw.Colour, t);
        Draw.Quad(instance.Rects.Rendered, 0, p.Theme.Rounding);

        var sliderRect = instance.Rects.Rendered;
        float animatedMin = Utilities.Lerp(range.Max, range.Min, Utilities.Clamp(t));
        switch (direction)
        {
            case Direction.Horizontal:
                sliderRect.MaxX = Utilities.MapRange(animatedMin, range.Max, sliderRect.MinX, sliderRect.MaxX, states[p.Identity]);
                break;
            case Direction.Vertical:
                sliderRect.MinY = Utilities.MapRange(animatedMin, range.Max, sliderRect.MaxY, sliderRect.MinY, states[p.Identity]);
                break;
        }

        sliderRect.MaxX = MathF.Max(sliderRect.MaxX, sliderRect.MinX + p.Theme.Padding * 3);
        sliderRect.MinY = MathF.Min(sliderRect.MinY, sliderRect.MaxY - p.Theme.Padding * 3);

        Draw.Colour = p.Theme.Accent[instance.State];
        anim.AnimateColour(ref Draw.Colour, t);
        Draw.OutlineWidth = 0;
        Draw.Quad(sliderRect.Expand(-p.Theme.Padding), 0, p.Theme.Rounding);

        if (labelFormat != null)
        {
            // TODO string allocation :S
            var v = states[p.Identity];
            var str = v.ToString();
            if (!string.IsNullOrWhiteSpace(labelFormat))
                str = string.Format(labelFormat, str);

            Draw.Font = p.Theme.Font;
            Draw.Colour = p.Theme.Text[instance.State];
            Draw.Text(str, instance.Rects.Rendered.GetCenter(), Vector2.One, HorizontalTextAlign.Center, VerticalTextAlign.Middle, instance.Rects.Rendered.Width);
        }
    }

    public void OnEnd(in ControlParams p)
    {

    }

    public void OnRemove(in ControlParams p)
    {
    }
}
