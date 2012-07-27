namespace OAuth2.Infrastructure
{
    public interface IConfiguration
    {
        T Get<T>();
    }
}