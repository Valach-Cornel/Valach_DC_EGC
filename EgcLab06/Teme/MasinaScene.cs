using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme
{
    public sealed class MasinaScene : Scene
    {
        private const string RestartAction = "reporneste animatia";

        private SceneObject _carRoot = null!;
        private Tween _carTween = null!;
        private readonly List<SceneObject> _wheels = new();

        public override string Name => "Masina cu bila";

        public override string Status => $"x = {_carRoot.Transform.Position.X:F1} | progres = {_carTween.Progress:P0}";

        public override IEnumerable<string> ActionNames => new[] { RestartAction };

        public override void Load(Input input)
        {
            CameraPosition = new Vector3(0.0f, 5.0f, 10.0f);
            CameraTarget = new Vector3(0.0f, 1.0f, 0.0f);

            input.Bind(RestartAction, Keys.R);

            Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.30f, 0.33f, 0.38f, 1.0f)));
            Mesh cube = AddMesh(Shapes.ShadedCube());
            Mesh sphere = AddMesh(Shapes.ShadedSphere(1.0f, 32, 16));
            Mesh cylinder = AddMesh(Shapes.ShadedCylinder(0.4f, 0.25f, 24));

            AddObject("podea", grid);

            // Drumul pe care merge masina
            SceneObject road = AddObject("drum", cube);
            road.Transform.Position = new Vector3(0.0f, 0.01f, 0.0f);
            road.Transform.Scale = new Vector3(16.0f, 0.02f, 2.8f);
            road.Tint = new Color4(0.22f, 0.22f, 0.25f, 1.0f);

            // Pivotul principal al masinii
            _carRoot = AddObject("masina_pivot", null);

            // Caroseria (baza masinii)
            SceneObject chassis = AddObject("caroserie", cube, _carRoot);
            chassis.Transform.Position = new Vector3(0.0f, 0.55f, 0.0f);
            chassis.Transform.Scale = new Vector3(2.4f, 0.5f, 1.2f);
            chassis.Tint = new Color4(0.85f, 0.2f, 0.2f, 1.0f);

            // Cabina (partea de sus)
            SceneObject cabin = AddObject("cabina", cube, _carRoot);
            cabin.Transform.Position = new Vector3(-0.15f, 1.0f, 0.0f);
            cabin.Transform.Scale = new Vector3(1.2f, 0.45f, 1.0f);
            cabin.Tint = new Color4(0.25f, 0.6f, 0.95f, 1.0f);

            // Cele 4 roti
            Vector3[] wheelOffsets =
            {
                new(-0.75f, 0.4f,  0.65f),
                new( 0.75f, 0.4f,  0.65f),
                new(-0.75f, 0.4f, -0.65f),
                new( 0.75f, 0.4f, -0.65f),
            };

            foreach (Vector3 offset in wheelOffsets)
            {
                // Pivot pentru orientarea cilindrului pe axa Z
                SceneObject wheelMount = AddObject("suport_roata", null, _carRoot);
                wheelMount.Transform.Position = offset;
                wheelMount.Transform.Rotation = new Vector3(90.0f, 0.0f, 0.0f);

                SceneObject wheel = AddObject("roata", cylinder, wheelMount);
                wheel.Tint = new Color4(0.2f, 0.2f, 0.22f, 1.0f);
                wheel.Spin = new Vector3(0.0f, -240.0f, 0.0f);
                _wheels.Add(wheel);
            }

            // Bila care se invarte in jurul masinii
            SceneObject ballOrbit = AddObject("orbita_bilei", null, _carRoot);
            ballOrbit.Transform.Position = new Vector3(0.0f, 1.1f, 0.0f);
            ballOrbit.Spin = new Vector3(0.0f, 140.0f, 0.0f);

            SceneObject ball = AddObject("bila", sphere, ballOrbit);
            ball.Transform.Position = new Vector3(2.2f, 0.0f, 0.0f);
            ball.Transform.Scale = new Vector3(0.35f);
            ball.Tint = new Color4(1.0f, 0.85f, 0.2f, 1.0f);
            ball.Spin = new Vector3(0.0f, 180.0f, 0.0f);

            // Animatia masinii
            _carTween = new Tween(
                new Vector3(-5.5f, 0.0f, 0.0f),
                new Vector3(5.5f, 0.0f, 0.0f),
                duration: 4.0f,
                ease: Easing.EaseInOut,
                pingPong: true
            );

            _carRoot.Transform.Position = _carTween.From;
        }

        public override void Update(float dt, Input input)
        {
            base.Update(dt, input);

            if (input.WasPressed(RestartAction))
            {
                _carTween.Restart();
                ConsolePanel.PrintMessage("animatia masinii a repornit");
            }

            float prevX = _carRoot.Transform.Position.X;
            _carTween.Update(dt);
            _carRoot.Transform.Position = _carTween.Value;

            // Sensul de rotatie al rotilor se adapteaza directiei de mers
            float dir = _carRoot.Transform.Position.X >= prevX ? -1.0f : 1.0f;
            foreach (SceneObject wheel in _wheels)
            {
                wheel.Spin = new Vector3(0.0f, dir * 240.0f, 0.0f);
            }
        }

        public override IEnumerable<string> Describe()
        {
            foreach (string line in base.Describe()) yield return line;
            yield return $"Masina         : x = {_carRoot.Transform.Position.X:F2}, progres = {_carTween.Progress:F2}";
        }
    }
}