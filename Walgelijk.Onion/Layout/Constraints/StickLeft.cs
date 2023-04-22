﻿using Walgelijk.Onion.Controls;

namespace Walgelijk.Onion.Layout;

public readonly struct StickLeft : IConstraint
{
    public void Apply(in ControlParams p)
    {
        //if (p.Node.Parent == null)
        //    return;

        //var parent = p.Tree.EnsureInstance(p.Node.Parent.Identity);
        var offset = 0 - p.Instance.Rects.Intermediate.MinX + Onion.Theme.Padding;
        p.Instance.Rects.Intermediate = p.Instance.Rects.Intermediate.Translate(offset, 0);
    }
}
