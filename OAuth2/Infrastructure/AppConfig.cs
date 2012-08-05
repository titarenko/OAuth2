namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Implementation of <see cref="IConfiguration"/> which is based on <see cref="System.Configuration.ConfigurationManager.AppSettings"/>.
    /// </summary>
    public class AppConfig : Configuration
    {
        private readonly IConfigurationManager configurationManager;
        private readonly string sectionName;
        private readonly bool allowInheritance;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfig"/> class.
        /// </summary>
        public AppConfig(IConfigurationManager configurationManager)
        {
            this.configurationManager = configurationManager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfig"/> class.
        /// </summary>
        private AppConfig(IConfigurationManager configurationManager, string sectionName, bool allowInheritance) 
            : this(configurationManager)
        {
            this.sectionName = sectionName;
            this.allowInheritance = allowInheritance;
        }

        /// <summary>
        /// Returns configuration section by name.
        /// </summary>
        /// <param name="name">The section name.</param>
        /// <param name="allowInheritance">Allows read values from parent section if true.</param>
        public override IConfiguration GetSection(string name, bool allowInheritance = true)
        {
            return new AppConfig(configurationManager, name, allowInheritance);
        }

        /// <summary>
        /// Returns value by key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override string Get(string key)
        {
            return sectionName.IsEmpty()
                       ? configurationManager.GetAppSetting(key)
                       : configurationManager.GetAppSetting("{0}.{1}".Fill(sectionName, key))
                         ?? (allowInheritance ? configurationManager.GetAppSetting(key) : null);
        }
    }
}