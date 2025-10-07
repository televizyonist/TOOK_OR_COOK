using System;
using System.Collections.Generic;
using UnityEngine;

namespace PnceHarekat
{
    /// <summary>
    /// Provides procedural sprite generation so the Unity project does not rely
    /// on binary PNG assets (the original HTML prototype drew everything with
    /// canvas commands). Each sprite is generated once and cached for the
    /// session.
    /// </summary>
    public static class SpriteLibrary
    {
        private const float defaultPixelsPerUnit = 64f;

        private static readonly Dictionary<string, Sprite> spriteCache = new();
        private static readonly Dictionary<string, Texture2D> textureCache = new();

        private static readonly Dictionary<string, Func<Texture2D>> generators = new()
        {
            ["Sprites/background_tile"] = CreateBackgroundTexture,
            ["Sprites/player_body"] = CreatePlayerBodyTexture,
            ["Sprites/player_turret"] = CreatePlayerTurretTexture,
            ["Sprites/enemy_basic"] = CreateEnemyTexture,
            ["Sprites/projectile_bolt"] = CreateProjectileTexture,
            ["Sprites/xp_orb"] = CreateXpOrbTexture,
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
                Debug.LogWarning($"SpriteLibrary has no generator for sprite key '{relativePath}'.");
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

            if (!generators.TryGetValue(key, out var factory))
            {
                return null;
            }

            texture = factory.Invoke();
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                textureCache[key] = texture;
            }

            return texture;
        }

        private static Texture2D CreateBackgroundTexture()
        {
            const int size = 128;
            var texture = CreateTexture(size, size, new Color32(6, 36, 32, 255), "background_tile");

            DrawCheckerboard(texture, 16, new Color32(10, 50, 44, 255));
            DrawBorder(texture, 2, new Color32(0, 0, 0, 160));
            texture.Apply();
            return texture;
        }

        private static Texture2D CreatePlayerBodyTexture()
        {
            const int width = 64;
            const int height = 64;
            var texture = CreateTexture(width, height, new Color32(28, 40, 64, 255), "player_body");

            FillRoundedRect(texture, new RectInt(6, 6, width - 12, height - 12), 10, new Color32(42, 66, 104, 255));
            FillRoundedRect(texture, new RectInt(18, 24, 28, 20), 6, new Color32(94, 160, 208, 255));
            DrawHighlight(texture, new RectInt(10, height - 18, width - 20, 6));
            texture.Apply();
            return texture;
        }

        private static Texture2D CreatePlayerTurretTexture()
        {
            const int width = 32;
            const int height = 48;
            var texture = CreateTexture(width, height, new Color32(0, 0, 0, 0), "player_turret");

            FillRoundedRect(texture, new RectInt(8, 0, 16, height - 8), 4, new Color32(62, 82, 112, 255));
            FillRoundedRect(texture, new RectInt(10, height - 20, 12, 12), 3, new Color32(160, 220, 240, 255));
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateEnemyTexture()
        {
            const int size = 56;
            var texture = CreateTexture(size, size, new Color32(0, 0, 0, 0), "enemy_basic");

            FillDiamond(texture, new Vector2Int(size / 2, size / 2), size / 2 - 4, new Color32(180, 40, 40, 255));
            FillDiamond(texture, new Vector2Int(size / 2, size / 2), size / 2 - 12, new Color32(240, 120, 60, 255));
            DrawCircle(texture, new Vector2Int(size / 2, size / 2), 6, new Color32(250, 220, 180, 255));
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateProjectileTexture()
        {
            const int width = 16;
            const int height = 32;
            var texture = CreateTexture(width, height, new Color32(0, 0, 0, 0), "projectile_bolt");

            FillRoundedRect(texture, new RectInt(2, 2, width - 4, height - 4), 6, new Color32(250, 240, 190, 255));
            FillRoundedRect(texture, new RectInt(4, 6, width - 8, height - 12), 4, new Color32(240, 170, 30, 255));
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateXpOrbTexture()
        {
            const int size = 32;
            var texture = CreateTexture(size, size, new Color32(0, 0, 0, 0), "xp_orb");

            FillCircle(texture, new Vector2Int(size / 2, size / 2), size / 2 - 2, new Color32(60, 200, 120, 255));
            FillCircle(texture, new Vector2Int(size / 2, size / 2 + 4), size / 2 - 10, new Color32(190, 250, 220, 255));
            DrawBorder(texture, 2, new Color32(20, 80, 40, 255));
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateTexture(int width, int height, Color32 fill, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name
            };

            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = fill;
            }

            texture.SetPixels32(pixels);
            return texture;
        }

        private static void DrawCheckerboard(Texture2D texture, int cellSize, Color32 accent)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    bool offset = ((x / cellSize) + (y / cellSize)) % 2 == 0;
                    if (offset)
                    {
                        var current = texture.GetPixel(x, y);
                        texture.SetPixel(x, y, Color.Lerp(current, accent, 0.35f));
                    }
                }
            }
        }

        private static void DrawBorder(Texture2D texture, int thickness, Color32 color)
        {
            for (int x = 0; x < texture.width; x++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    texture.SetPixel(x, t, color);
                    texture.SetPixel(x, texture.height - 1 - t, color);
                }
            }

            for (int y = 0; y < texture.height; y++)
            {
                for (int t = 0; t < thickness; t++)
                {
                    texture.SetPixel(t, y, color);
                    texture.SetPixel(texture.width - 1 - t, y, color);
                }
            }
        }

        private static void FillRoundedRect(Texture2D texture, RectInt rect, int radius, Color32 color)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    if (IsInsideRoundedRect(x, y, rect, radius))
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static bool IsInsideRoundedRect(int x, int y, RectInt rect, int radius)
        {
            int rx = Mathf.Clamp(x, rect.xMin + radius, rect.xMax - radius - 1);
            int ry = Mathf.Clamp(y, rect.yMin + radius, rect.yMax - radius - 1);

            int dx = x - rx;
            int dy = y - ry;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static void DrawHighlight(Texture2D texture, RectInt rect)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                float strength = Mathf.Lerp(0.4f, 0f, Mathf.InverseLerp(rect.yMin, rect.yMax, y));
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    var baseColor = texture.GetPixel(x, y);
                    texture.SetPixel(x, y, Color.Lerp(baseColor, Color.white, strength));
                }
            }
        }

        private static void FillDiamond(Texture2D texture, Vector2Int center, int radius, Color32 color)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    int dx = Mathf.Abs(x - center.x);
                    int dy = Mathf.Abs(y - center.y);
                    if (dx + dy <= radius)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void DrawCircle(Texture2D texture, Vector2Int center, int radius, Color32 color)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    if ((x - center.x) * (x - center.x) + (y - center.y) * (y - center.y) <= radius * radius)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void FillCircle(Texture2D texture, Vector2Int center, int radius, Color32 color)
        {
            DrawCircle(texture, center, radius, color);
        }
    }
}
