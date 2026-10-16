using System;

namespace NifRenderer.UI.Common
{
    public static class UIScale
    {
        private const float Min = 0.75f;

        private const float Max = 2.5f;

        public static float Factor { get; private set; } = 1.5f;

        public static event Action? Changed;

        public static float Value(float logical) => logical * Factor;

        public static int Value(int logical) => (int)MathF.Round(logical * Factor);

        public static void Set(float factor)
        {
            var clamped = Math.Clamp(factor, Min, Max);

            if (MathF.Abs(clamped - Factor) < 0.001f)
                return;

            Factor = clamped;

            Changed?.Invoke();
        }

        public static void Adjust(int steps) => Set(Factor + steps * 0.1f);
    }
}
