using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PnceHarekat
{
    public enum GameState
    {
        MainMenu,
        Playing,
        LevelUp,
        Paused,
        GameOver
    }

    public enum GameDifficulty
    {
        Easy,
        Normal
    }

    /// <summary>
    /// Central brain for the Pençe Harekatı prototype. The class takes the
    /// information from the HTML prototype and recreates the systems inside
    /// Unity in a runtime driven fashion so that the repository stays lean.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        public Rect PlayArea => playArea;
        public GameState State => state;
        public GameDifficulty Difficulty => difficulty;

        private readonly Rect playArea = new Rect(-18f, -10f, 36f, 20f);

        private readonly List<EnemyController> enemies = new();
        private readonly List<ProjectileController> projectiles = new();
        private readonly List<XpOrbController> xpOrbs = new();
        private readonly Dictionary<string, float> weaponDamageLedger = new();
        private readonly Dictionary<string, string> weaponNames = new();
        private readonly HashSet<string> ownedWeapons = new();

        private UIController ui;
        private PlayerController player;
        private Transform spriteRoot;

        private float enemySpawnTimer;
        private float enemySpawnInterval = 2.5f;
        private float elapsedTime;
        private int score;
        private int money;
        private int flags;
        private int wave;

        private GameState state = GameState.MainMenu;
        private GameDifficulty difficulty = GameDifficulty.Normal;

        private readonly System.Random rng = new();

        private readonly List<UpgradeDefinition> upgradePool = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public Transform SpriteRoot => spriteRoot;

        private void Start()
        {
            Application.targetFrameRate = 60;
            Cursor.visible = true;
            EnsureSpriteRoot();
            BuildBackground();
            CreateEventSystemIfNeeded();
            ui = UIController.Create(this);
            BuildUpgradePool();
            ui.ShowMainMenu();
        }

        private void Update()
        {
            if (state == GameState.Playing)
            {
                UpdatePlaying();
            }

            if (state == GameState.Playing || state == GameState.MainMenu)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    TogglePause();
                }
            }

            if (state == GameState.Playing && Input.GetKeyDown(KeyCode.M))
            {
                ui.ToggleMapOverlay();
            }
        }

        private void UpdatePlaying()
        {
            elapsedTime += Time.deltaTime;
            enemySpawnTimer += Time.deltaTime;

            float intervalModifier = difficulty == GameDifficulty.Easy ? 1.4f : 1f;
            if (enemySpawnTimer >= enemySpawnInterval * intervalModifier)
            {
                enemySpawnTimer = 0f;
                SpawnEnemyWave();
            }

            ui.UpdateHud(elapsedTime, score, money, flags);
        }

        private void EnsureSpriteRoot()
        {
            if (spriteRoot != null)
            {
                return;
            }

            var existing = transform.Find("Sprites");
            if (existing != null)
            {
                spriteRoot = existing;
            }
            else
            {
                var go = new GameObject("Sprites");
                spriteRoot = go.transform;
                spriteRoot.SetParent(transform);
            }

            spriteRoot.localPosition = Vector3.zero;
            spriteRoot.localRotation = Quaternion.identity;
            spriteRoot.localScale = Vector3.one;
        }

        private void BuildBackground()
        {
            var background = new GameObject("Background");
            background.transform.SetParent(spriteRoot);
            background.transform.position = new Vector3(playArea.center.x, playArea.center.y, 5f);
            var sr = background.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteLibrary.LoadSprite("Sprites/background_tile");
            sr.color = Color.white;
            sr.sortingOrder = -10;

            if (sr.sprite != null)
            {
                float spriteWidth = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                float spriteHeight = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                background.transform.localScale = new Vector3(
                    (playArea.width + 6f) / spriteWidth,
                    (playArea.height + 6f) / spriteHeight,
                    1f);
            }
            else
            {
                background.transform.localScale = new Vector3(playArea.width + 4f, playArea.height + 4f, 1f);
            }

            var grid = new GameObject("BackgroundGrid");
            grid.transform.SetParent(background.transform, worldPositionStays: false);
            var line = grid.AddComponent<LineRenderer>();
            line.positionCount = 0;
            var shader = Shader.Find("Sprites/Default");
            line.material = shader != null ? new Material(shader) : null;
            line.widthMultiplier = 0.02f;
            line.startColor = new Color(0f, 0.6f, 0.3f, 0.2f);
            line.endColor = line.startColor;

            var points = new List<Vector3>();
            for (float x = playArea.xMin; x <= playArea.xMax; x += 2f)
            {
                points.Add(new Vector3(x, playArea.yMin, 0f));
                points.Add(new Vector3(x, playArea.yMax, 0f));
            }
            for (float y = playArea.yMin; y <= playArea.yMax; y += 2f)
            {
                points.Add(new Vector3(playArea.xMin, y, 0f));
                points.Add(new Vector3(playArea.xMax, y, 0f));
            }

            line.positionCount = points.Count;
            line.useWorldSpace = false;
            if (points.Count > 0)
            {
                var offset = new Vector3(-playArea.center.x, -playArea.center.y, -0.5f);
                line.SetPositions(points.Select(p => p + offset).ToArray());
            }
        }

        private void CreateEventSystemIfNeeded()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        #region Public API used by other components

        public void RegisterEnemy(EnemyController enemy) => enemies.Add(enemy);
        public void UnregisterEnemy(EnemyController enemy) => enemies.Remove(enemy);
        public IReadOnlyList<EnemyController> ActiveEnemies => enemies;

        public void RegisterProjectile(ProjectileController projectile) => projectiles.Add(projectile);
        public void UnregisterProjectile(ProjectileController projectile) => projectiles.Remove(projectile);

        public void RegisterXpOrb(XpOrbController orb) => xpOrbs.Add(orb);
        public void UnregisterXpOrb(XpOrbController orb) => xpOrbs.Remove(orb);

        public void RegisterWeaponName(string id, string displayName)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            weaponNames[id] = displayName;
        }

        public void ReportDamage(string weaponId, float amount)
        {
            if (string.IsNullOrEmpty(weaponId))
            {
                return;
            }

            string friendlyName = weaponNames.TryGetValue(weaponId, out var display) ? display : weaponId;
            weaponDamageLedger.TryGetValue(friendlyName, out float current);
            weaponDamageLedger[friendlyName] = current + amount;
        }

        public void UpdatePlayerHealth(float current, float maximum)
        {
            ui.UpdateHealth(current, maximum);
        }

        public void UpdatePlayerLevel(float currentXp, float requiredXp, int level)
        {
            ui.UpdateXp(currentXp, requiredXp, level);
        }

        public void UpdateDashFuel(float ratio)
        {
            ui.UpdateDashFuel(ratio);
        }

        public void UpdateShieldCharge(float ratio)
        {
            ui.UpdateShieldCharge(ratio);
        }

        public void NotifyPlayerDeath()
        {
            if (state == GameState.GameOver)
            {
                return;
            }

            EndGame(false);
        }

        public void NotifyFlagCaptured()
        {
            flags++;
            ui.UpdateHud(elapsedTime, score, money, flags);
        }

        public void AddMoney(int amount) => money += amount;
        public void AddScore(int amount) => score += amount;

        public void OnEnemyKilled(EnemyController enemy, int reward, float xpAmount)
        {
            AddScore(reward);
            AddMoney(Mathf.Max(1, reward / 2));
            if (rng.NextDouble() < 0.05)
            {
                flags++;
            }
            SpawnXpOrb(enemy.transform.position, xpAmount);
            ui.UpdateHud(elapsedTime, score, money, flags);
        }

        public void OnPlayerLevelUp()
        {
            if (state != GameState.Playing)
            {
                return;
            }

            state = GameState.LevelUp;
            player.SetInputEnabled(false);
            ui.ShowLevelUp(GenerateUpgradeOptions());
        }

        public void ApplyUpgrade(UpgradeDefinition upgrade)
        {
            upgrade.Apply?.Invoke();
            ui.HideLevelUp();
            if (state == GameState.LevelUp)
            {
                ResumeGameplay();
            }
        }

        public void SkipUpgrade()
        {
            ui.HideLevelUp();
            ResumeGameplay();
        }

        public void TogglePause()
        {
            if (state == GameState.MainMenu || state == GameState.GameOver)
            {
                return;
            }

            if (state == GameState.Paused)
            {
                ResumeGameplay();
            }
            else if (state == GameState.Playing)
            {
                state = GameState.Paused;
                Time.timeScale = 0f;
                ui.ShowPauseMenu();
            }
        }

        public void ResumeGameplay()
        {
            if (state == GameState.Paused)
            {
                ui.HidePauseMenu();
            }

            state = GameState.Playing;
            Time.timeScale = 1f;
            if (player != null)
            {
                player.SetInputEnabled(true);
            }
        }

        public void RestartGame()
        {
            foreach (var enemy in enemies.ToArray())
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            enemies.Clear();

            foreach (var proj in projectiles.ToArray())
            {
                if (proj != null)
                {
                    Destroy(proj.gameObject);
                }
            }
            projectiles.Clear();

            foreach (var orb in xpOrbs.ToArray())
            {
                if (orb != null)
                {
                    Destroy(orb.gameObject);
                }
            }
            xpOrbs.Clear();

            if (player != null)
            {
                Destroy(player.gameObject);
                player = null;
            }

            weaponDamageLedger.Clear();
            weaponNames.Clear();
            ownedWeapons.Clear();
            elapsedTime = 0f;
            enemySpawnTimer = 0f;
            score = 0;
            money = 0;
            flags = 0;
            wave = 0;

            StartGame(difficulty);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            state = GameState.MainMenu;
            if (player != null)
            {
                Destroy(player.gameObject);
                player = null;
            }
            foreach (var enemy in enemies.ToArray())
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
            enemies.Clear();
            foreach (var proj in projectiles.ToArray())
            {
                if (proj != null)
                {
                    Destroy(proj.gameObject);
                }
            }
            projectiles.Clear();
            foreach (var orb in xpOrbs.ToArray())
            {
                if (orb != null)
                {
                    Destroy(orb.gameObject);
                }
            }
            xpOrbs.Clear();
            weaponDamageLedger.Clear();
            weaponNames.Clear();
            ownedWeapons.Clear();
            ui.ShowMainMenu();
        }

        public void StartGame(GameDifficulty selectedDifficulty)
        {
            EnsureSpriteRoot();
            difficulty = selectedDifficulty;
            state = GameState.Playing;
            Time.timeScale = 1f;
            elapsedTime = 0f;
            enemySpawnTimer = 0f;
            score = 0;
            money = 0;
            flags = 0;
            wave = 0;
            weaponDamageLedger.Clear();
            weaponNames.Clear();
            ownedWeapons.Clear();

            if (player == null)
            {
                var playerGo = new GameObject("Player");
                playerGo.transform.SetParent(spriteRoot, worldPositionStays: false);
                player = playerGo.AddComponent<PlayerController>();
            }
            player.transform.position = playArea.center;
            player.Initialise(this);
            ownedWeapons.Add("defaultGun");
            RegisterWeaponName("defaultGun", "Ana Silah");

            if (Camera.main != null)
            {
                Camera.main.orthographicSize = 11f;
                Camera.main.transform.position = new Vector3(playArea.center.x, playArea.center.y, -10f);
                Camera.main.backgroundColor = new Color(0.02f, 0.05f, 0.04f);
            }

            ui.HideAllScreens();
            ui.UpdateHud(elapsedTime, score, money, flags);
            ui.RefreshWeaponSlots(player.GetWeapons());
        }

        public void EndGame(bool didWin)
        {
            state = GameState.GameOver;
            Time.timeScale = 1f;
            if (player != null)
            {
                player.SetInputEnabled(false);
            }
            ui.ShowGameOver(didWin, score, elapsedTime, weaponDamageLedger);
        }

        #endregion

        #region Gameplay helpers

        private void SpawnEnemyWave()
        {
            wave++;
            int baseCount = difficulty == GameDifficulty.Easy ? 2 : 3;
            int count = baseCount + wave / 3;
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            var archetype = EnemyArchetype.Sample(difficulty, wave, rng);
            Vector2 spawnPos = GetRandomSpawnPosition();
            var go = new GameObject("Enemy " + archetype.Name);
            go.transform.SetParent(spriteRoot, worldPositionStays: false);
            var enemy = go.AddComponent<EnemyController>();
            enemy.Initialise(this, archetype, spawnPos);
        }

        private Vector2 GetRandomSpawnPosition()
        {
            int edge = rng.Next(4);
            float x;
            float y;
            switch (edge)
            {
                case 0:
                    x = playArea.xMin - 2f;
                    y = UnityEngine.Random.Range(playArea.yMin, playArea.yMax);
                    break;
                case 1:
                    x = playArea.xMax + 2f;
                    y = UnityEngine.Random.Range(playArea.yMin, playArea.yMax);
                    break;
                case 2:
                    y = playArea.yMin - 2f;
                    x = UnityEngine.Random.Range(playArea.xMin, playArea.xMax);
                    break;
                default:
                    y = playArea.yMax + 2f;
                    x = UnityEngine.Random.Range(playArea.xMin, playArea.xMax);
                    break;
            }

            return new Vector2(x, y);
        }

        private void SpawnXpOrb(Vector3 position, float xpAmount)
        {
            var go = new GameObject("XP Orb");
            go.transform.SetParent(spriteRoot, worldPositionStays: false);
            var orb = go.AddComponent<XpOrbController>();
            orb.Initialise(this, player, position, xpAmount);
        }

        private void BuildUpgradePool()
        {
            upgradePool.Clear();
            upgradePool.Add(new UpgradeDefinition
            {
                Id = "railgun",
                Icon = "➡️",
                Title = "Zırh Delici",
                Description = "Tek hedefe yüksek hasar veren delici atış ekler.",
                Requirement = () => !ownedWeapons.Contains("railgun"),
                Apply = () =>
                {
                    ownedWeapons.Add("railgun");
                    player.AddWeapon(PlayerWeapon.CreateRailgun());
                    ui.RefreshWeaponSlots(player.GetWeapons());
                }
            });

            upgradePool.Add(new UpgradeDefinition
            {
                Id = "shotgun",
                Icon = "🔥",
                Title = "Saçmalı Tüfek",
                Description = "Yakın mesafede geniş açıyla saçmalar fırlatır.",
                Requirement = () => !ownedWeapons.Contains("shotgun"),
                Apply = () =>
                {
                    ownedWeapons.Add("shotgun");
                    player.AddWeapon(PlayerWeapon.CreateShotgun());
                    ui.RefreshWeaponSlots(player.GetWeapons());
                }
            });

            upgradePool.Add(new UpgradeDefinition
            {
                Id = "laser",
                Icon = "〰",
                Title = "Lazer Işını",
                Description = "Hassas ve hızlı ateş eden enerji silahı.",
                Requirement = () => !ownedWeapons.Contains("laser"),
                Apply = () =>
                {
                    ownedWeapons.Add("laser");
                    player.AddWeapon(PlayerWeapon.CreateLaser());
                    ui.RefreshWeaponSlots(player.GetWeapons());
                }
            });

            upgradePool.Add(new UpgradeDefinition
            {
                Id = "damageBoost",
                Icon = "💥",
                Title = "Mühimmat Desteği",
                Description = "+20% tüm silah hasarı.",
                Requirement = () => true,
                Apply = () => player.ModifyDamageMultiplier(0.2f)
            });

            upgradePool.Add(new UpgradeDefinition
            {
                Id = "maxHealth",
                Icon = "🛡️",
                Title = "Ek Zırh",
                Description = "+30 maksimum zırh.",
                Requirement = () => player.Level < 10,
                Apply = () => player.ModifyMaxHealth(30f)
            });

            upgradePool.Add(new UpgradeDefinition
            {
                Id = "dashFuel",
                Icon = "💨",
                Title = "Yakıt Takviyesi",
                Description = "Atılma yakıtı daha hızlı dolar.",
                Requirement = () => true,
                Apply = () => player.BuffDashRecovery(0.25f)
            });

            upgradePool.Add(new UpgradeDefinition
            {
                Id = "shield",
                Icon = "🛡",
                Title = "Kalkan Gelişimi",
                Description = "Kalkan süresi artar.",
                Requirement = () => true,
                Apply = () => player.BuffShieldDuration(0.25f)
            });
        }

        private IReadOnlyList<UpgradeDefinition> GenerateUpgradeOptions()
        {
            var candidates = upgradePool
                .Where(u => u.Requirement == null || u.Requirement())
                .OrderBy(_ => rng.Next())
                .Take(3)
                .ToList();
            return candidates;
        }

        #endregion

        #region Nested data types

        public class UpgradeDefinition
        {
            public string Id;
            public string Icon;
            public string Title;
            public string Description;
            public Func<bool> Requirement;
            public Action Apply;
        }

        public struct EnemyArchetype
        {
            public string Name;
            public float Health;
            public float Speed;
            public float Damage;
            public float XpReward;
            public int ScoreReward;
            public Color BodyColor;

            public static EnemyArchetype Sample(GameDifficulty difficulty, int wave, System.Random rng)
            {
                int tier = Mathf.Clamp(1 + wave / 5, 1, 3);
                float modifier = difficulty == GameDifficulty.Easy ? 0.8f : 1f;
                if (tier == 1)
                {
                    return new EnemyArchetype
                    {
                        Name = "Piyade",
                        Health = 25f * modifier,
                        Speed = 2.6f,
                        Damage = 8f,
                        XpReward = 10f,
                        ScoreReward = 25,
                        BodyColor = new Color(0.7f, 0.1f, 0.1f)
                    };
                }

                if (tier == 2)
                {
                    return new EnemyArchetype
                    {
                        Name = "Zırhlı",
                        Health = 60f * modifier,
                        Speed = 2.2f,
                        Damage = 12f,
                        XpReward = 18f,
                        ScoreReward = 45,
                        BodyColor = new Color(0.65f, 0.35f, 0.1f)
                    };
                }

                return new EnemyArchetype
                {
                    Name = "Elit",
                    Health = 120f * modifier,
                    Speed = 1.8f,
                    Damage = 18f,
                    XpReward = 30f,
                    ScoreReward = 75,
                    BodyColor = new Color(0.35f, 0.25f, 0.6f)
                };
            }
        }

        #endregion

        #region UI Controller

        private class UIController
        {
            private readonly GameController game;
            private readonly Canvas canvas;
            private readonly Font font;

            private readonly Text timerText;
            private readonly Text scoreText;
            private readonly Text moneyText;
            private readonly Text flagText;
            private readonly Text healthText;
            private readonly Image healthFill;
            private readonly Image xpFill;
            private readonly Text levelText;
            private readonly RectTransform weaponSlotContainer;
            private readonly Image dashFillMask;
            private readonly Image shieldFillMask;

            private readonly GameObject mapHint;
            private readonly GameObject mapOverlay;

            private readonly GameObject mainMenu;
            private readonly GameObject pauseMenu;
            private readonly GameObject gameOverScreen;
            private readonly GameObject levelUpScreen;
            private readonly RectTransform upgradeOptionsContainer;
            private readonly Text gameOverTitle;
            private readonly Text gameOverStats;

            private readonly Color crtColor = new Color(0f, 1f, 0.6f);

            private UIController(GameController game)
            {
                this.game = game;
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                canvas = new GameObject("UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
                canvas.transform.SetParent(game.transform);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                var root = canvas.transform;

                CreateBackdrop(root);

                mainMenu = BuildMainMenu(root).gameObject;
                pauseMenu = BuildPauseMenu(root).gameObject;
                gameOverScreen = BuildGameOverScreen(root, out gameOverTitle, out gameOverStats).gameObject;
                levelUpScreen = BuildLevelUpScreen(root, out upgradeOptionsContainer).gameObject;

                var topBand = BuildTopBand(root, out weaponSlotContainer, out timerText, out scoreText, out moneyText, out flagText);
                var dashboard = BuildDashboard(root, out healthFill, out healthText, out xpFill, out levelText, out dashFillMask, out shieldFillMask);

                mapHint = BuildMapHint(root);
                mapOverlay = BuildMapOverlay(root);

                topBand.gameObject.SetActive(true);
                dashboard.gameObject.SetActive(true);
                HideAllScreens();
            }

            public static UIController Create(GameController game) => new UIController(game);

            private void CreateBackdrop(Transform root)
            {
                var vignette = new GameObject("Vignette", typeof(Image));
                var img = vignette.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.35f);
                var rect = vignette.GetComponent<RectTransform>();
                rect.SetParent(root, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            private RectTransform BuildTopBand(Transform root, out RectTransform weaponSlots, out Text timer, out Text score, out Text money, out Text flags)
            {
                var top = CreatePanel(root, "TopHeadBand", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(0f, -20f));
                var img = top.GetComponent<Image>();
                img.color = new Color(0f, 0.18f, 0.12f, 0.85f);

                var layout = top.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(30, 30, 15, 15);
                layout.spacing = 20f;
                layout.childAlignment = TextAnchor.MiddleCenter;

                weaponSlots = CreatePanel(top, "WeaponSlots", Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                var weaponLayout = weaponSlots.gameObject.AddComponent<HorizontalLayoutGroup>();
                weaponLayout.spacing = 10f;

                var rightGroup = CreatePanel(top, "TopRight", Vector2.zero, Vector2.one, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
                var rightLayout = rightGroup.gameObject.AddComponent<HorizontalLayoutGroup>();
                rightLayout.spacing = 10f;
                rightLayout.childAlignment = TextAnchor.MiddleRight;

                timer = CreateInfoDisplay(rightGroup, "00:00");
                score = CreateInfoDisplay(rightGroup, "SKOR: 0");
                money = CreateInfoDisplay(rightGroup, "TL: 0");
                flags = CreateInfoDisplay(rightGroup, "🚩 0/4");

                var mapButton = CreateButton(rightGroup, "🗺️", ToggleMapOverlay);
                mapButton.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 70f);
                var pauseButton = CreateButton(rightGroup, "⏸️", game.TogglePause);
                pauseButton.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 70f);

                return top;
            }

            private RectTransform BuildDashboard(Transform root, out Image health, out Text healthLabel, out Image xp, out Text level, out Image dashMask, out Image shieldMask)
            {
                var dash = CreatePanel(root, "Dashboard", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(0f, 20f));
                var img = dash.GetComponent<Image>();
                img.color = new Color(0.02f, 0.12f, 0.06f, 0.95f);

                var layout = dash.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(30, 30, 15, 15);
                layout.spacing = 30f;
                layout.childAlignment = TextAnchor.MiddleCenter;

                var left = CreatePanel(dash, "LeftPanel", Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
                var center = CreatePanel(dash, "CenterPanel", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var right = CreatePanel(dash, "RightPanel", Vector2.zero, Vector2.one, new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);

                var healthContainer = CreateGauge(left, "ZIRH", out health, out healthLabel);
                var xpContainer = CreateGauge(center, "SEVİYE 1", out xp, out level);

                dashMask = CreateSkillMeter(right, "ATILMA", "SHIFT");
                shieldMask = CreateSkillMeter(right, "KALKAN", "SPACE");

                return dash;
            }

            private RectTransform BuildMainMenu(Transform root)
            {
                var menu = CreateFullScreenPanel(root, "MainMenu");
                var layout = menu.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 20f;

                var title = CreateLabel(menu, "PENÇE\nHAREKATI", 72, FontStyle.Bold);
                title.alignment = TextAnchor.MiddleCenter;

                var subtitle = CreateLabel(menu, "Vatan sana emanet.", 28, FontStyle.Italic);
                subtitle.alignment = TextAnchor.MiddleCenter;

                var normal = CreateButton(menu, "NORMAL MOD", () =>
                {
                    HideAllScreens();
                    game.StartGame(GameDifficulty.Normal);
                });
                normal.GetComponentInChildren<Text>().fontSize = 32;

                var easy = CreateButton(menu, "KOLAY MOD", () =>
                {
                    HideAllScreens();
                    game.StartGame(GameDifficulty.Easy);
                });
                easy.GetComponentInChildren<Text>().fontSize = 24;

                var controls = CreateButton(menu, "KONTROLLER", () => ShowControlsModal());
                controls.GetComponentInChildren<Text>().fontSize = 20;

                return menu;
            }

            private RectTransform BuildPauseMenu(Transform root)
            {
                var panel = CreateFullScreenPanel(root, "PauseMenu");
                var card = CreatePanel(panel, "PauseCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 0f), new Vector2(400f, 0f));
                card.sizeDelta = new Vector2(600f, 400f);
                card.GetComponent<Image>().color = new Color(0f, 0.1f, 0.06f, 0.95f);
                var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 20f;
                CreateLabel(card, "OYUN DURDURULDU", 36, FontStyle.Bold);
                CreateButton(card, "DEVAM", () => game.ResumeGameplay());
                CreateButton(card, "YENİDEN BAŞLAT", () =>
                {
                    HidePauseMenu();
                    game.RestartGame();
                });
                CreateButton(card, "ANA MENÜ", game.ReturnToMainMenu);
                panel.gameObject.SetActive(false);
                return panel;
            }

            private RectTransform BuildGameOverScreen(Transform root, out Text title, out Text stats)
            {
                var panel = CreateFullScreenPanel(root, "GameOverScreen");
                var card = CreatePanel(panel, "GameOverCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(450f, 0f), new Vector2(450f, 0f));
                card.sizeDelta = new Vector2(700f, 500f);
                card.GetComponent<Image>().color = new Color(0f, 0.1f, 0.06f, 0.95f);
                var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 20f;
                title = CreateLabel(card, "OYUN BİTTİ", 48, FontStyle.Bold);
                stats = CreateLabel(card, "", 20, FontStyle.Normal);
                stats.alignment = TextAnchor.UpperCenter;
                CreateButton(card, "YENİDEN BAŞLAT", () =>
                {
                    HideGameOver();
                    game.RestartGame();
                });
                CreateButton(card, "ANA MENÜ", () =>
                {
                    HideGameOver();
                    game.ReturnToMainMenu();
                });
                panel.gameObject.SetActive(false);
                return panel;
            }

            private RectTransform BuildLevelUpScreen(Transform root, out RectTransform options)
            {
                var panel = CreateFullScreenPanel(root, "LevelUpScreen");
                var card = CreatePanel(panel, "LevelUpCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500f, 0f), new Vector2(500f, 0f));
                card.sizeDelta = new Vector2(900f, 520f);
                card.GetComponent<Image>().color = new Color(0f, 0.12f, 0.07f, 0.95f);
                var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.spacing = 15f;
                CreateLabel(card, "SEVİYE ATLADIN!", 40, FontStyle.Bold).alignment = TextAnchor.MiddleCenter;
                CreateLabel(card, "Bir yükseltme seç:", 24, FontStyle.Normal).alignment = TextAnchor.MiddleCenter;
                options = CreatePanel(card, "UpgradeOptions", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var optionsLayout = options.gameObject.AddComponent<VerticalLayoutGroup>();
                optionsLayout.spacing = 10f;
                optionsLayout.childAlignment = TextAnchor.MiddleCenter;
                CreateButton(card, "VAZGEÇ", game.SkipUpgrade);
                panel.gameObject.SetActive(false);
                return panel;
            }

            private GameObject BuildMapHint(Transform root)
            {
                var hint = CreatePanel(root, "MapHint", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(25f, -160f), new Vector2(300f, -120f));
                hint.GetComponent<Image>().color = new Color(0f, 0.15f, 0.08f, 0.8f);
                var text = CreateLabel(hint, "Harita için [M] tuşuna bas", 20, FontStyle.Normal);
                text.alignment = TextAnchor.MiddleLeft;
                hint.gameObject.SetActive(false);
                return hint.gameObject;
            }

            private GameObject BuildMapOverlay(Transform root)
            {
                var panel = CreateFullScreenPanel(root, "MapOverlay");
                panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
                var label = CreateLabel(panel, "Harita modu prototip aşamasında.", 32, FontStyle.Bold);
                label.alignment = TextAnchor.MiddleCenter;
                panel.gameObject.SetActive(false);
                return panel.gameObject;
            }

            private void ShowControlsModal()
            {
                var modal = CreateFullScreenPanel(canvas.transform, "ControlsModal");
                var card = CreatePanel(modal, "ControlsCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(400f, 0f), new Vector2(400f, 0f));
                card.sizeDelta = new Vector2(700f, 400f);
                card.GetComponent<Image>().color = new Color(0f, 0.1f, 0.06f, 0.95f);
                var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 12f;
                CreateLabel(card, "Kontroller", 32, FontStyle.Bold);
                CreateLabel(card, "WASD - Hareket\nShift - Atılma\nSpace - Kalkan\nM - Harita\nEsc - Duraklat", 20, FontStyle.Normal).alignment = TextAnchor.MiddleCenter;
                CreateButton(card, "TAMAM", () => Destroy(modal.gameObject));
            }

            private RectTransform CreateFullScreenPanel(Transform root, string name)
            {
                var panel = CreatePanel(root, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
                return panel;
            }

            private RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax)
            {
                var go = new GameObject(name, typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;
                go.GetComponent<Image>().color = new Color(0f, 0.1f, 0.05f, 0.75f);
                return rect;
            }

            private Text CreateLabel(Transform parent, string text, int size, FontStyle style)
            {
                var go = new GameObject("Label", typeof(Text));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(600f, 60f);
                var label = go.GetComponent<Text>();
                label.font = font;
                label.fontSize = size;
                label.fontStyle = style;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = crtColor;
                label.text = text;
                return label;
            }

            private Text CreateInfoDisplay(Transform parent, string text)
            {
                var panel = CreatePanel(parent, "Info", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                panel.GetComponent<Image>().color = new Color(0.02f, 0.08f, 0.04f, 0.85f);
                var label = CreateLabel(panel, text, 22, FontStyle.Bold);
                label.color = crtColor;
                return label;
            }

            private Button CreateButton(Transform parent, string text, Action action)
            {
                var go = new GameObject("Button", typeof(Image), typeof(Button));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(260f, 70f);
                var image = go.GetComponent<Image>();
                image.color = new Color(0f, 0.25f, 0.12f, 0.9f);
                var button = go.GetComponent<Button>();
                button.onClick.AddListener(() => action?.Invoke());

                var label = CreateLabel(go.transform, text, 24, FontStyle.Bold);
                label.alignment = TextAnchor.MiddleCenter;
                label.color = crtColor;

                return button;
            }

            private RectTransform CreateGauge(Transform parent, string title, out Image fill, out Text text)
            {
                var container = CreatePanel(parent, title + "Container", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                container.sizeDelta = new Vector2(400f, 80f);
                var background = new GameObject("GaugeBackground", typeof(Image));
                var bgRect = background.GetComponent<RectTransform>();
                bgRect.SetParent(container, false);
                bgRect.anchorMin = new Vector2(0f, 0.2f);
                bgRect.anchorMax = new Vector2(1f, 0.8f);
                bgRect.offsetMin = new Vector2(10f, 0f);
                bgRect.offsetMax = new Vector2(-10f, 0f);
                var bgImage = background.GetComponent<Image>();
                bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);

                var fillGo = new GameObject("Fill", typeof(Image));
                fill = fillGo.GetComponent<Image>();
                var fillRect = fill.GetComponent<RectTransform>();
                fillRect.SetParent(bgRect, false);
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.color = crtColor;
                fill.fillAmount = 1f;

                text = CreateLabel(container, title, 22, FontStyle.Bold);
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                return container;
            }

            private Image CreateSkillMeter(Transform parent, string label, string key)
            {
                var container = CreatePanel(parent, label + "Container", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                container.sizeDelta = new Vector2(200f, 120f);
                CreateLabel(container, label, 22, FontStyle.Bold);
                var barBg = new GameObject("SkillBar", typeof(Image));
                var bgRect = barBg.GetComponent<RectTransform>();
                bgRect.SetParent(container, false);
                bgRect.anchorMin = new Vector2(0.2f, 0.4f);
                bgRect.anchorMax = new Vector2(0.8f, 0.6f);
                var bgImage = barBg.GetComponent<Image>();
                bgImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);

                var fillGo = new GameObject("Fill", typeof(Image));
                var fill = fillGo.GetComponent<Image>();
                var fillRect = fill.GetComponent<RectTransform>();
                fillRect.SetParent(bgRect, false);
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(0f, 1f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                fill.fillAmount = 1f;
                fill.color = crtColor;

                CreateLabel(container, key, 20, FontStyle.Italic);
                return fill;
            }

            public void UpdateHud(float time, int score, int money, int flags)
            {
                TimeSpan ts = TimeSpan.FromSeconds(time);
                timerText.text = ts.ToString("mm':'ss");
                scoreText.text = $"SKOR: {score}";
                moneyText.text = $"TL: {money}";
                flagText.text = $"🚩 {flags}/4";
            }

            public void UpdateHealth(float current, float max)
            {
                healthFill.fillAmount = Mathf.Approximately(max, 0f) ? 0f : Mathf.Clamp01(current / max);
                healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }

            public void UpdateXp(float current, float required, int level)
            {
                xpFill.fillAmount = Mathf.Approximately(required, 0f) ? 0f : Mathf.Clamp01(current / required);
                levelText.text = $"SEVİYE {level}";
            }

            public void UpdateDashFuel(float ratio)
            {
                dashFillMask.fillAmount = Mathf.Clamp01(ratio);
            }

            public void UpdateShieldCharge(float ratio)
            {
                shieldFillMask.fillAmount = Mathf.Clamp01(ratio);
            }

            public void RefreshWeaponSlots(IReadOnlyList<PlayerWeapon> weapons)
            {
                foreach (Transform child in weaponSlotContainer)
                {
                    GameObject.Destroy(child.gameObject);
                }

                foreach (var weapon in weapons)
                {
                    var slot = CreatePanel(weaponSlotContainer, weapon.DisplayName, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    slot.sizeDelta = new Vector2(120f, 80f);
                    slot.GetComponent<Image>().color = new Color(0.05f, 0.15f, 0.08f, 0.9f);
                    CreateLabel(slot, weapon.Icon, 32, FontStyle.Normal).alignment = TextAnchor.MiddleCenter;
                    CreateLabel(slot, weapon.DisplayName, 18, FontStyle.Bold).alignment = TextAnchor.MiddleCenter;
                    CreateLabel(slot, $"Seviye {weapon.Level}", 16, FontStyle.Normal).alignment = TextAnchor.MiddleCenter;
                }
            }

            public void ShowMainMenu()
            {
                HideAllScreens();
                mainMenu.SetActive(true);
            }

            public void HideAllScreens()
            {
                mainMenu.SetActive(false);
                pauseMenu.SetActive(false);
                gameOverScreen.SetActive(false);
                levelUpScreen.SetActive(false);
                mapOverlay.SetActive(false);
                mapHint.SetActive(false);
            }

            public void ShowPauseMenu()
            {
                pauseMenu.SetActive(true);
            }

            public void HidePauseMenu()
            {
                pauseMenu.SetActive(false);
            }

            public void ShowGameOver(bool didWin, int score, float time, Dictionary<string, float> damage)
            {
                HideAllScreens();
                gameOverScreen.SetActive(true);
                gameOverTitle.text = didWin ? "ZAFER" : "OYUN BİTTİ";
                TimeSpan ts = TimeSpan.FromSeconds(time);
                var lines = new List<string>
                {
                    $"Süre: {ts:mm':'ss}",
                    $"Skor: {score}"
                };
                if (damage.Count > 0)
                {
                    lines.Add("Silah Hasarı:");
                    foreach (var kvp in damage.OrderByDescending(k => k.Value))
                    {
                        lines.Add($"- {kvp.Key}: {Mathf.RoundToInt(kvp.Value)}");
                    }
                }
                gameOverStats.text = string.Join("\n", lines);
            }

            public void HideGameOver()
            {
                gameOverScreen.SetActive(false);
            }

            public void ShowLevelUp(IReadOnlyList<GameController.UpgradeDefinition> upgrades)
            {
                foreach (Transform child in upgradeOptionsContainer)
                {
                    GameObject.Destroy(child.gameObject);
                }

                foreach (var upgrade in upgrades)
                {
                    var button = CreateButton(upgradeOptionsContainer, $"{upgrade.Icon} {upgrade.Title}\n{upgrade.Description}", () =>
                    {
                        game.ApplyUpgrade(upgrade);
                    });
                    var label = button.GetComponentInChildren<Text>();
                    label.alignment = TextAnchor.MiddleCenter;
                    label.resizeTextForBestFit = true;
                }

                levelUpScreen.SetActive(true);
            }

            public void HideLevelUp()
            {
                levelUpScreen.SetActive(false);
            }

            public void ToggleMapOverlay()
            {
                bool active = !mapOverlay.activeSelf;
                mapOverlay.SetActive(active);
                mapHint.SetActive(!active);
            }
        }

        #endregion
    }
}
