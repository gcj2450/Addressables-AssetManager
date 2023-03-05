namespace ZionGame.Example
{
    public interface IProgressBar
    {
        void SetVisible(bool visible);
        void SetProgress(float progress);
        void SetMessage(string message);
    }
}