namespace IntroSkip
{
    internal interface ISkipDisplayService
    {
        bool Active { get; }
        void Show();
        void Hide();
    }
}