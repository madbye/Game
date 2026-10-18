using System.Numerics;
using System.Drawing;
using Game;
using MGL.Physics;
using MGL;
using MGL.Graphics;
using MGL.IO;
using MGL.Utils;

var window = new Window(1280u, 720u, "Game");
window.Initialize();

var inputContext = window.GetInputContext();

var renderer = window.CreateRenderer();

using var vao = VertexArray.FromCommonMesh(renderer, MeshGenerator.GenCube());

using var texture = Texture2D.FromImage(
    renderer, 
    CheckerboardGenerator.GenCheckerboard(Color.Magenta, Color.Black, 4), 
    TextureFilter.Nearest, 
    false
);
texture.Bind();

var shader = renderer.DefaultShaders.GetPhong();
shader.Bind();

var lights = new Light[] { new DirectionalLight() };
Lighting.SetToProgram(lights, shader);

var sky = new Skybox(renderer, ImageLoader.LoadImage("Assets/sky.jpg", ColorComponents.RGB), TextureFilter.Linear);

var textRenderer = new TextRenderer(renderer, renderer.DefaultShaders.GetText());
var font = textRenderer.LoadFont("Assets/font.ttf", 24);
var textStyle = new TextStyle(font, Color.Green, false);

renderer.Enable(EnableCap.DepthTest);


var player = new PlayerController(inputContext, renderer);


var framebuffer = new Framebuffer(renderer, 1280/4, 720/4, TextureFilter.Nearest);

uint width = 0;
uint height = 0;

window.OnResize += (newWidth, newHeight) =>
{
    renderer.Viewport(newWidth, newHeight);
    player.Resize(newWidth, newHeight);
    textRenderer.Resize(newWidth, newHeight);
    framebuffer.Resize(newWidth / 4, newHeight / 4);
    width = newWidth;
    height = newHeight;
};

MeshCollider a = new MeshCollider(MeshGenerator.GenCube());
MeshCollider b = new MeshCollider(MeshGenerator.GenCube());

b.Transformation = Matrix4x4.CreateTranslation(0, -15f, 0) * Matrix4x4.CreateScale(10,1,10);
a.Transformation = Matrix4x4.CreateTranslation(0, -12, 0);

FireballManager.CreateFIreball(renderer, Vector3.Zero, Vector3.UnitX);


while (!window.ShouldClose())
{
    player.Update(window.DeltaTime, new[] { a, b });
    player.Camera.SetVPMatricesToProgram(shader);
    FireballManager.UpdateFireballs(window.DeltaTime);
    window.DoEvents();

    framebuffer.BeginFrame();

    renderer.ClearDepth();

    sky.Draw(player.Camera);

    shader.Bind();
    texture.Bind();
    shader.SetUniform("model", Matrix4x4.CreateTranslation(0, -12, 0));
    vao.Draw();
    shader.SetUniform("model", Matrix4x4.CreateTranslation(0, -15f, 0) * Matrix4x4.CreateScale(10, 1, 10));
    vao.Draw();

    FireballManager.DrawFireballs(player.Camera);

    renderer.Enable(EnableCap.Blend);
    player.DrawUI();

    framebuffer.EndFrame();
    framebuffer.DrawToScreen(width, height);
    textRenderer.DrawText($"X: {player.Position.X:F2}", textStyle, new(10, 10));
    textRenderer.DrawText($"Y: {player.Position.Y:F2}", textStyle, new(10, 35));
    textRenderer.DrawText($"Z: {player.Position.Z:F2}", textStyle, new(10, 60));

    window.SwapBuffers();

    Thread.Sleep(8);
}

vao.Dispose();
texture.Dispose();
shader.Dispose();