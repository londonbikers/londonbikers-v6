namespace LBV6Library.Models.Dtos
{
    /// <summary>
    /// DTO that the client can use to link to prominent users within a thread.
    /// i.e people who contribute the most or who have the most likes for their posts.
    /// </summary>
    public class ProminentUserDto
    {
        public string UserName { get; set; }
        public string ProfileFileStoreId { get; set; }
        public string Reason { get; set; }
    }
}