using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Poziția, rotația și scalarea unui obiect din scenă. Din ele se construiește matricea model,
/// care duce vârfurile obiectului din object space în world space (secțiunea 5).
/// </summary>
public class Transform2D
{
    public Vector2 Position = Vector2.Zero;   // translația, în unități de lume
    public float Rotation = 0.0f;             // unghiul, în GRADE, pozitiv = sens trigonometric
    public Vector2 Scale = new Vector2(1.0f, 1.0f);       // factorii de scalare pe x și pe y
    public float Depth = 0.0f;                // coordonata z; contează doar în perspectivă (secțiunea 7.2)

    public Matrix4 ModelMatrix =>
        // TODO (T1): scalare, apoi rotație, apoi translație
        // Daca translatia s-ar fi aplicat inaintea rotatiei atunci ar muta tot obiectul pe un arc de cerc in jurul originii
        // in loc sa-l roteasca pe loc in jurul propriului centru
        Matrix4.CreateScale(Scale.X, Scale.Y, 1) 
        * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation))
        * Matrix4.CreateTranslation(Position.X, Position.Y, Depth);

    /// <summary>O copie independentă (folosită pentru readucerea scenei în starea inițială).</summary>
    public Transform2D Clone() => (Transform2D)MemberwiseClone();
}
