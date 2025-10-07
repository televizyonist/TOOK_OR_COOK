using UnityEngine;

namespace PnceHarekat
{
    /// <summary>
    /// Simple weapon data container used by the player controller. Weapons can
    /// level up and expose convenience constructors for the main archetypes of
    /// the HTML reference.
    /// </summary>
    public class PlayerWeapon
    {
        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public string Icon { get; private set; }
        public float Damage { get; private set; }
        public float FireRate { get; private set; }
        public float Range { get; private set; }
        public float ProjectileSpeed { get; private set; }
        public int ProjectilesPerShot { get; private set; }
        public float Spread { get; private set; }
        public bool Piercing { get; private set; }
        public int Level { get; private set; } = 1;

        private PlayerWeapon() { }

        public float GetDamage(float multiplier) => Damage * multiplier;

        public void LevelUp()
        {
            Level++;
            Damage *= 1.25f;
            FireRate *= 1.1f;
            Range *= 1.05f;
        }

        public static PlayerWeapon CreateDefault()
        {
            return new PlayerWeapon
            {
                Id = "defaultGun",
                DisplayName = "Ana Silah",
                Icon = "🔫",
                Damage = 14f,
                FireRate = 2.8f,
                Range = 6f,
                ProjectileSpeed = 12f,
                ProjectilesPerShot = 1,
                Spread = 0f,
                Piercing = false
            };
        }

        public static PlayerWeapon CreateRailgun()
        {
            return new PlayerWeapon
            {
                Id = "railgun",
                DisplayName = "Zırh Delici",
                Icon = "➡️",
                Damage = 42f,
                FireRate = 1.2f,
                Range = 9f,
                ProjectileSpeed = 18f,
                ProjectilesPerShot = 1,
                Spread = 0f,
                Piercing = true
            };
        }

        public static PlayerWeapon CreateShotgun()
        {
            return new PlayerWeapon
            {
                Id = "shotgun",
                DisplayName = "Saçmalı",
                Icon = "🔥",
                Damage = 8f,
                FireRate = 1.8f,
                Range = 4.5f,
                ProjectileSpeed = 11f,
                ProjectilesPerShot = 5,
                Spread = 25f,
                Piercing = false
            };
        }

        public static PlayerWeapon CreateLaser()
        {
            return new PlayerWeapon
            {
                Id = "laser",
                DisplayName = "Lazer",
                Icon = "〰",
                Damage = 6f,
                FireRate = 6f,
                Range = 8f,
                ProjectileSpeed = 20f,
                ProjectilesPerShot = 1,
                Spread = 0f,
                Piercing = true
            };
        }
    }
}
