using UnityEngine;

namespace PnceHarekat
{
    /// <summary>
    /// Floating XP orb inspired by the HTML prototype. The orb will slowly move
    /// towards the player when nearby and awards experience on pickup.
    /// </summary>
    public class XpOrbController : MonoBehaviour
    {
        private GameController game;
        private PlayerController player;
        private float xpAmount;
        private SpriteRenderer renderer;
        private float hoverTimer;

        public void Initialise(GameController owner, PlayerController collector, Vector3 position, float amount)
        {
            game = owner;
            player = collector;
            xpAmount = amount;
            transform.position = position;

            renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteLibrary.LoadSprite("Sprites/xp_orb");
            renderer.color = Color.white;
            renderer.sortingOrder = 1;
            transform.localScale = Vector3.one;

            game.RegisterXpOrb(this);
        }

        private void OnDestroy()
        {
            if (game != null)
            {
                game.UnregisterXpOrb(this);
            }
        }

        private void Update()
        {
            if (game == null || player == null)
            {
                return;
            }

            hoverTimer += Time.deltaTime;
            float baseScale = 0.75f;
            float wobble = 0.08f;
            transform.localScale = new Vector3(baseScale + Mathf.Sin(hoverTimer * 3f) * wobble, baseScale + Mathf.Sin(hoverTimer * 3f + 1f) * wobble, 1f);

            Vector3 toPlayer = player.transform.position - transform.position;
            float distance = toPlayer.magnitude;
            if (game.State == GameState.Playing)
            {
                if (distance < 3f)
                {
                    transform.position += toPlayer.normalized * Time.deltaTime * Mathf.Lerp(1.5f, 6f, Mathf.Clamp01(1f - distance / 3f));
                }

                if (distance < 0.5f)
                {
                    player.CollectXp(xpAmount);
                    Destroy(gameObject);
                }
            }
        }
    }
}
