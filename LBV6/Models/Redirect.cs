namespace LBV6.Models
{
    public struct Redirect
    {
        public RedirectType Type { get; set; }
        public string Url { get; set; }
    }

    public enum RedirectType
    {
        Temporary,
        Permanent
    }
}