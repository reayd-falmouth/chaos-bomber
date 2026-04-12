using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Arena
{
    /// <summary>
    /// Plays <see cref="MultiFaceDestroySpriteAnimation"/> once when the shrink indestructible block spawns (server and clients).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShrinkBlockAppearVfx : MonoBehaviour
    {
        private MultiFaceDestroySpriteAnimation _anim;

        private void Awake()
        {
            _anim = GetComponent<MultiFaceDestroySpriteAnimation>();
        }

        private void OnEnable()
        {
            if (_anim == null)
                _anim = GetComponent<MultiFaceDestroySpriteAnimation>();
            if (_anim == null)
                return;
            _anim.loop = false;
            _anim.Play();
        }
    }
}
