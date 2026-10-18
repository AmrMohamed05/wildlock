using UnityEngine;

namespace Wildlock
{
    /// <summary>
    /// Receives AnimationEvents (OnFootstep, OnLand) embedded in StarterAssetsThirdPerson animation clips
    /// so Unity never logs missing-receiver warnings on Player or NPC Animator GameObjects.
    /// </summary>
    public class WildlockAnimationEventReceiver : MonoBehaviour
    {
        public void OnFootstep(AnimationEvent animationEvent)
        {
            // Optional subtle footstep feedback handled silently
        }

        public void OnLand(AnimationEvent animationEvent)
        {
            // Optional landing feedback handled silently
        }
    }
}
