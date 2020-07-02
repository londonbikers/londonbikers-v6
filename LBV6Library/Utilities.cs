using LBV6Library.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using MimeTypes;

namespace LBV6Library
{
    public static class Utilities
    {
        #region strings
        public static bool IsRichTextContent(string content)
        {
            return new Regex(@"<\s*([^ >]+)[^>]*>.*?<\s*/\s*\1\s*>").IsMatch(content);
        }

        public static string GetContentSynopsis(string content)
        {
            if (String.IsNullOrEmpty(content))
                return null;

            // take an arbitrary amount of any plain-text content
            if (!IsRichTextContent(content))
                return TrimNeatly(content, 100, "...");

            // rich-text post...
            // the opening content isn't a paragraph, i.e. it's an image, list, etc.
            // in this case we need to convert the content to text
            if (!content.StartsWith("<p>", StringComparison.InvariantCultureIgnoreCase))
                return TrimNeatly(RemoveHtml(content), 100) + "...";

            // return the first paragraph as this is text
            var para = content.Substring(0, content.IndexOf("</p>", StringComparison.Ordinal) + 4);
            if (para != content)
                para += "...";

            para = para.Replace("<p>", String.Empty).Replace("</p>", String.Empty);
            para = ConvertHtmlToText(para);
            return para;
        }

        /// <summary>
        /// Trims a piece of text to a maximum length if necessary without cutting a word off mid-way. Will err on the side of shortness rather than go over the limit.
        /// </summary>
        /// <param name="text">The text to trim.</param>
        /// <param name="maxLength">The maximum number of characters the text can be.</param>
        /// <param name="appendium">A piece of text that would be added to the end if a trim took place.</param>
        public static string TrimNeatly(string text, int maxLength, string appendium = null)
        {
            if (text.Length <= maxLength)
                return text;

            // perform initial trim
            var output = text.Substring(0, maxLength).Trim();

            // take back to the last space if there are some.
            // if there aren't, well that's going to look weird but it's better than returning an empty string.
            if (output.Contains(" "))
                output = text.Substring(0, output.LastIndexOf(' ')).Trim();

            if (!String.IsNullOrEmpty(appendium))
                output += appendium;

            return output;
        }

        /// <summary>
        /// Encodes text so that it can be used in URL components
        /// </summary>
        public static string EncodeText(string text)
        {
            var encode = Regex.Replace(text, @"\s", "-", RegexOptions.Compiled); // change space to hyphen
            encode = Regex.Replace(encode, @"[^a-zA-Z\d-]", String.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase); // only allow letters, numbers and hyphens
            encode = Regex.Replace(encode, @"-{2,}", "-", RegexOptions.Compiled | RegexOptions.IgnoreCase); // remove additional hyphens
            encode = Regex.Replace(encode, @"^[^a-z\d]*|[^a-z\d]*$", String.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase); // trim any non alpha-numerics from the start and end
            encode = encode.ToLower();
            return encode;
        }

        public static List<string> TurnRolesIntoCombinationsCsv(List<string> roles)
        {
            if (roles.Count == 1)
                return roles.GetRange(0, 1);

            // read the first role
            var role = roles.First();

            // apart from the last role send remaining roles for further processing
            var subsequentRoles = roles.GetRange(1, roles.Count - 1);
            var returnArray = TurnRolesIntoCombinationsCsv(subsequentRoles);

            // List to keep final string combinations
            var finalArray = returnArray.ToList();

            // add whatever is coming from the previous routine

            // take the last role
            finalArray.Add(role);

            // take the combination between the last role and the returning strings from the previous routine
            finalArray.AddRange(returnArray.Select(s => s + ";" + role));

            return finalArray;
        }

        /// <summary>
        /// Remove HTML tags from string using char array. Fast!
        /// </summary>
        public static string RemoveHtml(string source)
        {
            var array = new char[source.Length];
            var arrayIndex = 0;
            var inside = false;

            foreach (var @let in source)
            {
                switch (@let)
                {
                    case '<':
                        inside = true;
                        continue;
                    case '>':
                        inside = false;
                        continue;
                }

                if (inside) continue;
                array[arrayIndex] = @let;
                arrayIndex++;
            }

            var text = new string(array, 0, arrayIndex);
            text = HttpUtility.HtmlDecode(text);
            return text;
        }

        public static string ConvertHtmlToText(string html)
        {
            var text = html.Replace("&nbsp;", " ");
            text = text.Replace("&amp;", "&");
            return text;
        }
        #endregion

        #region numbers
        public static double GetKilobytes(long bytes)
        {
            return bytes / 1000D;
        }

        public static double GetMegabytes(long bytes)
        {
            return GetKilobytes(bytes) / 1000D;
        }

