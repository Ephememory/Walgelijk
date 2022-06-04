﻿using System.Globalization;
using System.Numerics;
using TemplateProject.Scenes;
using Walgelijk;
using Walgelijk.Imgui;
using Walgelijk.OpenTK;
using Walgelijk.SimpleDrawing;

namespace Videogame;

public class Entry
{
    public static Game? Game { get; private set; }

    public static void PrepareResourceInitialise()
    {
        Resources.BasePath = "resources";
        Resources.SetBasePathForType<IReadableTexture>("textures");
        Resources.SetBasePathForType<Texture>("textures");
        Resources.SetBasePathForType<AudioData>("sounds");
        Resources.SetBasePathForType<Font>("fonts");
    }

    public Entry()
    {
        Game = new Game(
             new OpenTKWindow("Videogame", -Vector2.One, new Vector2(1280, 720)),
             new OpenALAudioRenderer()
             );

        PrepareResourceInitialise();

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        Game.Window.SetIcon(TextureLoader.FromFile("resources/textures/icon.png"));
        Game.Window.TargetUpdateRate = 0;
        Game.Window.VSync = false;
        Game.AudioRenderer.Volume = 0.5f;

        Gui.Context.Order = RenderOrders.Imgui;

        TextureLoader.Settings.FilterMode = FilterMode.Linear;

        Game.Profiling.DrawQuickProfiler = false;
#if DEBUG
        Game.Console.DrawConsoleNotification = true;
        Game.DevelopmentMode = true;
#else
        Game.Console.DrawConsoleNotification = false;
        Game.DevelopmentMode = false;
#endif

        Draw.TextMeshGenerator.KerningMultiplier = 0.9f;
        Draw.TextMeshGenerator.LineHeightMultiplier = 0.9f;

        Game.Scene = MainScene.Create(Game);

        Game.Start();
    }
}
