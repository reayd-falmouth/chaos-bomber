using HybridGame.MasterBlaster.Scripts.Player.Input;
using UnityEngine;

namespace HybridGame.MasterBlaster.Scripts.Online
{
    /// <summary>
    /// IPlayerInput implementation driven by the host via ServerRpc/ClientRpc.
    /// Attach to player GameObjects on remote clients. The host calls ReceiveInput()
    /// after receiving SendInputServerRpc from the real player's client.
    /// </summary>
    public class NetworkPlayerInput : MonoBehaviour, IPlayerInput
    {
        private Vector2 _move;
        private bool _bombDown;
        private bool _detonate;

        /// <summary>Called by the host to push input received from the owning client. Move is logical XZ: (world X, world Z) in x/y.</summary>
        public void ReceiveInput(Vector2 move, bool bombDown, bool detonate)
        {
            _move = move;
            _bombDown = bombDown;
            _detonate = detonate;
        }

        public Vector2 GetMoveDirection() => _move;

        public bool GetBombDown()
        {
            bool v = _bombDown;
            _bombDown = false; // consume on read (one-shot)
            return v;
        }

        public bool GetDetonateHeld() => _detonate;
    }
}
