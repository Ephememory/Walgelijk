﻿using Walgelijk.Onion.Assets;

namespace Walgelijk.Onion;

public class Theme
{
    public ThemeProperty<Appearance> Background = (Appearance)new Color("#022525");
    public ThemeProperty<Appearance> Foreground = new(new Color("#055555"), new Color("#055555") * 1.1f, new Color("#055555") * 0.9f, new Color("#055555") * 0.8f);
    public ThemeProperty<Color> Text = new Color("#fcffff");
    public ThemeProperty<Color> Accent = new Color("#de3a67");
    public Color Highlight = new Color("#ffffff");

    public Font Font = Font.Default;
    public ThemeProperty<int> FontSize = 12;

    public int Padding = 5;
    public float Rounding = 1;
    public ThemeProperty<float> WindowTitleBarHeight = 24;

    public Color FocusBoxColour = new Color("#3adeda");
    public float FocusBoxSize = 5;
    public float FocusBoxWidth = 4;

    public Sound? HoverSound;
    public Sound? ActiveSound = new(BuiltInAssets.Click, false, false);
    public Sound? ScrollSound;
    public Sound? TriggerSound;
    public Sound? FocusSound;
}
