using System.Web.Mvc;

namespace LBV6
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new ErrorHandler.AiHandleErrorAttribute());
            //filters.Add(new HandleErrorAttribute());
        }
    }
}