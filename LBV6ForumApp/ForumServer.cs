using LBV6ForumApp.Controllers;
using LBV6Library;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Configuration;

namespace LBV6ForumApp
{
    public class ForumServer : IDisposable
    {
        #region members
        private static volatile ForumServer _instance;
        private static readonly object SyncRoot = new object();
        #endregion

        #region accessors
        /// <summary>
        /// Interact with the application via this property.
        /// </summary>
        public static ForumServer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (SyncRoot)
                {
                    if (_instance == null)
                        _instance = new ForumServer();
                }

                return _instance;
            }
        }

        public CategoryController Categories { get; }
        public ForumController Forums { get; }
        public PostController Posts { get; }
        public PhotosController Photos { get; }
        public EmailController Emails { get; }
        public PrivateMessageController Messages { get; }
        public UserController Users { get; }
        public NotificationController Notifications { get; }
        public BroadcastController Broadcasts { get; }

        internal CacheController Cache { get; }
        internal PostProcessController PostProcessing { get; }
        internal IndexController Indexes { get; }
        internal TelemetryClient Telemetry { get; }
        #endregion

        #region constructors
        private ForumServer()
        {
            Logging.LogInfo(GetType().FullName, "Constructing ForumServer.");
            TelemetryConfiguration.Active.InstrumentationKey = ConfigurationManager.AppSettings["ApplicationInsights.InstrumentationKey"];

            Categories = new CategoryController();
            Forums = new ForumController();
            Posts = new PostController();
            Photos = new PhotosController();
            
            Indexes = new IndexController();
            Emails = new EmailController();
            Messages = new PrivateMessageController();
            Users = new UserController(Posts, Photos);
            Notifications = new NotificationController(this);
            Cache = new CacheController();
            PostProcessing = new PostProcessController(this);
            Broadcasts = new BroadcastController();
            Telemetry = new TelemetryClient();
        }
        #endregion

        public void Dispose()
        {
            Logging.LogInfo(GetType().FullName, "Shutting ForumServer instance down.");
            Posts.Dispose();
        }
    }
}