using System.Web;

namespace LBV6ForumApp
{
    internal static class Helpers
    {
        /// <summary>
        /// Remove HTML tags from string using char array. Fast!
        /// </summary>
        internal static string RemoveHtml(string source)
        {
            var array = new char[source.Length];
            var arrayIndex = 0;
            var inside = false;

            foreach (var @let in source)
            {
                if (@let == '<')
                {
                    inside = true;
                    continue;
                }

                if (@let == '>')
                {
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
    }
}