using System;

namespace LBV6Library.Exceptions
{
    public class PhotoTooSmallException : Exception
    {
        public PhotoTooSmallException(string message) : base(message)
        {
        }
    }
}