        public static double GetGigabytes(long bytes)
        {
            return GetMegabytes(bytes) / 1000D;
        }

        public static double GetTerabytes(long bytes)
        {
            return GetGigabytes(bytes) / 1000D;
        }
        #endregion

        #region legacy accommodations
        /// <summary>
        /// Converts URLs to LB-hosted media mentioned in post content to new URL's.
        /// </summary>
        public static string ConvertContentLegacyUrls(string content)
        {
            return String.IsNullOrEmpty(content)
                ? content
                : Regex.Replace(content, "http://londonbikers.com/forums/uploads/images/", "/_images/", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        #endregion

        #region files
        public static string GetPostAttachmentUrl(PostAttachment attachment)
        {
            return $"{ConfigurationManager.AppSettings["LB.Url"]}/_amedia/{attachment.Created.Year}/{attachment.Created.Month}/{attachment.Created.Day}/{attachment.Filename.ToLower()}";
        }

        public static string GetPostAttachmentPath(PostAttachment attachment)
        {
            return $"{ConfigurationManager.AppSettings["LB.MediaRoot"]}\\{attachment.Created.Year}\\{attachment.Created.Month}\\{attachment.Created.Day}\\{attachment.Filename.ToLower()}";
        }

        /// <summary>
        /// Converts a content type, i.e. 'image/jpeg' into a file extension, i.e. '.jpg'.
        /// </summary>
        public static string GetFileExtensionForContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));

            return MimeTypeMap.GetExtension(contentType);
        }

        /// <summary>
        /// Returns the absolute URL for a Photo object. Does not take into consideration scaling.
        /// </summary>
        public static string GetUrlForPhoto(Photo photo)
        {
            if (photo == null)
                throw new ArgumentNullException(nameof(photo));

            return $"{ConfigurationManager.AppSettings["LB.Url"]}/os/{GetFileStoreIdForPhoto(photo)}?zoom=1";
        }

        public static string GetFileStoreIdForPhoto(Photo photo)
        {
            if (photo == null)
                throw new ArgumentNullException(nameof(photo));

            return photo.Id + GetFileExtensionForContentType(photo.ContentType);
        }

        public static string GetFileStoreIdForCoverPhoto(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!user.CoverPhotoId.HasValue)
                return null;

