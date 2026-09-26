namespace POMS.Web.Models;

public class SiteSettings
{
    public string SiteName { get; set; } = "Cricket Auction Manager";
    public string? LogoUrl { get; set; } = "";
    public string FooterAddress { get; set; } = "123 Cricket Stadium Road, Dhaka, Bangladesh";
    public string ContactEmail { get; set; } = "admin@bcl.elink.bd";
    public string ContactPhone { get; set; } = "+880 1234 567890";
    public string MapEmbedUrl { get; set; } = "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d116834.00977789467!2d90.33728815190835!3d23.780636450000003!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x3755b8b087026b81%3A0x8fa563bbdd5904c2!2sDhaka%2C%20Bangladesh!5e0!3m2!1sen!2sus!4v1714578120612!5m2!1sen!2sus";
    public string HeroTitle { get; set; } = "Build your dream squad.";
    public string HeroSubtitle { get; set; } = "Master every bid.";
    public List<SliderSlide> Slides { get; set; } = new();
}

public class SliderSlide
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ImageUrl { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
}
