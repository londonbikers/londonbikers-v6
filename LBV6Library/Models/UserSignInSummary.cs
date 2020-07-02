using LBV6Library.Interfaces;
using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace LBV6Library.Models
{
    public class UserSignInSummary : TableEntity, INotCachable
    {
        #region constructors
        public UserSignInSummary(string ipAddress, string userId)
        {
            PartitionKey = ipAddress;
            RowKey = userId;
            LastSignIn = DateTime.Now;
            Count = 1;
        }

        public UserSignInSummary() { }
        #endregion

        /// <summary>
        /// The number of times the user has signed-in from this IP address
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// The last time the user signed-in from this IP address
        /// </summary>
        public DateTime LastSignIn { get; set; }
    }
}