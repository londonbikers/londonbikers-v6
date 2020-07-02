using System;

namespace LBV6Library.Exceptions
{
    public class NotAuthorisedException : Exception
    {
        public NotAuthorisedException(string message) : base(message)
        {
        }
    }
}