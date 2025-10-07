using System.Collections.Generic;
using UnityEngine;

namespace PnceHarekat
{
    /// <summary>
    /// Handles player movement, health, leveling and weapon fire behaviour.
    /// All numbers are roughly based on the HTML reference experience but
    /// tuned to feel responsive in Unity.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float dashDuration = 0.25f;
        [SerializeField] private float dashCost = 0.35f;
        [SerializeField] private float dashRecoveryRate = 0.35f;

        [Header("Shield")]
        [SerializeField] private float shieldDuration = 2.5f;
        [SerializeField] private float shieldRecoveryRate = 0.25f;

        [Header("Vitals")]
        [SerializeField] private float maxHealth = 120f;

        private GameController game;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer turretRenderer;
        private Transform turretTransform;

        private readonly List<PlayerWeapon> weapons = new();

        private float health;
        private float xp;
        private int level = 1;
        private float dashFuel = 1f;
        private float dashTimer;
        private bool dashing;

        private float shieldCharge = 1f;
        private float shieldTimer;
        private bool shieldActive;

        private bool inputEnabled;
        private float damageMultiplier = 1f;

        private readonly Dictionary<string, float> cooldowns = new();

        private const float mapHintDelay = 10f;
        private float hintTimer;

        public int Level => level;

        public void Initialise(GameController owner)
        {
            game = owner;
            if (bodyRenderer == null)
            {
                bodyRenderer = gameObject.AddComponent<SpriteRenderer>();
                bodyRenderer.sprite = Resources.GetBuiltinResource<Sprite>("Sprites/Square.psd");
                bodyRenderer.color = new Color(0.2f, 0.7f, 0.4f);
                bodyRenderer.sortingOrder = 1;

                transform.localScale = new Vector3(1f, 1.4f, 1f);

                var turret = new GameObject("Turret");
                turretTransform = turret.transform;
                turretTransform.SetParent(transform, false);
                turretTransform.localPosition = new Vector3(0f, 0.4f, 0f);
                turretTransform.localScale = new Vector3(0.35f, 0.9f, 1f);
                turretRenderer = turret.AddComponent<SpriteRenderer>();
                turretRenderer.sprite = Resources.GetBuiltinResource<Sprite>("Sprites/Square.psd");
                turretRenderer.color = new Color(0.9f, 0.9f, 0.2f);
                turretRenderer.sortingOrder = 2;
            }

            weapons.Clear();
            cooldowns.Clear();
            AddWeapon(PlayerWeapon.CreateDefault());

            maxHealth = Mathf.Max(120f, maxHealth);
            health = maxHealth;
            xp = 0f;
            level = 1;
            dashFuel = 1f;
            dashTimer = 0f;
            shieldCharge = 1f;
            shieldTimer = 0f;
            shieldActive = false;
            damageMultiplier = 1f;
            hintTimer = mapHintDelay;

            game.UpdatePlayerHealth(health, maxHealth);
            game.UpdatePlayerLevel(xp, GetXpRequirement(level), level);
            game.UpdateDashFuel(dashFuel);
            game.UpdateShieldCharge(shieldCharge);

            inputEnabled = true;
        }

        private void Update()
        {
            if (game == null || game.State != GameState.Playing)
            {
                return;
            }

            if (!inputEnabled)
            {
                RegenerateResources(Time.deltaTime);
                return;
            }

            float dt = Time.deltaTime;
            HandleMovement(dt);
            HandleAbilities(dt);
            HandleWeapons(dt);
            RegenerateResources(dt);
        }

        private void HandleMovement(float dt)
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            float currentSpeed = moveSpeed;
            if (dashing)
            {
                currentSpeed *= 2.5f;
            }

            Vector3 displacement = (Vector3)(input * currentSpeed * dt);
            transform.position += displacement;

