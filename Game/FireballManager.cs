using System.Numerics;
using MGL.Graphics;

namespace Game;

public static class FireballManager
{
    private const float FireballLifetime = 5f;
    public static List<Fireball> Fireballs { get; private set; } = new List<Fireball>();

    public static void CreateFIreball(Renderer renderer, Vector3 position, Vector3 direction)
    {
        Fireballs.Add(new Fireball(renderer, position, direction));
    }

    public static void UpdateFireballs(double dt)
    {
        foreach (var fireball in Fireballs)
        {
            fireball.Update(dt);
        }
        Fireballs.RemoveAll(fb => fb.Lifetime >= FireballLifetime);
    }

    public static void DrawFireballs(ICamera camera)
    {
        foreach (var fireball in Fireballs)
        {
            fireball.Draw(camera);
        }
    }
}