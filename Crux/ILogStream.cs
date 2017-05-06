namespace Crux
{
    public interface ILogStream
    {
        void Write(string msg, int level);
    }
}
