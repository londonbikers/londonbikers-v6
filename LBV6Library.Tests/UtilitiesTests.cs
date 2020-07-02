using LBV6Library.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace LBV6Library.Tests
{
    [TestClass]
    public class UtilitiesTests
    {
        [TestMethod]
        public void IsRichTextPostTest()
        {
            const string i1 = @"They fly like planes.

http://www.hdwallpapers.in/walls/planes_movie-wide.jpg";
            var o1 = Utilities.IsRichTextContent(i1);
            Assert.IsFalse(o1);

            const string i2 = @"<p>How do replies look with new lines?</p><p>I don't know, checking it out.</p>";
            var o2 = Utilities.IsRichTextContent(i2);
            Assert.IsTrue(o2);

            const string i3 = "<figure><img src=\"https://i.imgur.com/WHR1lr9.jpg\"></figure>";
            var o3 = Utilities.IsRichTextContent(i3);
            Assert.IsTrue(o3);

            const string i4 = "<ol><li>One</li><li>Two</li><li>Three</li></ol>";
            var o4 = Utilities.IsRichTextContent(i4);
            Assert.IsTrue(o4);

            const string i5 = "erewrewr > something";
            var o5 = Utilities.IsRichTextContent(i5);
            Assert.IsFalse(o5);
        }

        [TestMethod]
        public void GetPostSynopsisTest()
        {
            const string i1 = "Donec at eleifend diam, non accumsan justo. Vestibulum metus tortor, suscipit nec mi sit amet, tincidunt iaculis felis. In bibendum varius elit, eget consectetur leo egestas id. Phasellus ac nisl odio. Mauris augue sem, tincidunt non orci eget, blandit auctor augue. Suspendisse orci neque, mollis ut arcu in, scelerisque placerat urna. Fusce venenatis porttitor ipsum sit amet ornare. Phasellus pellentesque orci ut consequat lacinia. Nulla enim augue, maximus ut lorem ac, condimentum blandit mi. Sed eu enim pulvinar, varius lectus et, sagittis odio. Ut dapibus, neque non elementum condimentum, nulla justo luctus elit, id viverra massa ante vel tellus. Quisque blandit ante in diam egestas posuere. Duis id libero vitae metus tincidunt dapibus. Donec venenatis, arcu eu accumsan gravida, nisl est mollis ligula, sit amet convallis sem ligula in leo. Cras vestibulum nisl nec elit cursus sollicitudin quis id mi. Curabitur congue felis et massa maximus, quis consectetur dolor sagittis.";
            var o1 = Utilities.GetContentSynopsis(i1);
            Assert.IsFalse(string.IsNullOrEmpty(o1));
            Assert.AreEqual("Donec at eleifend diam, non accumsan justo. Vestibulum metus tortor, suscipit nec mi sit amet,...", o1);

            var o2 = Utilities.GetContentSynopsis(i1);
            Assert.IsFalse(string.IsNullOrEmpty(o2));
            Assert.AreNotEqual("Donec at eleifend diam, non accumsan justo. Vestibulum", o2);

            const string i3 = "<p>How do replies look with new lines?</p><p>I don't know, checking it out.</p>";
            var o3 = Utilities.GetContentSynopsis(i3);
            Assert.IsFalse(string.IsNullOrEmpty(o3));
            Assert.AreEqual("How do replies look with new lines?...", o3);

            const string i4 = "<p>Single Line</p>";
            var o4 = Utilities.GetContentSynopsis(i4);
            Assert.IsFalse(string.IsNullOrEmpty(o4));
            Assert.AreEqual("Single Line", o4);

            const string i5 = @"<p>This is a test post using the new editor.</p>

<p>It's <strong>wonderful</strong>.</p><ol><li>Start your bike</li><li>Ride your bike</li><li>Be happy</li><li>Come home safely</li></ol>";
            var o5 = Utilities.GetContentSynopsis(i5);
            Assert.IsFalse(string.IsNullOrEmpty(o5));
            Assert.AreEqual("This is a test post using the new editor....", o5);

            const string i6 = "<figure><img src=\"https://i.imgur.com/WHR1lr9.jpg\"></figure>";
            var o6 = Utilities.GetContentSynopsis(i6);
            Assert.IsFalse(string.IsNullOrEmpty(o6));
            Assert.AreEqual("...", o6);

            const string i7 = "<ol><li>One</li><li>Two</li><li>Three</li></ol>";
            var o7 = Utilities.GetContentSynopsis(i7);
            Assert.IsFalse(string.IsNullOrEmpty(o7));
            Assert.AreEqual("OneTwoThree...", o7);

            const string i8 = "<p>Yada yada.&nbsp;Testing edits.</p>";
            var o8 = Utilities.GetContentSynopsis(i8);
            Assert.IsFalse(string.IsNullOrEmpty(o8));
            Assert.AreEqual("Yada yada. Testing edits.", o8);
        }

        [TestMethod]
        public void TrimNeatlyTest()
        {
            const string i1 = "Donec at eleifend diam, non accumsan justo. Vestibulum metus tortor, suscipit nec mi sit amet, tincidunt iaculis felis. In bibendum varius elit, eget consectetur leo egestas id. Phasellus ac nisl odio. Mauris augue sem, tincidunt non orci eget, blandit auctor augue. Suspendisse orci neque, mollis ut arcu in, scelerisque placerat urna. Fusce venenatis porttitor ipsum sit amet ornare. Phasellus pellentesque orci ut consequat lacinia. Nulla enim augue, maximus ut lorem ac, condimentum blandit mi. Sed eu enim pulvinar, varius lectus et, sagittis odio. Ut dapibus, neque non elementum condimentum, nulla justo luctus elit, id viverra massa ante vel tellus. Quisque blandit ante in diam egestas posuere. Duis id libero vitae metus tincidunt dapibus. Donec venenatis, arcu eu accumsan gravida, nisl est mollis ligula, sit amet convallis sem ligula in leo. Cras vestibulum nisl nec elit cursus sollicitudin quis id mi. Curabitur congue felis et massa maximus, quis consectetur dolor sagittis.";

            var o1 = Utilities.TrimNeatly(i1, 10);
            Assert.IsFalse(string.IsNullOrEmpty(o1));
            Assert.AreEqual("Donec at", o1);

            var o2 = Utilities.TrimNeatly(i1, 20);
            Assert.IsFalse(string.IsNullOrEmpty(o2));
            Assert.AreEqual("Donec at eleifend", o2);

            var o3 = Utilities.TrimNeatly(i1, 50);
            Assert.IsFalse(string.IsNullOrEmpty(o3));
            Assert.AreEqual("Donec at eleifend diam, non accumsan justo.", o3);

            var o4 = Utilities.TrimNeatly(i1, 50, "...");
            Assert.IsFalse(string.IsNullOrEmpty(o4));
            Assert.AreEqual("Donec at eleifend diam, non accumsan justo....", o4);
        }

        [TestMethod]
        public void ConvertContentLegacyUrlsTest()
        {
            const string c1 = "<IMG src=\"http://londonbikers.com/forums/uploads/Images/8accd408-1ec0-4b58-882f-a090.jpg\">";
            var r1 = Utilities.ConvertContentLegacyUrls(c1);
            Assert.IsFalse(string.IsNullOrEmpty(r1));
            Assert.AreEqual("<IMG src=\"/_images/8accd408-1ec0-4b58-882f-a090.jpg\">", r1);

            // ------------------------

            const string c2 = @"http://www.bbc.co.uk/news/uk-england-cambridgeshire-34343385

Facepalm...";
            var r2 = Utilities.ConvertContentLegacyUrls(c2);
            Assert.IsFalse(string.IsNullOrEmpty(r2));
            Assert.AreEqual(c2, r2);

            // ------------------------

            const string c3 = null;
            var r3 = Utilities.ConvertContentLegacyUrls(c3);
            Assert.IsNull(r3);

            // ------------------------

            const string c4 = "";
            var r4 = Utilities.ConvertContentLegacyUrls(c4);
            Assert.IsNotNull(r4);
            Assert.AreEqual("", r4);

            // ------------------------

            const string c5 = @"here's Scorch's thread: https://londonbikers.com/forums/posts/952930/problems-with-new-ajs";
            var r5 = Utilities.ConvertContentLegacyUrls(c5);
            Assert.IsFalse(string.IsNullOrEmpty(r5));
            Assert.AreEqual(c5, r5);
        }

        [TestMethod]
        public void TurnRolesIntoCombinationsCsvTest()
        {
            var i1 = new List<string> {"Administrator", "Moderator"};
            var r1 = Utilities.TurnRolesIntoCombinationsCsv(i1);

            Assert.IsNotNull(r1);
            Assert.IsTrue(r1.Count == 3);
            Assert.IsTrue(r1.Contains("Administrator"));
            Assert.IsTrue(r1.Contains("Moderator"));
            Assert.IsTrue(r1.Contains("Moderator;Administrator"));
        }

        [TestMethod]
        public void AreListContentsTheSameTest()
        {
            var a1 = new List<string> { "a" };
            var b1 = new List<string> { "a" };
            var r1 = Utilities.AreListContentsTheSame(a1, b1);
            Assert.IsTrue(r1);

            var a2 = new List<string> { "a" };
            var b2 = new List<string> { "b" };
            var r2 = Utilities.AreListContentsTheSame(a2, b2);
            Assert.IsFalse(r2);

            var a3 = new List<string> { "a" };
            var b3 = new List<string> { "a", "b" };
            var r3 = Utilities.AreListContentsTheSame(a3, b3);
            Assert.IsFalse(r3);

            var a4 = new List<string> { "a", "b" };
            var b4 = new List<string> { "a", "b" };
            var r4 = Utilities.AreListContentsTheSame(a4, b4);
            Assert.IsTrue(r4);

            var a5 = new List<string> { "b", "a" };
            var b5 = new List<string> { "a", "b" };
            var r5 = Utilities.AreListContentsTheSame(a5, b5);
            Assert.IsTrue(r5);

            var a6 = new List<string> { "b", "a", "c" };
            var b6 = new List<string> { "a", "b" };
            var r6 = Utilities.AreListContentsTheSame(a6, b6);
            Assert.IsFalse(r6);
        }

        [TestMethod]
        public void TrimListTest()
        {
            var i1 = new List<long>();
            Utilities.TrimList(i1, 1);
            Assert.IsNotNull(i1);
            Assert.IsTrue(i1.Count == 0);

            var i2 = new List<long> { 1 };
            Utilities.TrimList(i2, 1);
            Assert.IsNotNull(i2);
            Assert.IsTrue(i2.Count == 1);

            var i3 = new List<long> { 1, 2, 3, 4 };
            Utilities.TrimList(i3, 3);
            Assert.IsNotNull(i3);
            Assert.IsTrue(i3.Count == 3);

            var i4 = new List<long> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Utilities.TrimList(i4, 3);
            Assert.IsNotNull(i4);
            Assert.IsTrue(i4.Count == 3);

            var i5 = new List<long>();
            for (var i = 0; i < 3000; i++)
                i5.Add(i);
            Utilities.TrimList(i5, 100);
            Assert.IsNotNull(i5);
            Assert.IsTrue(i5.Count == 100);
        }

        [TestMethod]
        public void GetKilobytesTest()
        {
            const long i1 = 1000;
            var o1 = Utilities.GetKilobytes(i1);
            Assert.AreEqual(o1, 1);

            const long i2 = 2000;
            var o2 = Utilities.GetKilobytes(i2);
            Assert.AreEqual(o2, 2);

            const long i3 = 8000;
            var o3 = Utilities.GetKilobytes(i3);
            Assert.AreEqual(o3, 8);
        }

        [TestMethod]
        public void GetMegabytesTest()
        {
            const long i1 = 1000000;
            var o1 = Utilities.GetMegabytes(i1);
            Assert.AreEqual(o1, 1);

            const long i2 = 4000000;
            var o2 = Utilities.GetMegabytes(i2);
            Assert.AreEqual(o2, 4);

            const long i3 = 1000000000;
            var o3 = Utilities.GetMegabytes(i3);
            Assert.AreEqual(o3, 1000);
        }

        [TestMethod]
        public void GetGigabytesTest()
        {
            const long i1 = 1000000000;
            var o1 = Utilities.GetGigabytes(i1);
            Assert.AreEqual(o1, 1);

            const long i2 = 2000000000;
            var o2 = Utilities.GetGigabytes(i2);
            Assert.AreEqual(o2, 2);

            const long i3 = 10000000000;
            var o3 = Utilities.GetGigabytes(i3);
            Assert.AreEqual(o3, 10);
        }

        [TestMethod]
        public void GetTerabytesTest()
        {
            const long i1 = 1000000000000;
            var o1 = Utilities.GetTerabytes(i1);
            Assert.AreEqual(o1, 1);

            const long i2 = 2000000000000;
            var o2 = Utilities.GetTerabytes(i2);
            Assert.AreEqual(o2, 2);

            const long i3 = 10000000000000;
            var o3 = Utilities.GetTerabytes(i3);
            Assert.AreEqual(o3, 10);
        }

        [TestMethod]
        public void GetFileExtensionForContentTypeTest()
        {
            const string i1 = "image/jpeg";
            var o1 = Utilities.GetFileExtensionForContentType(i1);
            Assert.IsFalse(string.IsNullOrEmpty(o1));
            Assert.AreEqual(o1, ".jpg");

            const string i3 = "image/png";
            var o3 = Utilities.GetFileExtensionForContentType(i3);
            Assert.IsFalse(string.IsNullOrEmpty(o3));
            Assert.AreEqual(o3, ".png");

            const string i4 = "image/bmp";
            var o4 = Utilities.GetFileExtensionForContentType(i4);
            Assert.IsFalse(string.IsNullOrEmpty(o4));
            Assert.AreEqual(o4, ".bmp");

            try
            {
                Utilities.GetFileExtensionForContentType(null);
                Assert.Fail();
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }

            try
            {
                Utilities.GetFileExtensionForContentType(string.Empty);
                Assert.Fail();
            }
            catch (ArgumentNullException)
            {
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void GetAutoRotatedImageDimensionsTest()
        {
            using (var landscape = Image.FromFile(@"..\..\..\Assets\IMG_20150925_201851_1.JPG"))
            {
                var size = Utilities.GetAutoRotatedImageDimensions(landscape);
                Assert.IsNotNull(size);
                Assert.AreEqual(3264, size.Width);
                Assert.AreEqual(2448, size.Height);
            }

            using (var portrait = Image.FromFile(@"..\..\..\Assets\IMG_20150925_205134.JPG"))
            {
                var size = Utilities.GetAutoRotatedImageDimensions(portrait);
                Assert.IsNotNull(size);
                Assert.AreEqual(2448, size.Width);
                Assert.AreEqual(3264, size.Height);
            }
        }

        [TestMethod]
        public void IsMobileTest()
        {
            // mobile devices
            const string iPhone4 = "Mozilla/5.0 (iPhone; U; CPU iPhone OS 4_2_1 like Mac OS X; en-us) AppleWebKit/533.17.9 (KHTML, like Gecko) Version/5.0.2 Mobile/8C148 Safari/6533.18.5";
            Assert.IsTrue(Utilities.IsMobile(iPhone4), nameof(iPhone4) + " not detected as mobile");

            const string iPhone6Plus = "Mozilla/5.0 (iPhone; CPU iPhone OS 9_1 like Mac OS X) AppleWebKit/601.1.46 (KHTML, like Gecko) Version/9.0 Mobile/13B143 Safari/601.1";
            Assert.IsTrue(Utilities.IsMobile(iPhone6Plus), nameof(iPhone6Plus) + " not detected as mobile");

            const string iPad = "Mozilla/5.0 (iPad; CPU OS 9_1 like Mac OS X) AppleWebKit/601.1.46 (KHTML, like Gecko) Version/9.0 Mobile/13B137 Safari/601.1";
            Assert.IsTrue(Utilities.IsMobile(iPad), nameof(iPad) + " not detected as mobile");

            const string iPadMini = "Mozilla/5.0 (iPad; CPU OS 7_0_4 like Mac OS X) AppleWebKit/537.51.1 (KHTML, like Gecko) Version/7.0 Mobile/11B554a Safari/9537.53";
            Assert.IsTrue(Utilities.IsMobile(iPadMini), nameof(iPadMini) + " not detected as mobile");

            const string googleNexus7 = "Mozilla/5.0 (Linux; Android 4.3; Nexus 7 Build/JSS15Q) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.76 Safari/537.36";
            Assert.IsTrue(Utilities.IsMobile(googleNexus7), nameof(googleNexus7) + " not detected as mobile");

            const string nokiaN9 = "Mozilla/5.0 (MeeGo; NokiaN9) AppleWebKit/534.13 (KHTML, like Gecko) NokiaBrowser/8.5.0 Mobile Safari/534.13";
            Assert.IsTrue(Utilities.IsMobile(nokiaN9), nameof(nokiaN9) + " not detected as mobile");

            const string blackberryZ30 = "Mozilla/5.0 (BB10; Touch) AppleWebKit/537.10+ (KHTML, like Gecko) Version/10.0.9.2372 Mobile Safari/537.10+";
            Assert.IsTrue(Utilities.IsMobile(blackberryZ30), nameof(blackberryZ30) + " not detected as mobile");

            const string lgOptimusL70 = "Mozilla/5.0 (Linux; U; Android 4.4.2; en-us; LGMS323 Build/KOT49I.MS32310c) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/46.0.2490.76 Mobile Safari/537.36";
            Assert.IsTrue(Utilities.IsMobile(lgOptimusL70), nameof(lgOptimusL70) + " not detected as mobile");

            const string amazonKindleFireHdx = "Mozilla/5.0 (Linux; U; en-us; KFAPWI Build/JDQ39) AppleWebKit/535.19 (KHTML, like Gecko) Silk/3.13 Safari/535.19 Silk-Accelerated=true";
            Assert.IsTrue(Utilities.IsMobile(amazonKindleFireHdx), nameof(amazonKindleFireHdx) + " not detected as mobile");

            // desktop devices
            const string windows10Chrome = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.97 Safari/537.36";
            Assert.IsFalse(Utilities.IsMobile(windows10Chrome), nameof(windows10Chrome) + " not detected as windows 10 chrome");

            const string windows10Firefox = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:43.0) Gecko/20100101 Firefox/43.0";
            Assert.IsFalse(Utilities.IsMobile(windows10Firefox), nameof(windows10Firefox) + " not detected as windows 10 firefox");

            const string windows10Edge = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Safari/537.36 Edge/13.10586";
            Assert.IsFalse(Utilities.IsMobile(windows10Edge), nameof(windows10Edge) + " not detected as windows 10 edge");

            const string windows10InternetExplorer = "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko";
            Assert.IsFalse(Utilities.IsMobile(windows10InternetExplorer), nameof(windows10InternetExplorer) + " not detected as windows 10 internet explorer");
        }

        [TestMethod]
        public void FindPhotoCommentTest()
        {
            // single topic photo comment
            var t1 = new Post();
            var p1 = new Photo();
            var c1 = new PhotoComment {Id = 1, Text = "Awesome"};
            p1.Comments.Add(c1);
            t1.Photos.Add(p1);
            var o1 = Utilities.FindPhotoComment(t1, 1);
            Assert.IsNotNull(o1);
            Assert.AreEqual(o1.Text, "Awesome");

            // multiple topic photo comment
            var t2 = new Post();
            var p2 = new Photo();
            var c2V1 = new PhotoComment { Id = 1, Text = "Awesome1" };
            var c2V2 = new PhotoComment { Id = 2, Text = "Awesome2" };
            p2.Comments.Add(c2V1);
            p2.Comments.Add(c2V2);
            t2.Photos.Add(p2);
            var o2 = Utilities.FindPhotoComment(t2, 2);
            Assert.IsNotNull(o2);
            Assert.AreEqual(o2.Text, "Awesome2");

            // single nested topic photo comment
            var t3 = new Post();
            var p3 = new Photo();
            var c3V1 = new PhotoComment { Id = 1, Text = "Awesome1" };
            var c3V2 = new PhotoComment { Id = 2, Text = "Awesome2" };
            c3V1.ChildComments.Add(c3V2);
            p3.Comments.Add(c3V1);
            t3.Photos.Add(p3);
            var o3 = Utilities.FindPhotoComment(t3, 2);
            Assert.IsNotNull(o3);
            Assert.AreEqual(o3.Text, "Awesome2");

            // multiple nested topic photo comment
            var t4 = new Post();
            var p4 = new Photo();
            var c4V1 = new PhotoComment { Id = 1, Text = "Awesome1" };
            var c4V2 = new PhotoComment { Id = 2, Text = "Awesome2" };
            var c4V3 = new PhotoComment { Id = 3, Text = "Awesome3" };
            c4V1.ChildComments.Add(c4V2);
            c4V1.ChildComments.Add(c4V3);
            p4.Comments.Add(c4V1);
            t4.Photos.Add(p4);
            var o4 = Utilities.FindPhotoComment(t4, 3);
            Assert.IsNotNull(o4);
            Assert.AreEqual(o4.Text, "Awesome3");

            // single nested-nested topic photo comment
            var t5 = new Post();
            var p5 = new Photo();
            var c5V1 = new PhotoComment { Id = 1, Text = "Awesome1" };
            var c5V2 = new PhotoComment { Id = 2, Text = "Awesome2" };
            var c5V3 = new PhotoComment { Id = 3, Text = "Awesome3" };
            c5V2.ChildComments.Add(c5V3);
            c5V1.ChildComments.Add(c5V2);
            p5.Comments.Add(c5V1);
            t5.Photos.Add(p5);
            var o5 = Utilities.FindPhotoComment(t5, 3);
            Assert.IsNotNull(o5);
            Assert.AreEqual(o5.Text, "Awesome3");

            // single reply photo comment
            var t6 = new Post();
            var r6 = new Post();
            t6.Replies.Add(r6);
            var p6 = new Photo();
            var c6 = new PhotoComment { Id = 1, Text = "Awesome" };
            p6.Comments.Add(c6);
            r6.Photos.Add(p6);
            var o6 = Utilities.FindPhotoComment(t6, 1);
            Assert.IsNotNull(o6);
            Assert.AreEqual(o6.Text, "Awesome");

            // multiple reply photo comment
            var t7 = new Post();
            var r7 = new Post();
            t7.Replies.Add(r7);
            var p7 = new Photo();
            var c7V1 = new PhotoComment { Id = 1, Text = "Awesome1" };
            var c7V2 = new PhotoComment { Id = 2, Text = "Awesome2" };
            p7.Comments.Add(c7V1);
            p7.Comments.Add(c7V2);
            r7.Photos.Add(p7);
            var o7 = Utilities.FindPhotoComment(t7, 2);
            Assert.IsNotNull(o7);
            Assert.AreEqual(o7.Text, "Awesome2");

            // single nested reply photo comment
            var t8 = new Post();
            var r8 = new Post();
            t8.Replies.Add(r8);
            var p8 = new Photo();
            var c8V1 = new PhotoComment { Id = 1, Text = "Awesome1" };
            var c8V2 = new PhotoComment { Id = 2, Text = "Awesome2" };
            c8V1.ChildComments.Add(c8V2);
            p8.Comments.Add(c8V1);
            r8.Photos.Add(p8);
            var o8 = Utilities.FindPhotoComment(t8, 2);
            Assert.IsNotNull(o8);
            Assert.AreEqual(o8.Text, "Awesome2");

            // single nested-nested reply photo comment
            var t9 = new Post();
            var r9 = new Post();
            t9.Replies.Add(r9);
            var p9 = new Photo();
            var c9V1 = new PhotoComment { Id = 1, Text = "Awesome1" };
            var c9V2 = new PhotoComment { Id = 2, Text = "Awesome2" };
            var c9V3 = new PhotoComment { Id = 3, Text = "Awesome3" };
            c9V2.ChildComments.Add(c9V3);
            c9V1.ChildComments.Add(c9V2);
            p9.Comments.Add(c9V1);
            r9.Photos.Add(p9);
            var o9 = Utilities.FindPhotoComment(t9, 3);
            Assert.IsNotNull(o9);
            Assert.AreEqual(o9.Text, "Awesome3");
        }
    }
}