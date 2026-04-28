using UnityEngine;

namespace Game.UI
{
    public static class MapPathRevealState
    {
        private static float revealUntilRealtime;
        private static float lastDuration;
        private static System.Action replayAction;

        public static bool IsRevealed => Time.realtimeSinceStartup < revealUntilRealtime;

        public static void Reveal(float duration, System.Action onRevealAgain = null)
        {
            if (duration <= 0f) return;
            lastDuration = duration;
            replayAction = onRevealAgain;
            revealUntilRealtime = Time.realtimeSinceStartup + duration;
        }

        public static void RevealAgain()
        {
            if (lastDuration <= 0f) return;
            revealUntilRealtime = Time.realtimeSinceStartup + lastDuration;
            replayAction?.Invoke();
        }
    }
}
