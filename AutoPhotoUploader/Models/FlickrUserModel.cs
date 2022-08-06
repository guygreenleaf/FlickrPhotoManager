namespace AutoPhotoUploader.Models
{
    public class FlickrUserModel
    {
        public string? OAuthToken { get; set; }
        public string? OAuthTokenSecret { get; set; }
        public string? OAuthVerifier { get; set; }
        public string? OAuthSignature { get; set; }

        public string? OAuthAccessToken { get; set; }
        public string? UserID { get; set; }
        public string? Username { get; set; }
    }
}
