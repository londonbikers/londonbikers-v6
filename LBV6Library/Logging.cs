using log4net;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LBV6Library
{
    /// <summary>
    /// Log captures are differentiated by category, i.e. debug, info, warning and error.
    /// - Debug is the lowest level and should not used in production.
    /// - Info is used for metrics and behaviour trending analysis.
    /// - Warning is for events that are recoverable from.
    /// - Error events cannot be recovered from and impact the application flow.
    /// </summary>
    /// <remarks>
    /// Make sure you add the following lines to your web.config file appsettings node:
    /// add key="log4net.Config" value="..\log4net.config"
    /// add key="log4net.Config.Watch" value="True"
    /// </remarks>
    public static class Logging
    {
        #region debug
        /// <summary>
        /// Logs information to do with metrics and behaviour trending.
        /// This overload is only for cases where you need to specify the calling class or method. Use the simpler overload to have this
        /// worked out automatically.
        /// </summary>
        public static void LogDebug(string callerName, string message, [CallerMemberName] string memberName = "")
        {
            var log = LogManager.GetLogger(callerName + "." + memberName);
            log.Debug(message);
            Debug.WriteLine(">>>> " + message);
        }
        #endregion

        #region info
        /// <summary>
        /// Logs information to do with metrics and behaviour trending.
        /// This overload is only for cases where you need to specify the calling class or method. Use the simpler overload to have this
        /// worked out automatically.
        /// </summary>
        public static void LogInfo(string callerName, string message, ConsoleColor backgroundColour = ConsoleColor.Black, [CallerMemberName] string memberName = "")
        {
            var log = LogManager.GetLogger(callerName + "." + memberName);
            log.Info(message);
            Debug.WriteLine(">>>> " + message);
        }
        #endregion

        #region warnings
        public static void LogWarning(string callerName, string message, [CallerMemberName] string memberName = "")
        {
            var log = LogManager.GetLogger(callerName + "." + memberName);
            log.Warn(message);
            Debug.WriteLine(">>>> " + message);
        }
        #endregion

        #region errors
        public static void LogError(string callerName, Exception exception, [CallerMemberName] string memberName = "")
        {
            var log = LogManager.GetLogger(callerName + "." + memberName);
            log.Error(exception);
            Debug.WriteLine(">>>> " + exception.Message);
        }

        public static void LogError(string callerName, string message, Exception exception, [CallerMemberName] string memberName = "")
        {
            var log = LogManager.GetLogger(callerName + "." + memberName);
            log.Error(message, exception);
            Debug.WriteLine(">>>> " + message);
        }

        public static void LogError(string callerName, string message, [CallerMemberName] string memberName = "")
        {
            var log = LogManager.GetLogger(callerName + "." + memberName);
            log.Error(message);
            Debug.WriteLine(">>>> " + message);
        }

        public static void LogPageError(string callerName, string url, Exception exception, [CallerMemberName] string memberName = "")
        {
            var message = exception != null ? $"({url}) {exception.Message}" : $"({url}) null exception";
            var log = LogManager.GetLogger(callerName + "." + memberName);
            log.Error(message, exception);
            Debug.WriteLine(">>>> " + message);
        }
        #endregion
    }
}
