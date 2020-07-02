using System.Collections.Generic;

namespace LBV6Library.Models.Google
{
    public class Email
    {
        public string Value { get; set; }
        public string Type { get; set; }
    }

    public class Url
    {
        public string Value { get; set; }
        public string Type { get; set; }
        public string Label { get; set; }
    }

    public class Name
    {
        public string Formatted { get; set; }
        public string FamilyName { get; set; }
        public string GivenName { get; set; }
        public string MiddleName { get; set; }
        public string HonorificPrefix { get; set; }
        public string HonorificSuffix { get; set; }
    }

    public class Image
    {
        public string Url { get; set; }
    }

    public class Organization
    {
        public string Name { get; set; }
        public string Department { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public bool Primary { get; set; }
    }

    public class PlacesLived
    {
        public string Value { get; set; }
        public bool Primary { get; set; }
    }

    public class AgeRange
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class CoverPhoto
    {
        public string Url { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }

    public class CoverInfo
    {
        public int TopImageOffset { get; set; }
        public int LeftImageOffset { get; set; }
    }

    public class Cover
    {
        public string Layout { get; set; }
        public CoverPhoto CoverPhoto { get; set; }
        public CoverInfo CoverInfo { get; set; }
    }

    public class GooglePlusUserProfile
    {
        public string Kind { get; set; }
        public string Etag { get; set; }
        public string Nickname { get; set; }
        public string Occupation { get; set; }
        public string Skills { get; set; }
        public string Birthday { get; set; }
        public string Gender { get; set; }
        public List<Email> Emails { get; set; }
        public List<Url> Urls { get; set; }
        public string ObjectType { get; set; }
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public Name Name { get; set; }
        public string Tagline { get; set; }
        public string BraggingRights { get; set; }
        public string AboutMe { get; set; }
        public string CurrentLocation { get; set; }
        public string RelationshipStatus { get; set; }
        public string Url { get; set; }
        public Image Image { get; set; }
        public List<Organization> Organizations { get; set; }
        public List<PlacesLived> PlacesLived { get; set; }
        public bool IsPlusUser { get; set; }
        public string Language { get; set; }
        public AgeRange AgeRange { get; set; }
        public int PlusOneCount { get; set; }
        public int CircledByCount { get; set; }
        public bool Verified { get; set; }
        public Cover Cover { get; set; }
        public string Domain { get; set; }
    }
}
