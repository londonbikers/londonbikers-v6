using ImageResizer.Configuration;
using ImageResizer.Configuration.Xml;
using ImageResizer.Plugins;
using ImageResizer.Util;
using LBV6Library;
using net.openstack.Core.Domain;
using net.openstack.Core.Providers;
using net.openstack.Providers.Rackspace;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Web;

namespace LBV6
{
    public class OpenStackObjectStoragePlugin : IPlugin, IVirtualImageProvider, IRedactDiagnostics
    {
        #region members
        private string _virtualFilesystemPrefix;
        #endregion

        #region accessors
        public string VirtualFilesystemPrefix
        {
            get { return _virtualFilesystemPrefix; }
            set
            {
                if (!value.EndsWith("/"))
                    value += "/";

                _virtualFilesystemPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative(value);
            }
        }
        private string OpenStackContainer { get; }
        private string OpenStackUsername { get; }
        private string OpenStackPassword { get; }
        private string OpenStackProjectName { get; }
        private string OpenStackRegion { get; }
        #endregion

        #region constructors
        public OpenStackObjectStoragePlugin(NameValueCollection args)
        {
            _virtualFilesystemPrefix = "~/os/";

            OpenStackContainer = args.Get("OpenStackContainer");
            OpenStackUsername = args.Get("OpenStackUsername");
            OpenStackPassword = args.Get("OpenStackPassword");
            OpenStackProjectName = args.Get("OpenStackProjectName");
            OpenStackRegion = args.Get("OpenStackRegion");
        }
        #endregion

        #region IVirtualImageProvider
        public bool FileExists(string virtualPath, NameValueCollection queryString)
        {
            // doing a http request to get object meta-data would seem to be the quickest way to get
            // a decision on whether or not a file exists in Object Storage but this is increasing latency.
            // better just to assume it exists and handle any Not Found situation downstream.
            return true;
        }

        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            Logging.LogDebug(GetType().FullName, $"virtualPath: {virtualPath}, queryString.Count: {queryString.Count}");

            // set a default container to load from
            var container = OpenStackContainer;

            // are we loading from a non-default container?
            if (queryString["c"] != null)
                container = queryString["c"];

            var ovhCloudFilesProvider = (CloudFilesProvider)HttpContext.Current.Application["ovhCloudFilesProvider"];

            // ReSharper disable once InvertIf - would result in duplication - WTF
            if (ovhCloudFilesProvider == null)
            {
                ovhCloudFilesProvider = GetFilesProvider();
                HttpContext.Current.Application["ovhCloudFilesProvider"] = ovhCloudFilesProvider;
            }

            return new OpenStackObjectStorageVirtualFile(ovhCloudFilesProvider, container, virtualPath);
        }
        #endregion

        #region IPlugin
        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
        #endregion

        #region IRedactDiagnostics
        public Node RedactFrom(Node resizer)
        {
            foreach (var n in resizer.queryUncached("plugins.add"))
            {
                if (n.Attrs["OpenStackUsername"] != null) n.Attrs.Set("OpenStackUsername", "[redacted]");
                if (n.Attrs["OpenStackPassword"] != null) n.Attrs.Set("OpenStackPassword", "[redacted]");
            }
            return resizer;
        }
        #endregion

        #region private methods
        private CloudFilesProvider GetFilesProvider()
        {
            var sw = new Stopwatch();
            sw.Start();

            var urlBase = new Uri("https://auth.cloud.ovh.net");
            var cloudIdentityWithProject = new CloudIdentityWithProject
            {
                Username = OpenStackUsername,
                Password = OpenStackPassword,
                ProjectName = OpenStackProjectName
            };

            var identityProvider = new OpenStackIdentityProvider(urlBase, cloudIdentityWithProject);
            var cloudFilesProvider = new CloudFilesProvider(null, OpenStackRegion, identityProvider, null);

            sw.Stop();
            Logging.LogDebug(GetType().FullName, $"GetFilesProvider(): Elapsed: {sw.Elapsed}");
            return cloudFilesProvider;
        }
        #endregion

        public class OpenStackObjectStorageVirtualFile : IVirtualFile
        {
            #region accessors
            public string VirtualPath { get; }
            private string Container { get; }
            private CloudFilesProvider CloudFilesProvider { get; }
            #endregion

            #region constructors
            public OpenStackObjectStorageVirtualFile(CloudFilesProvider cloudFilesProvider, string container, string virtualPath)
            {
                CloudFilesProvider = cloudFilesProvider;
                VirtualPath = virtualPath;
                Container = container;
            }
            #endregion

            public Stream Open()
            {
                Logging.LogDebug(GetType().FullName, $"Open(): Container: {Container}, VirtualPath: {VirtualPath}");

                // watermarks get loaded from disk
                // images get loaded from OVH cloud storage

                if (!VirtualPath.StartsWith("/os/"))
                {
                    // watermark
                    var watermarkStream = File.Open(HttpContext.Current.Server.MapPath(VirtualPath) ?? throw new InvalidOperationException(), FileMode.Open);
                    return watermarkStream;
                }

                var sw = new Stopwatch();
                sw.Start();

                // the Openstack object id is the filename without the extension.
                var objectName = VirtualPath.Substring(VirtualPath.LastIndexOf("/", StringComparison.Ordinal) + 1).Split('.')[0];

                var stream = new MemoryStream();
                CloudFilesProvider.GetObject(Container, objectName, stream);
                stream.Seek(0, SeekOrigin.Begin);

                sw.Stop();
                var speed = (Utilities.GetKilobytes(stream.Length) / sw.Elapsed.TotalSeconds).ToString("F0");
                Logging.LogDebug(GetType().FullName, $"VirtualPath: {VirtualPath}, Elapsed: {sw.Elapsed.TotalMilliseconds} ms. File Length: {Utilities.GetKilobytes(stream.Length):F0} KB. Speed: {speed} KB/sec");
                return stream;
            }
        }
    }
}