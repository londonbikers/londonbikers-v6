using LBV6Library;
using Microsoft.ApplicationInsights;
using System.Web.Http.ExceptionHandling;

namespace LBV6
{
    /// <inheritdoc />
    /// <summary>
    /// Used by the app to catch all exceptions and send them to Azure Application Insights and Log4Net.
    /// </summary>
    public class AiExceptionLogger : ExceptionLogger
    {
        public override void Log(ExceptionLoggerContext context)
        {
            if (context?.Exception != null)
            {
                var ai = new TelemetryClient();
                ai.TrackException(context.Exception);

                // also log to Log4Net
                Logging.LogPageError(GetType().FullName, context.Request.RequestUri.AbsoluteUri, context.Exception);
            }

            base.Log(context);
        }
    }
}