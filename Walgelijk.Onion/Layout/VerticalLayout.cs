﻿using Walgelijk.Onion.Controls;

namespace Walgelijk.Onion.Layout;

public readonly struct VerticalLayout : ILayout
{
    public void Apply(in ControlParams p, int index, int childId)
    {
        var child = p.Tree.EnsureInstance(childId);
        if (index > 0)
        {
            var heightSoFar = p.Node.GetChildren().Take(index).Sum(static i => Onion.Tree.EnsureInstance(i.Identity).Rects.Intermediate.Height + Onion.Theme.Padding);
            child.Rects.Intermediate = child.Rects.Intermediate.Translate(0, heightSoFar);
        }
    }
}
