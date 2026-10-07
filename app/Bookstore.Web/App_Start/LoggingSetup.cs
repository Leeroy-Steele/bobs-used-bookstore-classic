using NLog.AWS.Logger;
using BobsBookstoreClassic.Data;
using Bookstore.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace Bookstore.Web
{
    public static class LoggingSetup
    {
        public static void ConfigureLogging(ILoggingBuilder loggingBuilder, IConfiguration configuration)
        {
            loggingBuilder.ClearProviders();

            var config = new LoggingConfiguration();
            Target loggingTarget;

            if (BookstoreConfiguration.TryGetSetting("Services/LoggingService") == "aws")
            {
                loggingTarget = new AWSTarget { LogGroup = Constants.AppName };
            }
            else
            {
                loggingTarget = new DebuggerTarget();
            }

            config.AddTarget("logging", loggingTarget);
            config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Info, loggingTarget));
            LogManager.Configuration = config;

            loggingBuilder.AddNLog(config);
        }
    }
}
