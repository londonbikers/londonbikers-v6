using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace LBV6
{
    public class WhitespaceModule : IHttpModule
    {
        #region IHttpModule Members
        void IHttpModule.Dispose()
        {
            // Nothing to dispose; 
        }

        void IHttpModule.Init(HttpApplication context)
        {
            context.BeginRequest += ContextBeginRequest;
        }
        #endregion

        static void ContextBeginRequest(object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            if (app == null) return;
            if (app.Request.AcceptTypes == null) return;
            if (app.Request.AcceptTypes.Contains("text/html"))
                app.Response.Filter = new WhitespaceFilter(app.Response.Filter);
        }

        #region Stream filter
        private class WhitespaceFilter : Stream
        {
            public WhitespaceFilter(Stream sink)
            {
                _sink = sink;
            }

            private readonly Stream _sink;
            private static readonly Regex Reg = new Regex(@"(?<=[^])\t{2,}|(?<=[>])\s{2,}(?=[<])|(?<=[>])\s{2,11}(?=[<])|(?=[\n])\s{2,}");

            #region Properites
            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return true; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override void Flush()
            {
                _sink.Flush();
            }

            public override long Length
            {
                get { return 0; }
            }

            public override long Position { get; set; }
            #endregion

            #region Methods
            public override int Read(byte[] buffer, int offset, int count)
            {
                return _sink.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _sink.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _sink.SetLength(value);
            }

            public override void Close()
            {
                _sink.Close();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                var data = new byte[count];
                Buffer.BlockCopy(buffer, offset, data, 0, count);
                var html = System.Text.Encoding.Default.GetString(buffer);
                html = Reg.Replace(html, string.Empty);
                var outdata = System.Text.Encoding.Default.GetBytes(html);
                _sink.Write(outdata, 0, outdata.GetLength(0));
            }
            #endregion
        }
        #endregion
    }
}