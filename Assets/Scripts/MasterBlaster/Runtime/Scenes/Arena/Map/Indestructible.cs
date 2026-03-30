using HybridGame.MasterBlaster.Scripts.Player;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Scenes.Arena.Map
{
    [RequireComponent(typeof(Collider))]
    public class Indestructible : MonoBehaviour
    {
        private AnimatedSpriteRenderer anim;

        private void Awake()
        {
            anim = GetComponent<AnimatedSpriteRenderer>();
        }

        private void Start()
        {
            if (anim != null)
                anim.StartAnimation();
        }
    }
}
