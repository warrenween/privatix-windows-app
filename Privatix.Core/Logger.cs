using System.IO;
using System.Text;
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Config;
using log4net.Core;


namespace Privatix.Core
{
    public static class Logger
    {
        private static ILog _ilog;

        public static ILog Log
        {
            get { return _ilog ?? (_ilog = LogManager.GetLogger(typeof(Logger))); }
        }

        public static void InitLogger()
        {
            var fileAppender = new RollingFileAppender();

            var patternLayout = new PatternLayout
            {
                ConversionPattern =
                    "%date [%thread] %-5level %class.%method - %message : %exception%newline"
            };
            patternLayout.ActivateOptions();
            fileAppender.Name = "PrivatixAppender";
            fileAppender.CountDirection = 10;
            fileAppender.AppendToFile = true;
            fileAppender.Encoding = Encoding.UTF8;
            fileAppender.LockingModel = new FileAppender.MinimalLock();
            fileAppender.MaximumFileSize = "3MB";
            fileAppender.MaxSizeRollBackups = 5;
            fileAppender.RollingStyle = RollingFileAppender.RollingMode.Size;
            fileAppender.ImmediateFlush = true;
            fileAppender.StaticLogFileName = true;

            if (!Directory.Exists(Config.LogPath))
                Directory.CreateDirectory(Config.LogPath);

            fileAppender.File = Path.Combine(Config.LogPath, Config.LogFilename);

            fileAppender.Layout = patternLayout;
            fileAppender.Threshold = Level.All;

            fileAppender.ActivateOptions();
            BasicConfigurator.Configure(fileAppender);
        }
    }
}
