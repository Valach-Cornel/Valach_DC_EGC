using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class OrbitCamera : Camera
{
    public Vector3 Target { get; private set; }
    public float Distance { get; private set; }
    public float Azimuth { get; private set; }     
    public float Elevation { get; private set; }

    public const float MaxElevation = 89.0f;
    public const float MinDistance = 1.5f;
    public const float MaxDistance = 40.0f;
    public float MouseSensitivity = 0.1f;         
    public float KeySpeed = 90.0f;
    public float ZoomStep = 0.8f;
    public float TargetSpeed = 4.0f; 

    private static readonly Vector3 WorldUp = Vector3.UnitY;

    public OrbitCamera() { Reset(); }

    public override string Name => "Orbita";

    private Vector3 Offset()
    {
        float az = MathHelper.DegreesToRadians(Azimuth);
        float el = MathHelper.DegreesToRadians(Elevation);
        return Distance * new Vector3(MathF.Cos(el) * MathF.Cos(az),
                                      MathF.Sin(el),
                                      MathF.Cos(el) * MathF.Sin(az));
    }

    public override Vector3 Position => Target + Offset();
    public override Vector3 Front => Vector3.Normalize(Target - Position);
    public override Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, WorldUp);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        // rotirea cu mouse-ul
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth += mouseDelta.X * MouseSensitivity;
            Elevation += mouseDelta.Y * MouseSensitivity;
        }

        // rotire cu sagetile
        if (keyboard.IsKeyDown(Keys.Right)) Azimuth += KeySpeed * dt;
        if (keyboard.IsKeyDown(Keys.Left)) Azimuth -= KeySpeed * dt;
        if (keyboard.IsKeyDown(Keys.Up)) Elevation += KeySpeed * dt;
        if (keyboard.IsKeyDown(Keys.Down)) Elevation -= KeySpeed * dt;

        Elevation = MathHelper.Clamp(Elevation, -MaxElevation, MaxElevation);
        
        // schimbarea distantei cu rotita
        Distance = MathHelper.Clamp(Distance - mouse.ScrollDelta.Y * ZoomStep, MinDistance, MaxDistance);

        // BONUS: mutarea tintei cu tastele W A S D
        float az = MathHelper.DegreesToRadians(Azimuth);
        Vector3 forward = new(-MathF.Cos(az), 0f, -MathF.Sin(az)); 
        Vector3 right = new(MathF.Sin(az), 0f, -MathF.Cos(az));

        Vector3 move = Vector3.Zero;
        if (keyboard.IsKeyDown(Keys.W)) move += forward;
        if (keyboard.IsKeyDown(Keys.S)) move -= forward;
        if (keyboard.IsKeyDown(Keys.D)) move += right;
        if (keyboard.IsKeyDown(Keys.A)) move -= right;
        if (move.LengthSquared > 0f)
            Target += Vector3.Normalize(move) * TargetSpeed * dt;
    }

    // BONUS: preluarea pozitiei si a directiei camerei FPS fara salt
    public void SetFromFps(Vector3 position, Vector3 front, float distance = 5.0f)
    {
        Distance = distance;
        Target = position + front * distance;
        Vector3 o = -front;
        Elevation = MathHelper.RadiansToDegrees(MathF.Asin(o.Y));
        Azimuth = MathHelper.RadiansToDegrees(MathF.Atan2(o.Z, o.X));
    }

    public override void Reset()
    {
        Target = new Vector3(0f, 1.0f, 0f);
        Distance = 8.0f;
        Azimuth = 90.0f;
        Elevation = 20.0f;
        FieldOfView = 60.0f;
    }

    public override IEnumerable<string> Describe()
    {
        Vector3 p = Position;
        yield return $"Tinta          : ({Target.X,6:F2}, {Target.Y,6:F2}, {Target.Z,6:F2})";
        yield return $"Distanta       : {Distance,6:F2}";
        yield return $"Azimut / elev. : {Azimuth,7:F1}° / {Elevation,6:F1}°";
        yield return $"Pozitie        : ({p.X,6:F2}, {p.Y,6:F2}, {p.Z,6:F2})";
    }
}