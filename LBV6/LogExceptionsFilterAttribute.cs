using LBV6Library;
using System.Web.Http.Filters;

namespace LBV6
{
    public class LogExceptionsFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            Logging.LogPageError(GetType().FullName, context.Request.RequestUri.AbsoluteUri, context.Exception);
        }
    }
}