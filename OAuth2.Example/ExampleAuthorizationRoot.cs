using OAuth2.Configuration;
using OAuth2.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OAuth2.Example
{
    public class ExampleAuthorizationRoot : AuthorizationRoot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationRoot" /> class.
        /// </summary>
        /// <remarks>
        /// Since this is boundary class, we decided to create 
        /// parameterless constructor where default implementations of dependencies are used.
        /// So, despite we encourage you to employ IoC pattern, 
        /// you are still able to just create instance of manager manually and then use it in your project.
        /// </remarks>
        public ExampleAuthorizationRoot() : 
            base(new ConfigurationManager(), "oauth2", new RequestFactory())
        {
        }
    }
}
