namespace IntroSkip.Displays
{
    internal class NoSkipDisplayService : ISkipDisplayService
    {
        public bool Active => false;

        public void Show() => _ = true;
        public void Hide() => _ = true;
    }
}