namespace Waluty.Engine.AppBoot
{
    public interface IServiceLocator
    {
        T GetInstance<T>() where T : class;
    }
}
