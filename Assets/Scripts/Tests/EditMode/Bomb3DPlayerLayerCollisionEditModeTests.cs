using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Guardrail: CharacterController blocking for armed bombs requires Bomb3D and Player to collide in the project matrix.
    /// </summary>
    public class Bomb3DPlayerLayerCollisionEditModeTests
    {
        [Test]
        public void Bomb3D_and_Player_layers_are_not_ignored_by_physics()
        {
            int bomb = LayerMask.NameToLayer("Bomb3D");
            int player = LayerMask.NameToLayer("Player");
            Assert.That(bomb, Is.GreaterThanOrEqualTo(0), "Bomb3D layer must exist");
            Assert.That(player, Is.GreaterThanOrEqualTo(0), "Player layer must exist");
            Assert.That(Physics.GetIgnoreLayerCollision(bomb, player), Is.False,
                "Bomb3D and Player must collide so armed bombs block CharacterController movement.");
        }
    }
}
