using RestSharp;

namespace OAuth2.Client
{
    public class RequestFactory : IRequestFactory
    {
        public IRestClient NewClient()
        {
            return new RestClient();
        }

        public IRestRequest NewRequest()
        {
            return new RestRequest();
        }
    }
}