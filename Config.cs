using System;
using System.Collections.Generic;
using System.Text;

using System.Configuration;

namespace POC.Console.Sender
{
    class Config
    {
        public static string GetConfig(string configKey)
        {
            string sResult = string.Empty;
            sResult = System.Configuration.ConfigurationSettings.AppSettings[configKey];
            return sResult;
        }
    }
}
