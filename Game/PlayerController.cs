using System;
using System.Numerics;
using MGL.Physics;
using MGL.Graphics;
using MGL.Input;
using MGL.IO;

namespace Game;

public class PlayerController
{
    public float Gravity = 30f;
    public float JumpSpeed = 8f;
    public float PlayerSpeed = 60f;
    public float SprintMultiplier = 2f;
    public float CrouchingMultiplier = 0.5f;
    
    public Vector3 Position = new Vector3(0, 20, 0);
    public Vector3 Velocity = new Vector3();
    public PerspectiveCamera Camera { get; private set; } = new PerspectiveCamera(new(0, 2, 0), Quaternion.Identity, 1280, 720, 80, 0.1f, 1000);
    public MeshCollider Collider { get; private set; }

    public bool IsGrounded = false;

    private InputContext inputContext;
    
    private const float EyeHeight = 1.8f;
    private const float CrouchEyeHeight = 1f;
    private const float CameraSpeed = 50f;
    private bool isCrouching = false;
    private bool isSprinting = false;
    
    private float yaw = 0f, pitch = 0f;
    private Vector2 lastMouse = Vector2.Zero;
    private float sensitivity = 0.12f;
    
    private float bobTimer = 0f;
    private float bobAmount = 0f;
    private const float BobFreqWalk = 14f;
    private const float BobFreqSprint = 18f;
    private const float BobFreqCrouch = 10f;
    private const float BobAmpVert = 0.08f;
    private const float BobAmpHoriz = 0.04f;
    private float breathing = 0f;

    private Sprite _hand1;
    private Sprite _hand2;
    private Renderer _renderer;

    private float fireballCooldown = 0.5f;
    private float fireballTimer = 0f;
    private bool isFiring = false;
    
    private const float ShootAnimationDuration = 0.3f;
    private float shootAnimationTimer = 0f;
    
    private MeshCollider _standCollider = new MeshCollider(new Vector3[]
    {
        new(0.25f, 0f, 0.25f),
        new(-0.25f, 0f, 0.25f),
        new(0.25f, 0f, -0.25f),
        new(-0.25f, 0f, -0.25f),
        
        new(0.25f, EyeHeight, 0.25f),
        new(-0.25f, EyeHeight, 0.25f),
        new(0.25f, EyeHeight, -0.25f),
        new(-0.25f, EyeHeight, -0.25f)
    });

    private MeshCollider _crouchCollider = new MeshCollider(new Vector3[]
    {
        new(0.25f, 0f, 0.25f),
        new(-0.25f, 0f, 0.25f),
        new(0.25f, 0f, -0.25f),
        new(-0.25f, 0f, -0.25f),
        
        new(0.25f, CrouchEyeHeight, 0.25f),
        new(-0.25f, CrouchEyeHeight, 0.25f),
        new(0.25f, CrouchEyeHeight, -0.25f),
        new(-0.25f, CrouchEyeHeight, -0.25f)
    });
    
    public PlayerController(InputContext inputContext, Renderer renderer)
    {
        _renderer = renderer;
        this.inputContext = inputContext;
        this.inputContext.SetMouseMode(MouseMode.Disabled);
        inputContext.MouseMove += pos =>
        {
            Vector2 delta = pos - lastMouse;
            lastMouse = pos;

            float sens = MathF.PI / 180 * sensitivity;
            
            yaw += delta.X * sens;
            pitch += delta.Y * sens;
            
            float limit = MathF.PI / 2f - 0.01f;
            pitch = Math.Clamp(pitch, -limit, limit);

            Camera.Rotation = Quaternion.CreateFromYawPitchRoll(0, pitch, 0) * Quaternion.CreateFromYawPitchRoll(yaw, 0, 0);
        };
        _hand1 = new Sprite(renderer, ImageLoader.LoadImage("Assets/hand1.png", ColorComponents.RGBA), TextureFilter.Nearest);
        _hand2 = new Sprite(renderer, ImageLoader.LoadImage("Assets/hand2.png", ColorComponents.RGBA), TextureFilter.Nearest);
    }

