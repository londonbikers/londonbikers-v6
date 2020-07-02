using System;
using System.Threading;
using System.Web.Hosting;

namespace LBV6Library
{
    /// <summary>
    /// add some jobs to the background queue
    /// </summary>
    public static class BackgroundTaskScheduler
    {
        /// <summary>
        /// Send the work item to the background queue
        /// </summary>
        /// <param name="workItem">work item to enqueue</param>
        public static void QueueBackgroundWorkItem(Action<CancellationToken> workItem)
        {
            try
            {
                HostingEnvironment.QueueBackgroundWorkItem(workItem);
            }
            catch (InvalidOperationException)
            {
                workItem.Invoke(new CancellationToken());
            }
        }
    }
}