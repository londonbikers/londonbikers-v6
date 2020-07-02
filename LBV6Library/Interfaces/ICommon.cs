using System;

namespace LBV6Library.Interfaces
{
    internal interface ICommon
    {
        long Id { get; set; }
        DateTime Created { get; set; }
    }
}