            ClampInsidePlayArea();
        }

        private void HandleAbilities(float dt)
        {
            if (!dashing && Input.GetKeyDown(KeyCode.LeftShift))
            {
                TryDash();
            }

            if (dashing)
            {
                dashTimer -= dt;
                if (dashTimer <= 0f)
                {
                    dashing = false;
                }
            }

            if (!shieldActive && Input.GetKeyDown(KeyCode.Space))
            {
                TryActivateShield();
            }

            if (shieldActive)
            {
                shieldTimer -= dt;
                if (shieldTimer <= 0f)
                {
                    shieldActive = false;
                    shieldCharge = 0f;
                }
            }
        }

        private void HandleWeapons(float dt)
        {
            if (weapons.Count == 0)
            {
                return;
            }

            EnemyController target = AcquireTarget();
            if (target != null)
            {
                Vector2 dir = (target.transform.position - transform.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                turretTransform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            foreach (var weapon in weapons)
            {
                if (!cooldowns.TryGetValue(weapon.Id, out float timer))
                {
                    timer = 0f;
                }

                timer -= dt;
                if (timer <= 0f && target != null)
                {
                    if (Vector2.Distance(transform.position, target.transform.position) <= weapon.Range)
                    {
                        FireWeapon(weapon, target);
                        timer = 1f / weapon.FireRate;
                    }
                }

                cooldowns[weapon.Id] = timer;
            }
        }

        private void RegenerateResources(float dt)
        {
            if (!dashing)
            {
                dashFuel = Mathf.Clamp01(dashFuel + dashRecoveryRate * dt);
            }
            game.UpdateDashFuel(dashFuel);

            if (!shieldActive)
            {
                shieldCharge = Mathf.Clamp01(shieldCharge + shieldRecoveryRate * dt);
            }
            game.UpdateShieldCharge(shieldCharge);

            hintTimer -= dt;
            if (hintTimer < 0f && game != null)
            {
                hintTimer = mapHintDelay;
            }
        }

        private void ClampInsidePlayArea()
        {
            if (game == null)
            {
                return;
            }

            Rect area = game.PlayArea;
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, area.xMin + 0.5f, area.xMax - 0.5f);
            pos.y = Mathf.Clamp(pos.y, area.yMin + 0.5f, area.yMax - 0.5f);
            transform.position = pos;
        }

        private EnemyController AcquireTarget()
        {
            if (game == null)
            {
                return null;
            }

            EnemyController closest = null;
            float bestSqr = float.PositiveInfinity;
            foreach (var enemy in game.ActiveEnemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    closest = enemy;
                }
            }

            return closest;
        }

        private void FireWeapon(PlayerWeapon weapon, EnemyController target)
        {
            Vector2 baseDirection = (target.transform.position - transform.position).normalized;
            for (int i = 0; i < weapon.ProjectilesPerShot; i++)
            {
                float spread = weapon.Spread * (weapon.ProjectilesPerShot > 1 ? ((float)i / (weapon.ProjectilesPerShot - 1) - 0.5f) : 0f);
                Vector2 dir = Quaternion.Euler(0f, 0f, spread) * baseDirection;
                var projectileGo = new GameObject($"Projectile_{weapon.Id}");
                var projectile = projectileGo.AddComponent<ProjectileController>();
                projectile.Initialise(game, weapon, transform.position + (Vector3)(dir * 0.6f), dir, weapon.ProjectileSpeed, weapon.GetDamage(damageMultiplier));
            }
        }

        public void TakeDamage(float amount)
        {
            if (shieldActive)
            {
                return;
            }

            health -= amount;
            if (health <= 0f)
            {
                health = 0f;
                game.UpdatePlayerHealth(health, maxHealth);
                inputEnabled = false;
                game.NotifyPlayerDeath();
                return;
            }

            game.UpdatePlayerHealth(health, maxHealth);
        }

        public void CollectXp(float amount)
        {
            xp += amount;
            float required = GetXpRequirement(level);
            while (xp >= required)
            {
                xp -= required;
                level++;
                required = GetXpRequirement(level);
                game.UpdatePlayerLevel(xp, required, level);
                game.OnPlayerLevelUp();
                return;
            }

            game.UpdatePlayerLevel(xp, required, level);
        }

        private float GetXpRequirement(int lvl)
        {
            return 70f + (lvl - 1) * 30f;
        }

        private void TryDash()
        {
            if (dashFuel < dashCost)
            {
                return;
            }

            dashFuel = Mathf.Clamp01(dashFuel - dashCost);
            dashTimer = dashDuration;
            dashing = true;
            game.UpdateDashFuel(dashFuel);
        }

        private void TryActivateShield()
        {
            if (shieldCharge < 1f)
            {
                return;
            }

            shieldCharge = 0f;
            shieldTimer = shieldDuration;
            shieldActive = true;
            game.UpdateShieldCharge(shieldCharge);
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

        public void AddWeapon(PlayerWeapon weapon)
        {
            var existing = weapons.Find(w => w.Id == weapon.Id);
            if (existing != null)
            {
                existing.LevelUp();
            }
            else
            {
                weapons.Add(weapon);
            }

            cooldowns[weapon.Id] = 0f;
            game?.RegisterWeaponName(weapon.Id, weapon.DisplayName);
        }

        public IReadOnlyList<PlayerWeapon> GetWeapons() => weapons;

        public void ModifyDamageMultiplier(float additive)
        {
            damageMultiplier *= 1f + additive;
        }

        public void ModifyMaxHealth(float amount)
        {
            maxHealth += amount;
            health += amount;
            game.UpdatePlayerHealth(health, maxHealth);
        }

        public void BuffDashRecovery(float bonus)
        {
            dashRecoveryRate *= 1f + bonus;
        }

        public void BuffShieldDuration(float bonus)
        {
            shieldDuration *= 1f + bonus;
        }
    }
}
