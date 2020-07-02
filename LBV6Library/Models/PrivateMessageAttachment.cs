using LBV6Library.Interfaces;
using System;

namespace LBV6Library.Models
{
    /// <inheritdoc cref="ICommon" />
    /// <inheritdoc cref="INotCachable" />
    /// <summary>
    /// A legacy object brought over from the old InstantForum software. 
    /// PM's don't support attachments any longer, or at least, not yet.
    /// </summary>
    public class PrivateMessageAttachment : ICommon, INotCachable
    {
        public long Id { get; set; }
        public PrivateMessage PrivateMessage { get; set; }
        public DateTime Created { get; set; }
        public string Filename { get; set; }
        public string ContentType { get; set; }
        public int? LegacyId { get; set; }

        /// <summary>
        /// The unique identifier for the file in the cloud file storage system.
        /// </summary>
        public Guid? FilestoreId { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
    }
}