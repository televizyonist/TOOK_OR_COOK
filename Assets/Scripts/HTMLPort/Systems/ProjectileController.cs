using System.Collections.Generic;
using UnityEngine;

namespace PnceHarekat
{
    /// <summary>
    /// Moves a projectile forward and applies damage to enemies using simple
    /// distance based collision checks.
    /// </summary>
    public class ProjectileController : MonoBehaviour
    {
        private GameController game;
        private PlayerWeapon weapon;
        private Vector2 direction;
        private float speed;
        private float damage;
        private float lifeTime = 2.5f;
        private readonly HashSet<EnemyController> hitEnemies = new();

        public void Initialise(GameController owner, PlayerWeapon sourceWeapon, Vector2 position, Vector2 dir, float projectileSpeed, float damageAmount)
        {
            game = owner;
            weapon = sourceWeapon;
            direction = dir.normalized;
            speed = projectileSpeed;
            damage = damageAmount;
            transform.position = position;

            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteLibrary.LoadSprite("Sprites/projectile_bolt");
            renderer.color = Color.white;
            renderer.sortingOrder = 3;
            transform.localScale = Vector3.one;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            game.RegisterProjectile(this);
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.UnregisterProjectile(this);
            }
        }

        private void Update()
        {
            if (game == null || game.State != GameState.Playing)
            {
                return;
            }

            float dt = Time.deltaTime;
            transform.position += (Vector3)(direction * speed * dt);
            lifeTime -= dt;
            if (lifeTime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            foreach (var enemy in game.ActiveEnemies)
            {
                if (enemy == null || hitEnemies.Contains(enemy))
                {
                    continue;
                }

                float sqr = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqr <= 0.4f * 0.4f)
                {
                    enemy.TakeDamage(damage, weapon.Id);
                    if (weapon.Piercing)
                    {
                        hitEnemies.Add(enemy);
                    }
                    else
                    {
                        Destroy(gameObject);
                        return;
                    }
                }
            }
        }
    }
}
