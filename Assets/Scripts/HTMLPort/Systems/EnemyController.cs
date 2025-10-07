using UnityEngine;

namespace PnceHarekat
{
    /// <summary>
    /// Basic chase-and-bite enemy used for the prototype replication. Enemies
    /// move towards the player, deal contact damage and notify the game
    /// controller when they die.
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        private GameController game;
        private PlayerController player;
        private SpriteRenderer renderer;

        private float health;
        private float speed;
        private float damage;
        private float xpReward;
        private int scoreReward;
        private float attackCooldown = 0.8f;
        private float attackTimer;

        public void Initialise(GameController controller, GameController.EnemyArchetype archetype, Vector2 spawnPosition)
        {
            game = controller;
            player = FindObjectOfType<PlayerController>();
            transform.position = spawnPosition;

            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = SpriteLibrary.LoadSprite("Sprites/enemy_basic");
                renderer.sortingOrder = 0;
                transform.localScale = Vector3.one;
            }

            renderer.color = archetype.BodyColor;
            health = archetype.Health;
            speed = archetype.Speed;
            damage = archetype.Damage;
            xpReward = archetype.XpReward;
            scoreReward = archetype.ScoreReward;
            attackCooldown = Mathf.Max(0.6f, 1.1f - 0.02f * scoreReward);
            attackTimer = 0f;

            game.RegisterEnemy(this);
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.UnregisterEnemy(this);
            }
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindObjectOfType<PlayerController>();
            }

            if (game == null || game.State != GameState.Playing || player == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            attackTimer -= dt;
            Vector3 targetPosition = player.transform.position;
            Vector3 dir = (targetPosition - transform.position).normalized;
            transform.position += dir * speed * dt;

            if ((targetPosition - transform.position).sqrMagnitude <= 0.6f * 0.6f)
            {
                TryAttack();
            }
        }

        private void TryAttack()
        {
            if (attackTimer > 0f)
            {
                return;
            }

            attackTimer = attackCooldown;
            player.TakeDamage(damage);
        }

        public void TakeDamage(float amount, string weaponId)
        {
            health -= amount;
            if (health <= 0f)
            {
                health = 0f;
                Die();
                game.ReportDamage(weaponId, amount);
            }
            else
            {
                game.ReportDamage(weaponId, amount);
            }
        }

        private void Die()
        {
            if (game != null)
            {
                game.OnEnemyKilled(this, scoreReward, xpReward);
            }

            Destroy(gameObject);
        }
    }
}
