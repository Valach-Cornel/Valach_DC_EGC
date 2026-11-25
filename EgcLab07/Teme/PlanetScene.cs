// Varianta C: planeta
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme;

public sealed class PlanetaScene : Scene
{
    private const string NewPlanetAction = "planeta noua";
    private const string ToggleCloudsAction = "arata / ascunde norii";

    private Material _planet = null!;
    private Tween _seedTween = null!;
    private Tween _cloudTween = null!;
    private readonly Random _random = new();

    private bool _cloudsVisible = true;
    private int _planetCount = 1;

    public override string Name => "planeta";

    public override IEnumerable<string> ActionNames => new[]
    {
        NewPlanetAction,
        ToggleCloudsAction,
    };

    public override string Status => $"planeta #{_planetCount} | nori: {(_cloudsVisible ? "pornit" : "oprit")}";

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 0.8f, 4.2f);
        CameraTarget = Vector3.Zero;

        input.Bind(NewPlanetAction, Keys.R);
        input.Bind(ToggleCloudsAction, Keys.Space);

        Mesh sphere = AddMesh(Shapes.ShadedSphere(1.5f, 64, 32));

        // 4 parametri reglabili cu sagetile (Param) + 2 uniforme controlate prin Tween = 6 uniforme proprii (float + vec3)
        _planet = AddMaterial("planeta", "procedural.vert", "planet.frag")
            .Param("uSeaLevel", 0.30f, 0.70f, 0.48f)
            .Param("uNoiseScale", 1.5f, 6.0f, 2.8f)
            .Param("uIceLatitude", 0.55f, 0.98f, 0.78f)
            .Param("uCloudSpeed", 0.0f, 0.5f, 0.08f);

        Vector3 initialSeed = new(1.2f, 3.4f, 5.6f);
        _seedTween = new Tween(initialSeed, initialSeed, 1.2f, Easing.EaseInOut);
        _planet.Uniforms.Set("uSeed", initialSeed);

        _cloudTween = new Tween(new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f), 0.7f, Easing.EaseInOut);
        _planet.Uniforms.Set("uCloudCover", 1.0f);

        SceneObject planetObj = AddObject("planeta", sphere, _planet);
        planetObj.Spin = new Vector3(0.0f, 12.0f, 0.0f);
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        // R genereaza o planeta noua: samanta (vec3) trece lin spre o valoare aleatoare
        if (input.WasPressed(NewPlanetAction))
        {
            _planetCount++;
            Vector3 nextSeed = new(
                _random.NextSingle() * 20.0f - 10.0f,
                _random.NextSingle() * 20.0f - 10.0f,
                _random.NextSingle() * 20.0f - 10.0f
            );
            _seedTween.GoTo(nextSeed, 1.2f);
            ConsolePanel.PrintMessage($"planeta #{_planetCount}");
        }

        // Space ascunde / arata norii lin
        if (input.WasPressed(ToggleCloudsAction))
        {
            _cloudsVisible = !_cloudsVisible;
            float target = _cloudsVisible ? 1.0f : 0.0f;
            _cloudTween.GoTo(new Vector3(target, 0.0f, 0.0f), 0.7f);
            ConsolePanel.PrintMessage($"nori: {(_cloudsVisible ? "pornit" : "oprit")}");
        }

        _seedTween.Update(dt);
        _cloudTween.Update(dt);

        _planet.Uniforms.Set("uSeed", _seedTween.Value);
        _planet.Uniforms.Set("uCloudCover", _cloudTween.Value.X);
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        Vector3 s = _seedTween.Value;
        yield return $"Planeta        : #{_planetCount}   samanta ({s.X:F2}, {s.Y:F2}, {s.Z:F2})";
        yield return $"Nori           : {(_cloudsVisible ? "vizibili" : "ascunsi")} (opacitate {_cloudTween.Value.X:P0})";
    }
}