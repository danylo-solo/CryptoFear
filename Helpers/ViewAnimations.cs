namespace CryptoFear.Helpers;

public static class ViewAnimations
{
    public static async Task PressAnimationAsync(this View view, uint duration = 150)
    {
        await view.ScaleTo(0.96, duration / 2, Easing.CubicOut);
        await view.ScaleTo(1.0, duration / 2, Easing.CubicOut);
    }

    public static async Task FadeInFromBottomAsync(this View view, uint duration = 300, double distance = 20)
    {
        view.Opacity = 0;
        view.TranslationY = distance;

        await Task.WhenAll(
            view.FadeTo(1, duration, Easing.CubicOut),
            view.TranslateTo(0, 0, duration, Easing.CubicOut)
        );
    }

    public static async Task FadeInStaggeredAsync(this IEnumerable<View> views, uint delay = 50, uint duration = 300)
    {
        var viewList = views.ToList();

        foreach (var view in viewList)
        {
            view.Opacity = 0;
            view.TranslationY = 15;
        }

        var tasks = new List<Task>();
        for (int i = 0; i < viewList.Count; i++)
        {
            var view = viewList[i];
            var delayMs = i * delay;

            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay((int)delayMs);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Task.WhenAll(
                        view.FadeTo(1, duration, Easing.CubicOut),
                        view.TranslateTo(0, 0, duration, Easing.CubicOut)
                    );
                });
            }));
        }

        await Task.WhenAll(tasks);
    }

    public static void AnimateCountUp(this Label label, int targetValue, uint duration = 600)
    {
        var animation = new Animation(v =>
        {
            label.Text = ((int)v).ToString();
        }, 0, targetValue, Easing.CubicOut);

        animation.Commit(label, "CountUpAnimation", 16, duration);
    }

    public static async Task FadeOutAsync(this View view, uint duration = 200)
    {
        await view.FadeTo(0, duration, Easing.CubicIn);
    }

    public static async Task FadeInAsync(this View view, uint duration = 200)
    {
        view.Opacity = 0;
        await view.FadeTo(1, duration, Easing.CubicOut);
    }

    public static async Task BounceAsync(this View view, uint duration = 200)
    {
        await view.ScaleTo(1.05, duration / 2, Easing.CubicOut);
        await view.ScaleTo(1.0, duration / 2, Easing.BounceOut);
    }

    public static async Task SpringScaleAsync(this View view, double targetScale = 1.0, uint duration = 300)
    {
        var overshoot = targetScale + (targetScale * 0.08);
        await view.ScaleTo(overshoot, (uint)(duration * 0.6), Easing.CubicOut);
        await view.ScaleTo(targetScale, (uint)(duration * 0.4), Easing.CubicOut);
    }

    public static async Task ShakeAsync(this View view, uint duration = 400)
    {
        var segment = duration / 4;
        await view.TranslateTo(-10, 0, segment, Easing.CubicOut);
        await view.TranslateTo(8, 0, segment, Easing.CubicOut);
        await view.TranslateTo(-5, 0, segment, Easing.CubicOut);
        await view.TranslateTo(0, 0, segment, Easing.CubicOut);
    }

    public static void PerformHapticFeedback(HapticFeedbackType type = HapticFeedbackType.Click)
    {
        try
        {
            HapticFeedback.Default.Perform(type);
        }
        catch
        {
            // Not supported on this platform
        }
    }
}
