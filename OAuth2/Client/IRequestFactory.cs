using RestSharp;

namespace OAuth2.Client
{
    public interface IRequestFactory
    {
        IRestClient NewClient();
        IRestRequest NewRequest();
    }
}