            return user.CoverPhotoId + GetFileExtensionForContentType(user.CoverPhotoContentType);
        }

        public static string GetFileStoreIdForPrivateMessageAttachment(PrivateMessageAttachment attachment)
        {
            if (attachment == null)
                throw new ArgumentNullException(nameof(attachment));

            return attachment.FilestoreId + GetFileExtensionForContentType(attachment.ContentType);
        }

        /// <summary>
        /// Returns the file-store id for the users profile photo, if they have one.
        /// Clients will use this to construct an ImageResizer URL.
        /// </summary>
        public static string GetFileStoreIdForProfilePhoto(User user)
        {
            return !user.ProfilePhotoVersion.HasValue ? null : $"{user.Id}_{user.ProfilePhotoVersion.Value}.jpg";
        }
        #endregion

        #region collections
        /// <summary>
        /// Compares the contents of two string lists to see if they are the same. Order of items in either list doesn't matter.
        /// </summary>
        public static bool AreListContentsTheSame(List<string> a, List<string> b)
        {
            if (a.Count != b.Count)
                return false;

            a.Sort();
            b.Sort();

            return !a.Where((t, i) => !t.Equals(b[i], StringComparison.InvariantCultureIgnoreCase)).Any();
        }

        /// <summary>
        /// Ensures a list has only a specific maximum number of items by removing excess ones at the end.
        /// </summary>
        public static void TrimList(List<long> list, int maximumItems)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (maximumItems < 1)
                throw new ArgumentException("maximumItems cannot be less than one.");

            if (list.Count <= maximumItems)
                return;

            list.RemoveRange(maximumItems, list.Count - maximumItems);
        }

        /// <summary>
        /// Ensures a list has only a specific maximum number of items by removing excess ones at the end.
        /// </summary>
        public static void TrimList(List<KeyValuePair<long, bool>> list, int maximumItems)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (maximumItems < 1)
                throw new ArgumentException("maximumItems cannot be less than one.");

            if (list.Count <= maximumItems)
                return;

            list.RemoveRange(maximumItems, list.Count - maximumItems);
        }
        #endregion

        #region images
        public static Size GetAutoRotatedImageDimensions(Image image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            var size = new Size();
            if (Array.IndexOf(image.PropertyIdList, 274) > -1)
            {
                var orientation = (int)image.GetPropertyItem(274).Value[0];
                switch (orientation)
                {
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        size.Width = image.Height;
                        size.Height = image.Width;
                        break;
                    default:
                        // no rotation required
                        size.Width = image.Width;
                        size.Height = image.Height;
                        break;
                }

                return size;
            }

            // no orientation meta-data, so assume landscape
            Logging.LogInfo(typeof(Utilities).FullName, "No image meta-data found for orientation. Assuming landscape.");
            size.Width = image.Width;
            size.Height = image.Height;
            return size;
        }
        #endregion

        #region UI
        public static bool IsMobile(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return false;

            return Regex.IsMatch(userAgent, "iPhone|iPod|iPad|BlackBerry|BB10|Android|MeeGo|KFAPWI", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        #endregion

        #region photo comments
        /// <summary>
        /// Enumerates a topics photos and reply photos to look for a comment photo, no matter where it is in the hierarchy.
        /// </summary>
        /// <param name="topic">The topic to search photos and reply photos for the comment photo for.</param>
        /// <param name="photoCommentId">the id of the photo comment to search for.</param>
        public static PhotoComment FindPhotoComment(Post topic, long photoCommentId)
        {
            foreach (var photo in topic.Photos)
            {
                var topicPhotoResult = FindPhotoComment(photo.Comments, photoCommentId);
                if (topicPhotoResult != null)
                    return topicPhotoResult;
            }

            // ReSharper disable once LoopCanBeConvertedToQuery - unreadable
            foreach (var reply in topic.Replies)
            {
                foreach (var photo in reply.Photos)
                {
                    var topicPhotoResult = FindPhotoComment(photo.Comments, photoCommentId);
                    if (topicPhotoResult != null)
                        return topicPhotoResult;
                }
            }

            return null;
        }

        public static Photo FindPhoto(Post topic, Guid photoId)
        {
            var photo = topic.Photos.SingleOrDefault(q => q.Id.Equals(photoId));
            if (photo != null)
                return photo;

            foreach (var reply in topic.Replies)
            {
                photo = reply.Photos.SingleOrDefault(q => q.Id.Equals(photoId));
                if (photo != null)
                    return photo;
            }

            return null;
        }

        /// <summary>
        /// Enumerates a PhotoComment's child comments looking for a PhotoComment with a particular id.
        /// </summary>
        /// <param name="photoCommentsToSearch">The list of photo comments to search.</param>
        /// <param name="photoCommentIdToFind">The id of the photo comment to find.</param>
        /// <returns>Will return the PhotoComment for the specified id, or null if no PhotoComment was found.</returns>
        public static PhotoComment FindPhotoComment(IEnumerable<PhotoComment> photoCommentsToSearch, long photoCommentIdToFind)
        {
            foreach (var pc in photoCommentsToSearch)
            {
                if (pc.Id.Equals(photoCommentIdToFind))
                    return pc;

                if (pc.ChildComments.Any())
                    return FindPhotoComment(pc.ChildComments, photoCommentIdToFind);
            }

            // didn't find the photo comment in the hierarchy
            return null;
        }

        /// <summary>
        /// Enumerates the topic and reply photos and finds the photo the comment relates too.
        /// </summary>
        /// <param name="topic">The topic that the comment ultimately resides in.</param>
        /// <param name="photoCommentIdToFind">The id of the photo comment in question.</param>
        /// <returns>Will return the Photo for the PhotoComment, or null if no Photo was found.</returns>
        public static Photo FindPhotoForPhotoComment(Post topic, long photoCommentIdToFind)
        {
            foreach (var photo in topic.Photos)
            {
                var topicPhotoResult = FindPhotoComment(photo.Comments, photoCommentIdToFind);
                if (topicPhotoResult != null)
                    return photo;
            }

            foreach (var reply in topic.Replies)
            {
                foreach (var photo in reply.Photos)
                {
                    var replyPhotoResult = FindPhotoComment(photo.Comments, photoCommentIdToFind);
                    if (replyPhotoResult != null)
                        return photo;
                }
            }

            return null;
        }
        #endregion

        #region internal methods
        /// <summary>
        /// Retrieves the name of the method that called the current one.
        /// </summary>
        internal static string GetCallingMethodName()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(2);
            if (frame == null)
                return string.Empty;

            var method = frame.GetMethod();
            return method?.Name ?? string.Empty;
        }

        /// <summary>
        /// Retrieves the full namespace (including class) of the method that called the current one.
        /// </summary>
        internal static string GetCallingClassName()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(2);

            var method = frame?.GetMethod();
            if (method == null)
                return string.Empty;

            var declaringType = method.DeclaringType;
            return declaringType != null ? declaringType.FullName : string.Empty;
        }
        #endregion
    }
}