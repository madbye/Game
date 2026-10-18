using System.Numerics;
using MGL.Graphics;
using MGL.IO;
using MGL.Utils;

namespace Game;

public class Fireball
{
    private const float Speed = 20f;
    
    public Vector3 Position { get; private set; }
    public Vector3 Direction { get; private set; }
    public float Lifetime { get; private set; } = 0;
    
    private static VertexArray? _mesh;
    private static Texture2D? _texture2D;

    private Renderer _renderer;
    private ShaderProgram _shader;

    private bool _disposed = false;
   
    
    public Fireball(Renderer renderer, Vector3 position, Vector3 direction)
    {
        _mesh ??= VertexArray.FromCommonMesh(renderer, MeshGenerator.GenUVSphere(5, 6));
        _texture2D ??= Texture2D.FromImage(renderer, ImageLoader.LoadImage("Assets/fireball.png", ColorComponents.RGB),TextureFilter.Nearest, false);
        _shader = renderer.DefaultShaders.GetUnlit();

        Position = position;
        Direction = direction;
    }
    
    public void Update(double dt)
    {
        float delta = (float)dt / 1000f;
        Lifetime += delta;
        Position += Direction * Speed * delta;
    }
    public Quaternion Rotation = Quaternion.Identity;

    public void Draw(ICamera camera)
    {
        _shader.Bind();

        Quaternion spin = Quaternion.CreateFromAxisAngle(
            Vector3.UnitZ,
            Lifetime * 10f
        );

        Quaternion finalRotation = Rotation * spin;

        Matrix4x4 model =
            Matrix4x4.CreateScale(0.2f) *
            Matrix4x4.CreateFromQuaternion(finalRotation) *
            Matrix4x4.CreateTranslation(Position);

        _shader.SetUniform("model", model);

        camera.SetVPMatricesToProgram(_shader);
        _texture2D.Bind();
        _mesh.Draw();
    }
}