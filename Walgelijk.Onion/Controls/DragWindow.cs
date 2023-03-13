﻿using System.Numerics;
using System.Runtime.CompilerServices;
using Walgelijk.Onion.Animations;
using Walgelijk.Onion.Layout;
using Walgelijk.SimpleDrawing;

namespace Walgelijk.Onion.Controls;

public readonly struct DragWindow : IControl
{
    public readonly bool IsOpen;

    public DragWindow(bool isOpen)
    {
        IsOpen = isOpen;
    }

    public static ControlState Start(string title, int identity = 0, [CallerLineNumber] int site = 0)
    {
        var (instance, node) = Onion.Tree.Start(IdGen.Hash(nameof(DragWindow).GetHashCode(), identity, site), new DragWindow(true));
        instance.Name = title;
        return instance.State;
    }

    public static ControlState Start(string title, ref bool isOpen, int identity = 0, [CallerLineNumber] int site = 0)
    {
        Onion.Animation.SetDuration(0.1f);
        Onion.Animation.Add(new FadeAnimation());
        Onion.Animation.Add(new ShrinkAnimation());
        var (instance, node) = Onion.Tree.Start(IdGen.Hash(nameof(DragWindow).GetHashCode(), identity, site), new DragWindow(true));
        instance.Name = title;

        // Onion.Layout.Offset(5, 5);
        Onion.Layout.Size(18, 18);
        Onion.Layout.Enqueue(new StickTop());
        Onion.Layout.Enqueue(new StickRight());

        if (Button.Click("X", instance.Identity))
        {
            //isOpen = !isOpen;
        }

        //if (!Openness.TryGetValue(instance.Identity, out isOpen))
        //    Openness.AddOrSet(identity, isOpen);
        return instance.State;
    }

    public void OnAdd(in ControlParams p) { }

    public void OnRemove(in ControlParams p) { }

    public void OnStart(in ControlParams p)
    {

    }

    public void OnProcess(in ControlParams p)
    {
        var d = p.Instance.Rects.ComputedGlobal;
        d.MaxY = d.MinY + 24 + Onion.Theme.Padding * 2;

        //if (IsOpen)
            ControlUtils.ProcessDraggable(p, d);

        //if (p.Instance.IsActive && !wasActive)
        //{
        //    var topMost =
        //        (p.Node.Parent == null ? Onion.Tree.Root.GetChildren() : p.Node.Parent.GetChildren()).
        //        Min(static n => n.RequestedLocalOrder);
        //    p.Node.RequestedLocalOrder = topMost - 1;
        //}
    }

    public void OnRender(in ControlParams p)
    {
        (ControlTree tree, Layout.Layout layout, Input input, GameState state, Node node, ControlInstance instance) = p;

        instance.Rects.Rendered = instance.Rects.ComputedGlobal;
        var t = node.SecondsAlive / instance.AllowedDeadTime;
        var anim = instance.Animations;

        var fg = Onion.Theme.Foreground;
        Draw.Colour = fg.Color;
        Draw.Texture = fg.Texture;

        if (instance.State.HasFlag(ControlState.Active))
            Draw.Colour = fg.Color.Brightness(0.9f);
        else if (instance.State.HasFlag(ControlState.Hover))
        {
            IControl.SetCursor(DefaultCursor.Pointer);
            Draw.Colour = fg.Color.Brightness(1.2f);
        }

        anim.AnimateRect(ref instance.Rects.Rendered, t);
        anim.AnimateColour(ref Draw.Colour, t);
        Draw.Quad(instance.Rects.Rendered, 0, Onion.Theme.Rounding);

        Draw.Colour = Onion.Theme.Text;
        anim.AnimateColour(ref Draw.Colour, t);

        Draw.Text(
            instance.Name, instance.Rects.ComputedGlobal.BottomLeft + new Vector2(Onion.Theme.Padding), 
            Vector2.One, HorizontalTextAlign.Left, VerticalTextAlign.Top, instance.Rects.ComputedGlobal.Width);

        var bg = Onion.Theme.Background;
        Draw.Colour = bg.Color;
        Draw.Texture = bg.Texture;
        anim.AnimateColour(ref Draw.Colour, t);
        var container = instance.Rects.Rendered.Expand(-Onion.Theme.Padding);
        container.MinY += 24;
        anim.AnimateRect(ref container, t);
        Draw.Quad(container, 0, Onion.Theme.Rounding);
    }

    public void OnEnd(in ControlParams p)
    {

    }
}
