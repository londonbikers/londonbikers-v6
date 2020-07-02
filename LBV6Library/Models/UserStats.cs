namespace LBV6Library.Models
{
    public class UserStats
    {
        public int TotalUsers { get; set; }
        public int EnabledUsers { get; set; }
        public int SuspendedUsers { get; set; }
        public int BannedUsers { get; set; }
        /// <summary>
        /// How many users have their email address confirmed.
        /// </summary>
        public int ConfirmedUsers { get; set; }
        public int FacebookLogins { get; set; }
        public int GoogleLogins { get; set; }
    }
}
