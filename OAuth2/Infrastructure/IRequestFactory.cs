using RestSharp;

namespace OAuth2.Infrastructure
{
    public interface IRequestFactory
    {
        IRestClient NewClient();
        IRestRequest NewRequest();
    }
}