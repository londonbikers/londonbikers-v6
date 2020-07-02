using LBV6.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Specialized;
using System.Configuration;

namespace LBV6.Tests
{
    /// <summary>
    /// This class tests code within the LBV6 Global.asax.cs class.
    /// </summary>
    [TestClass]
    public class LegacyRedirectTests
    {
        [TestMethod]
        public void RedirectLegacyTopicShortFormUrlNoIdTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/Topic.aspx";
            const string path = "/forums/Topic.aspx";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNull(redirect);
        }

        [TestMethod]
        public void RedirectLegacyTopicShortFormUrlNoSuchIdTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/Topic000000.aspx";
            const string path = "/forums/Topic000000.aspx";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual(ConfigurationManager.AppSettings["LB.Url"], redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyTopicShortFormUrlForm1Test()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/Topic812033.aspx";
            const string path = "/forums/Topic812033.aspx";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/posts/230427/hiprotec-any-good", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyTopicShortFormUrlForm2Test()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/topic760445-44-1.aspx";
            const string path = "/forums/topic760445-44-1.aspx";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/posts/191949/blackbird-xx-owners", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyPrintTopicUrlTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/printtopic812033.aspx";
            const string path = "/forums/printtopic812033.aspx";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/posts/230427/hiprotec-any-good", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyFindPostUrlTest()
        {
            var app = new MvcApplication();

            #region reply
            var absoluteUri = "http://localhost/forums/FindPost742650.aspx";
            var path = "/forums/FindPost742650.aspx";
            var isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/posts/791858/just-orderd-a-cutthroat-razor-from-amazon", redirect.Value.Url);
            #endregion

            #region topic
            absoluteUri = "http://localhost/forums/FindPost740848.aspx";
            path = "/forums/FindPost740848.aspx";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/posts/791858/just-orderd-a-cutthroat-razor-from-amazon", redirect.Value.Url);
            #endregion

        }

        [TestMethod]
        public void RedirectLegacyRecentUrlHttpTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/recent";
            const string path = "/forums/recent";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/latest", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyPostUrlReplyTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/posts/505798";
            const string path = "/forums/posts/505798";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/posts/91473/road-legal-green-lane-bike?hid=91689", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyPostUrlReplySecondPageTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/posts/934811";
            const string path = "/forums/posts/934811";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/posts/277674/newbie-facebike-photo-sheet?p=2&hid=278442", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyPostUrlTopicTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/posts/914509";
            const string path = "/forums/posts/914509";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/posts/277674/newbie-facebike-photo-sheet", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyTopicUrlTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/posts/1101459";
            const string path = "/forums/posts/1101459";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/posts/1043831/time-for-a-reboot-relaunching-lb", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyTopicUrlPage2Test()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/1101459/time-for-a-reboot-relaunching-lb";
            const string path = "/forums/1101459/time-for-a-reboot-relaunching-lb";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection {{"PageIndex", "2"}};

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/forums/posts/1043831/time-for-a-reboot-relaunching-lb?p=2", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectTemporarilyUnsupportedUrlTest()
        {
            var app = new MvcApplication();

            #region news
            var absoluteUri = "http://localhost/news";
            var path = "/news";
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, false, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Temporary);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region features
            absoluteUri = "http://localhost/features";
            path = "/features";
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, false, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Temporary);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region articles
            absoluteUri = "http://localhost/articles";
            path = "/articles";
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, false, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Temporary);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region galleries
            absoluteUri = "http://localhost/galleries";
            path = "/galleries";
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, false, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Temporary);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region store
            absoluteUri = "http://localhost/store";
            path = "/store";
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, false, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Temporary);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region competitions
            absoluteUri = "http://localhost/competitions";
            path = "/competitions";
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, false, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Temporary);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region charity
            absoluteUri = "http://localhost/charity";
            path = "/charity";
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, false, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Temporary);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region advertising
            absoluteUri = "http://localhost/advertising";
            path = "/advertising";
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, false, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Temporary);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion
        }

        [TestMethod]
        public void RedirectPermanentlyUnsupportedUrlTest()
        {
            var app = new MvcApplication();

            #region events
            var absoluteUri = "http://localhost/events";
            var path = "/events";
            var isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region directory
            absoluteUri = "http://localhost/directory";
            path = "/directory";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region partners
            absoluteUri = "http://localhost/partners";
            path = "/partners";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region tags
            absoluteUri = "http://localhost/tags";
            path = "/tags";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region blog
            absoluteUri = "http://localhost/blog";
            path = "/blog";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region tv
            absoluteUri = "http://localhost/tv";
            path = "/tv";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region story.aspx
            absoluteUri = "http://localhost/story.aspx";
            path = "/story.aspx";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region bsb
            absoluteUri = "http://localhost/bsb";
            path = "/bsb";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region motogp
            absoluteUri = "http://localhost/motogp";
            path = "/motogp";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region podcast
            absoluteUri = "http://localhost/podcast";
            path = "/podcast";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region technology
            absoluteUri = "http://localhost/technology";
            path = "/technology";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region products
            absoluteUri = "http://localhost/products";
            path = "/products";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region forums/rssfeed
            absoluteUri = "http://localhost/forums/rssfeed";
            path = "/forums/rssfeed";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region insurance
            absoluteUri = "http://localhost/insurance";
            path = "/insurance";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion

            #region forum calendar
            absoluteUri = "http://localhost/forums/Calendar26-12-2269-2.aspx";
            path = "/forums/Calendar26-12-2269-2.aspx";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/change", redirect.Value.Url);
            #endregion
        }

        [TestMethod]
        public void RedirectTemporarilyLegacyImageHandlerUrlTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/img.ashx?id=54c1e16d-595c-4db0-866e-616ab1800a0f";
            const string path = "/img.ashx?id=54c1e16d-595c-4db0-866e-616ab1800a0f";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Temporary);
            Assert.AreEqual("https://localhost/content/images/lb-image-not-available.png", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectPermanentlyLegacyImageHandlerUrlTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/forums/uploads/avatars/54c1e16d-595c-4db0-866e-616ab1800a0f.jpg";
            const string path = "/forums/uploads/avatars/54c1e16d-595c-4db0-866e-616ab1800a0f.jpg";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/content/images/lb-image-not-available.png", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyNonConvertibleUrlTest()
        {
            var app = new MvcApplication();

            #region avatars
            var absoluteUri = "http://localhost/forums/uploads/avatars/54c1e16d-595c-4db0-866e-616ab1800a0f.jpg";
            var path = "/forums/uploads/avatars/54c1e16d-595c-4db0-866e-616ab1800a0f.jpg";
            var isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/content/images/lb-image-not-available.png", redirect.Value.Url);
            #endregion

            #region private messages
            absoluteUri = "http://localhost/forums/PrivateMessage8658-228278.aspx";
            path = "/forums/PrivateMessage8658-228278.aspx";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual(ConfigurationManager.AppSettings["LB.Url"], redirect.Value.Url);
            #endregion
        }

        [TestMethod]
        public void RedirectLegacyLoginUrlTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/login";
            const string path = "/login";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/account/sign-in", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyRegisterUrlTest()
        {
            var app = new MvcApplication();

            const string absoluteUri = "http://localhost/register";
            const string path = "/register";
            const bool isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/account/register", redirect.Value.Url);
        }

        [TestMethod]
        public void RedirectLegacyMiscUrlTest()
        {
            var app = new MvcApplication();

            #region convertible url
            var absoluteUri = "http://localhost/contact";
            var path = "/contact";
            var isSecureConnection = false;
            var queryString = new NameValueCollection();

            var redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNotNull(redirect);
            Assert.IsTrue(redirect.HasValue);
            Assert.AreEqual(redirect.Value.Type, RedirectType.Permanent);
            Assert.AreEqual("https://localhost/contact", redirect.Value.Url);
            #endregion

            #region well-known
            absoluteUri = "http://localhost/.well-known/something";
            path = "/.well-known/";
            isSecureConnection = false;
            queryString = new NameValueCollection();

            redirect = app.RedirectLegacyUrls(absoluteUri, path, isSecureConnection, queryString);

            Assert.IsNull(redirect);
            #endregion
        }
    }
}