﻿using OAuth2.Configuration;
namespace OAuth2.Infrastructure
{
    /// <summary>
    /// Defines API for <see cref="System.Configuration.ConfigurationManager"/> wrapper.
    /// </summary>
    public interface IConfigurationManager
    {
        /// <summary>
        /// Returns value obtained from 
        /// <see cref="System.Configuration.ConfigurationManager.AppSettings"/> using specified key.
        /// </summary>
        OAuthSettings GetSetting(string sectionName);
    }
}