    public void Update(double deltaTime, ISupportable[] scene)
    {
        float dt = (float)deltaTime / 1000f;
        breathing += dt * 2;
        
        fireballTimer -= dt;
        shootAnimationTimer -= dt;
        
        isCrouching = inputContext.IsKeyPressed(KeyboardKey.ControlLeft);
        isSprinting = inputContext.IsKeyPressed(KeyboardKey.ShiftLeft);
        Collider = isCrouching ? _crouchCollider : _standCollider;
        
        if (IsGrounded && inputContext.IsKeyPressed(KeyboardKey.Space))
            Velocity.Y = JumpSpeed;

        Velocity.X *= 0.9f;
        Velocity.Z *= 0.9f;
        
        Velocity.Y -= Gravity * dt;
        Position += Velocity * dt;

        Vector3 forward = Vector3.Transform(-Vector3.UnitZ, Camera.Rotation);
        Vector3 forwardFlat = Vector3.Normalize(new(-forward.X, 0, forward.Z));
        
        Vector3 right = Vector3.Transform(Vector3.UnitX, Camera.Rotation);
        Vector3 rightFlat = Vector3.Normalize(new(-right.X, 0, right.Z));
        
        Vector3 up = Vector3.Transform(Vector3.UnitX, Camera.Rotation);
        Vector3 upFlat = Vector3.Normalize(new(-up.X, 0, up.Z));

        Vector3 moveDir = Vector3.Zero;
        if (inputContext.IsKeyPressed(KeyboardKey.W)) moveDir += forwardFlat;
        if (inputContext.IsKeyPressed(KeyboardKey.S)) moveDir -= forwardFlat;
        if (inputContext.IsKeyPressed(KeyboardKey.D)) moveDir -= rightFlat;
        if (inputContext.IsKeyPressed(KeyboardKey.A)) moveDir += rightFlat;
        if (inputContext.GetMouseButton(MouseButton.Left)
            && fireballTimer <= 0f)
        {
            Quaternion camWorldRot = Quaternion.Conjugate(Camera.Rotation);

            Vector3 cameraForward = Vector3.Transform(-Vector3.UnitZ, camWorldRot);
            Vector3 cameraRight = Vector3.Transform(Vector3.UnitX, camWorldRot);
            Vector3 cameraUp = Vector3.Transform(Vector3.UnitY, camWorldRot);

            Vector3 handOffset =
                cameraForward * 0.7f +
                cameraRight * 0.4f -
                cameraUp * 0.4f;

            Vector3 handPosition = Position
                                   + new Vector3(0, isCrouching ? CrouchEyeHeight : EyeHeight, 0)
                                   + handOffset;

            Vector3 lookDir = Vector3.Normalize(cameraForward);

            FireballManager.CreateFIreball(_renderer, handPosition, lookDir);

            fireballTimer = fireballCooldown;
            shootAnimationTimer = ShootAnimationDuration;
        }
        else
        {
            isFiring = false;
        }

        if (moveDir != Vector3.Zero)
        {
            moveDir = Vector3.Normalize(moveDir);
            float currentMultiplier = isCrouching ? CrouchingMultiplier : (isSprinting ? SprintMultiplier : 1f);
            Velocity += moveDir * PlayerSpeed * currentMultiplier * dt;
        }
        
        IsGrounded = false;

        for (int pass = 0; pass < 4; pass++)
        {
            bool pushed = false;

            foreach (var other in scene)
            {
                Collider.Transformation = Matrix4x4.CreateTranslation(Position);

                if (!GJK.Intersect(Collider, other, out var s)) continue;
                if (!EPA.TryGetPenetration(Collider, other, s, out var n, out var d)) continue;
                
                Position -= n * d;
                pushed = true;

                if (n.Y < -0.7f)
                {
                    IsGrounded = true;
                    if (Velocity.Y < 0) Velocity.Y = 0;
                }
                else if (n.Y > 0.7f)
                {
                    if (Velocity.Y > 0) Velocity.Y = 0;
                }
            }

            if (!pushed) break;
        }

        Collider.Transformation = Matrix4x4.CreateTranslation(Position);

        Vector3 cameraTarget = Position + new Vector3(0, isCrouching ? CrouchEyeHeight : EyeHeight, 0);

        bool isMoving = moveDir != Vector3.Zero && IsGrounded;
        float targetBobAmount = isMoving ? 1f : 0f;

        bobAmount += (targetBobAmount - bobAmount) * 10f * dt;

        if (bobAmount > 0.001f)
        {
            float freq = isCrouching ? BobFreqCrouch : (isSprinting ? BobFreqSprint : BobFreqWalk);
            float ampMult = isCrouching ? 0.5f : (isSprinting ? 1.3f : 1f);

            bobTimer += (isMoving ? freq : BobFreqWalk) * dt;

            float verticalOffset = MathF.Sin(bobTimer) * BobAmpVert * ampMult * bobAmount;
            float horizontalOffset = MathF.Cos(bobTimer * 0.5f) * BobAmpHoriz * ampMult * bobAmount;

            cameraTarget += new Vector3(0, verticalOffset, 0) + (rightFlat * horizontalOffset);
        }
        else
        {
            bobTimer = 0f;
        }

        Camera.Position = Vector3.Lerp(Camera.Position, cameraTarget + new Vector3(0, MathF.Sin(breathing + MathF.PI/ 2) / 100, 0), 1f - MathF.Exp(-CameraSpeed * dt));
    }

    public void DrawUI()
    {
        Quaternion camWorldRot = Quaternion.Conjugate(Camera.Rotation); 

        Vector3 forward = Vector3.Transform(-Vector3.UnitZ, camWorldRot);
        Vector3 right   = Vector3.Transform( Vector3.UnitX, camWorldRot);
        Vector3 up      = Vector3.Transform( Vector3.UnitY, camWorldRot);

        Vector3 handOffset = forward * 0.5f + right * 0.3f - up * 0.25f;
        Vector3 handPosition = Position + handOffset + new Vector3(0, MathF.Sin(breathing) / 100, 0);

        
        Matrix4x4 worldMatrix = Matrix4x4.CreateScale(0.2f) *
                                Matrix4x4.CreateFromQuaternion(camWorldRot) *
                                Matrix4x4.CreateTranslation(handPosition + new Vector3(0, isCrouching ? CrouchEyeHeight : EyeHeight, 0));

        if (shootAnimationTimer > 0f)
            _hand2.Draw(Camera, worldMatrix);
        else
            _hand1.Draw(Camera, worldMatrix);
        
        
    }
    public void Resize(uint w, uint h)
    {
        Camera.Width = w;
        Camera.Height = h;
    }
}