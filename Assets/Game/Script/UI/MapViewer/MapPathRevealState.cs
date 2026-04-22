using UnityEngine;

namespace Game.UI
{
    public static class MapPathRevealState
    {
        private static float revealUntilRealtime;

        public static bool IsRevealed => Time.realtimeSinceStartup < revealUntilRealtime;

        public static void Reveal(float duration)
        {
            if (duration <= 0f) return;
            revealUntilRealtime = Time.realtimeSinceStartup + duration;
        }
    }
}
