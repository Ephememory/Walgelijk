﻿using System.Numerics;
using Walgelijk;
using Walgelijk.Prism;

namespace TestWorld;

public struct PrismScene : ISceneCreator
{
    public Scene Load(Game game)
    {
        var scene = new Scene(game);

        scene.AddSystem(new SpinnySystem());

        scene.AddSystem(new PrismTransformSystem());
        scene.AddSystem(new PrismCameraSystem());
        scene.AddSystem(new PrismFreecamSystem());
        scene.AddSystem(new PrismMeshRendererSystem());

        var camera = scene.CreateEntity();
        scene.AttachComponent(camera, new PrismTransformComponent());
        scene.AttachComponent(camera, new PrismCameraComponent
        {
            ClearColour = new Color("#152123")
        });

        for (int i = 0; i < 15; i++)
        {
            var cube = scene.CreateEntity();
            scene.AttachComponent(cube, new PrismTransformComponent
            {
                Scale = new Vector3(Utilities.RandomFloat(0.5f, 1)),
                Rotation = Quaternion.CreateFromYawPitchRoll(Utilities.RandomFloat(), Utilities.RandomFloat(), Utilities.RandomFloat()),
                Position = new Vector3(Utilities.RandomFloat(-20, 20), Utilities.RandomFloat(0, 20), Utilities.RandomFloat(-20, 20))
            });
            scene.AttachComponent(cube, new PrismMeshComponent());
            scene.AttachComponent(cube, new SpinnyComponent());
        }


        for (int x = -5; x < 10; x++)
            for (int y = -5; y < 10; y++)
            {
                var cube = scene.CreateEntity();
                scene.AttachComponent(cube, new PrismTransformComponent
                {
                    Scale = new Vector3(0.5f),
                    Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2),
                    Position = new Vector3(x, -0.5f, y)
                });

                PrismPrimitives.GenerateQuad(new Vector2(1), out var verts, out var indices);
                var vxb = new VertexBuffer(verts, indices) { PrimitiveType = Primitive.Triangles };

                scene.AttachComponent(cube, new PrismMeshComponent(vxb)
                {
                    Material = new Material(Material.DefaultTextured)
                }).Material.SetUniform("mainTex", TexGen.Colour(1,1, Utilities.RandomColour()));
            }
        return scene;
    }

    public class SpinnyComponent : Component { }

    public class SpinnySystem : Walgelijk.System
    {
        public override void Update()
        {
            foreach (var item in Scene.GetAllComponentsOfType<SpinnyComponent>())
            {
                var transform = Scene.GetComponentFrom<PrismTransformComponent>(item.Entity);
                transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, Time.DeltaTime);
            }
        }
    }
}