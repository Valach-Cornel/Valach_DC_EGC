using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc.Teme
{
    public sealed class ObstacoleScene : Scene
    {
        private const string RestartAction = "joc nou";

        private const float LaneWidth = 2.2f;
        private const float PlayerZ = 2.5f;
        private const float SpawnZ = -38.0f;
        private const float DespawnZ = 8.0f;
        private const float LaneSwitchDuration = 0.15f;

        private const float Gravity = 24.0f;
        private const float JumpSpeed = 10.0f;
        private const float GroundY = 0.5f;

        private static readonly Vector3 PlayerScale = new(0.9f, 1.0f, 0.9f);
        private static readonly Vector3 LowObstacleScale = new(1.5f, 0.9f, 0.9f);
        private static readonly Vector3 TallObstacleScale = new(1.5f, 2.5f, 0.9f);

        private SceneObject _player = null!;
        private SceneObject _shadow = null!;
        private readonly List<SceneObject> _obstacles = new();
        private readonly List<SceneObject> _stripes = new();

        private int _currentLane = 0;
        private Tween _laneTween = null!;

        private float _playerY = GroundY;
        private float _verticalSpeed = 0.0f;
        private bool OnGround => _playerY <= GroundY + 1e-4f;

        private readonly Random _rng = new();
        private float _spawnTimer = 0.0f;
        private float _stripeOffset = 0.0f;
        private const float StripeSpacing = 4.0f;

        private float _score = 0.0f;
        private float _bestScore = 0.0f;
        private bool _gameOver = false;

        public override string Name => "Evita obstacolele";

        public override string Status => _gameOver
            ? $"COLIZIUNE! Scor: {_score:F1} s | Record: {_bestScore:F1} s - Apasa R pentru joc nou"
            : $"Scor: {_score:F1} s | Record: {_bestScore:F1} s | Viteza: {WorldSpeed:F1} u/s";

        public override IEnumerable<string> ActionNames => new[]
        {
            Actions.Left, Actions.Right, Actions.Jump, RestartAction,
        };

        private float WorldSpeed => 11.0f + _score * 0.55f;
        private float SpawnInterval => MathF.Max(0.40f, 1.30f - _score * 0.025f);

        public override void Load(Input input)
        {
            CameraPosition = new Vector3(0.0f, 5.5f, 9.5f);
            CameraTarget = new Vector3(0.0f, 1.0f, -6.0f);

            input.Bind(Actions.Left, Keys.A, Keys.Left);
            input.Bind(Actions.Right, Keys.D, Keys.Right);
            input.Bind(Actions.Jump, Keys.Space);
            input.Bind(RestartAction, Keys.R);

            Mesh cube = AddMesh(Shapes.ShadedCube());
            Mesh disk = AddMesh(Shapes.ShadedCylinder(0.5f, 0.02f, 24));

            // Drumul si bordurile
            SceneObject road = AddObject("drum", cube);
            road.Transform.Position = new Vector3(0.0f, -0.05f, -15.0f);
            road.Transform.Scale = new Vector3(LaneWidth * 3.2f, 0.1f, 50.0f);
            road.Tint = new Color4(0.22f, 0.22f, 0.25f, 1.0f);

            for (int side = -1; side <= 1; side += 2)
            {
                SceneObject border = AddObject("bordura", cube);
                border.Transform.Position = new Vector3(side * (LaneWidth * 1.6f), 0.1f, -15.0f);
                border.Transform.Scale = new Vector3(0.15f, 0.2f, 50.0f);
                border.Tint = new Color4(0.5f, 0.5f, 0.55f, 1.0f);
            }

            // Marcajele dintre benzi
            int stripeRows = (int)((DespawnZ - SpawnZ) / StripeSpacing) + 1;
            for (int i = 0; i < stripeRows; i++)
            {
                for (int divider = -1; divider <= 1; divider += 2)
                {
                    SceneObject stripe = AddObject($"marcaj {i}", cube);
                    stripe.Transform.Scale = new Vector3(0.12f, 0.02f, 1.6f);
                    stripe.Tint = new Color4(0.9f, 0.9f, 0.9f, 1.0f);
                    _stripes.Add(stripe);
                }
            }

            _shadow = AddObject("umbra jucatorului", disk);
            _shadow.Tint = new Color4(0.05f, 0.05f, 0.08f, 1.0f);

            _player = AddObject("jucator", cube);
            _player.Transform.Scale = PlayerScale;

            for (int i = 0; i < 12; i++)
            {
                SceneObject obs = AddObject($"obstacol {i}", cube);
                obs.Visible = false;
                _obstacles.Add(obs);
            }

            ResetGame();
        }

        public override void Update(float dt, Input input)
        {
            base.Update(dt, input);

            if (input.WasPressed(RestartAction))
            {
                ResetGame();
                ConsolePanel.PrintMessage("joc nou");
                return;
            }

            if (_gameOver) return;

            _score += dt;
            float speed = WorldSpeed;

            // Schimbarea benzii cu Tween si EaseOut
            int step = (input.WasPressed(Actions.Right) ? 1 : 0) - (input.WasPressed(Actions.Left) ? 1 : 0);
            if (step != 0)
            {
                int nextLane = Math.Clamp(_currentLane + step, -1, 1);
                if (nextLane != _currentLane)
                {
                    _currentLane = nextLane;
                    _laneTween.GoTo(new Vector3(_currentLane * LaneWidth, 0.0f, 0.0f), LaneSwitchDuration);
                }
            }
            _laneTween.Update(dt);
            float playerX = _laneTween.Value.X;

            // Salt cu gravitatie
            if (OnGround && input.WasPressed(Actions.Jump))
            {
                _verticalSpeed = JumpSpeed;
            }

            if (!OnGround || _verticalSpeed > 0.0f)
            {
                _verticalSpeed -= Gravity * dt;
                _playerY += _verticalSpeed * dt;
                if (_playerY <= GroundY)
                {
                    _playerY = GroundY;
                    _verticalSpeed = 0.0f;
                }
            }

            _player.Transform.Position = new Vector3(playerX, _playerY, PlayerZ);

            float height = _playerY - GroundY;
            float shadowSize = 1.1f / (1.0f + 0.25f * height);
            _shadow.Transform.Position = new Vector3(playerX, 0.01f, PlayerZ);
            _shadow.Transform.Scale = new Vector3(shadowSize, 1.0f, shadowSize);

            _stripeOffset = (_stripeOffset + speed * dt) % StripeSpacing;
            UpdateStripes();

            _spawnTimer -= dt;
            if (_spawnTimer <= 0.0f)
            {
                _spawnTimer = SpawnInterval;
                SpawnObstacle();
            }

            // Deplasare obstacole si verificare coliziuni AABB
            foreach (SceneObject obs in _obstacles)
            {
                if (!obs.Visible) continue;

                obs.Transform.Position += new Vector3(0.0f, 0.0f, speed * dt);

                if (AabbOverlap(_player, obs))
                {
                    _gameOver = true;
                    _bestScore = MathF.Max(_bestScore, _score);
                    _player.Tint = new Color4(1.0f, 0.2f, 0.2f, 1.0f);
                    ConsolePanel.PrintMessage($"Coliziune! Scor: {_score:F1} s | Record: {_bestScore:F1} s");
                    return;
                }

                if (obs.Transform.Position.Z > DespawnZ)
                {
                    obs.Visible = false;
                }
            }
        }

        private void SpawnObstacle()
        {
            SceneObject? obs = _obstacles.FirstOrDefault(o => !o.Visible);
            if (obs == null) return;

            int lane = _rng.Next(-1, 2);
            bool isTall = _rng.NextDouble() < 0.45;

            Vector3 scale = isTall ? TallObstacleScale : LowObstacleScale;
            obs.Transform.Scale = scale;
            obs.Transform.Position = new Vector3(lane * LaneWidth, scale.Y * 0.5f, SpawnZ);
            obs.Tint = isTall
                ? new Color4(0.85f, 0.25f, 0.25f, 1.0f)
                : new Color4(0.95f, 0.75f, 0.20f, 1.0f);
            obs.Visible = true;
        }

        // Suprapunere pe toate cele 3 axe
        private static bool AabbOverlap(SceneObject a, SceneObject b)
        {
            Vector3 minA = a.Transform.Position - a.Transform.Scale * 0.46f;
            Vector3 maxA = a.Transform.Position + a.Transform.Scale * 0.46f;

            Vector3 minB = b.Transform.Position - b.Transform.Scale * 0.48f;
            Vector3 maxB = b.Transform.Position + b.Transform.Scale * 0.48f;

            return minA.X <= maxB.X && maxA.X >= minB.X &&
                   minA.Y <= maxB.Y && maxA.Y >= minB.Y &&
                   minA.Z <= maxB.Z && maxA.Z >= minB.Z;
        }

        private void UpdateStripes()
        {
            int idx = 0;
            int rows = _stripes.Count / 2;
            for (int i = 0; i < rows; i++)
            {
                float z = SpawnZ + i * StripeSpacing + _stripeOffset;
                for (int divider = -1; divider <= 1; divider += 2)
                {
                    _stripes[idx].Transform.Position = new Vector3(divider * (LaneWidth * 0.5f), 0.01f, z);
                    idx++;
                }
            }
        }

        private void ResetGame()
        {
            _gameOver = false;
            _score = 0.0f;
            _spawnTimer = 0.6f;

            _currentLane = 0;
            _laneTween = new Tween(Vector3.Zero, Vector3.Zero, LaneSwitchDuration, Easing.EaseOut);

            _playerY = GroundY;
            _verticalSpeed = 0.0f;

            _player.Transform.Position = new Vector3(0.0f, GroundY, PlayerZ);
            _player.Tint = new Color4(0.3f, 0.85f, 1.0f, 1.0f);

            _shadow.Transform.Position = new Vector3(0.0f, 0.01f, PlayerZ);
            _shadow.Transform.Scale = new Vector3(1.1f, 1.0f, 1.1f);

            foreach (SceneObject obs in _obstacles)
            {
                obs.Visible = false;
            }

            UpdateStripes();
        }

        public override IEnumerable<string> Describe()
        {
            foreach (string line in base.Describe()) yield return line;
            yield return $"Scor / Record  : {_score:F2} s / {_bestScore:F2} s (viteza lumii: {WorldSpeed:F1})";
            yield return $"Jucator        : banda {_currentLane}, x = {_player.Transform.Position.X:F2}, y = {_playerY:F2}";
        }
    }
}