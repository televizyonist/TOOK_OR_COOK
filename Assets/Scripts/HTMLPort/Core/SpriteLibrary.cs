using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PnceHarekat
{
    public static class SpriteLibrary
    {
        private const float defaultPixelsPerUnit = 64f;

        private static readonly Dictionary<string, Sprite> spriteCache = new();
        private static readonly Dictionary<string, Texture2D> textureCache = new();

        private static readonly Dictionary<string, string> spriteToPath = new()
        {
            ["Sprites/background_tile"] = "Art/Sprites/background_tile.png",
            ["Sprites/player_body"] = "Art/Sprites/player_body.png",
            ["Sprites/player_turret"] = "Art/Sprites/player_turret.png",
            ["Sprites/enemy_basic"] = "Art/Sprites/enemy_basic.png",
            ["Sprites/projectile_bolt"] = "Art/Sprites/projectile_bolt.png",
            ["Sprites/xp_orb"] = "Art/Sprites/xp_orb.png",
        };

        public static Sprite LoadSprite(string relativePath, Vector2? pivot = null, float pixelsPerUnit = defaultPixelsPerUnit)
        {
            Vector2 pivotValue = pivot ?? new Vector2(0.5f, 0.5f);
            string cacheKey = $"{relativePath}|{pivotValue.x:F3},{pivotValue.y:F3}|{pixelsPerUnit:F2}";

            if (spriteCache.TryGetValue(cacheKey, out var sprite))
            {
                return sprite;
            }

            var texture = GetTexture(relativePath);
            if (texture == null)
            {
                Debug.LogWarning($"SpriteLibrary could not load sprite key '{relativePath}'.");
                return null;
            }

            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivotValue, pixelsPerUnit);
            sprite.name = texture.name;
            spriteCache[cacheKey] = sprite;
            return sprite;
        }

        private static Texture2D GetTexture(string key)
        {
            if (textureCache.TryGetValue(key, out var texture))
            {
                return texture;
            }

            if (!spriteToPath.TryGetValue(key, out var relativePath))
            {
                return null;
            }

            texture = LoadTexture(relativePath);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                textureCache[key] = texture;
            }

            return texture;
        }

        private static Texture2D LoadTexture(string relativePath)
        {
            foreach (var basePath in EnumerateSearchRoots())
            {
                if (string.IsNullOrEmpty(basePath))
                {
                    continue;
                }

                string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
                string fullPath = Path.Combine(basePath, normalized);
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                try
                {
                    byte[] data = File.ReadAllBytes(fullPath);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                    {
                        name = Path.GetFileNameWithoutExtension(relativePath)
                    };

                    if (texture.LoadImage(data))
                    {
                        return texture;
                    }

                    Debug.LogWarning($"SpriteLibrary failed to decode texture at '{fullPath}'.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"SpriteLibrary could not load texture '{fullPath}': {ex.Message}");
                }
            }

            return null;
        }

        private static IEnumerable<string> EnumerateSearchRoots()
        {
            yield return Application.streamingAssetsPath;
            yield return Application.dataPath;
        }
    }
}
