using AutoPhotoUploader.Services;
using Microsoft.Extensions.Configuration;
using AutoPhotoUploader.Models;

var configProvider = new ConfigurationBuilder()
    .SetBasePath(Directory
    .GetCurrentDirectory())
    .AddJsonFile($"appsettings.json");

var config = configProvider.Build();

FlickrService flickr = new FlickrService(config.GetSection("flickrKey").Value, config.GetSection("flickrSecret").Value);

FlickrUserModel flickrUser = new FlickrUserModel();

(string, string)? OAuthTokenAndSecret = await flickr.GetUserOathTokenSecret();

if(OAuthTokenAndSecret != null)
{
    flickrUser.OAuthToken = OAuthTokenAndSecret.Value.Item1;
    flickrUser.OAuthTokenSecret = OAuthTokenAndSecret.Value.Item2;
    flickrUser.OAuthVerifier = flickr.GetUserAuthorization(flickrUser.OAuthToken, config.GetSection("flickrEmail").Value, config.GetSection("flickrPassword").Value);

    //Request access oauthtoken and oauthtokensecret
    (string, string)? accessOAuthTokens = await flickr.GetOAuthAccessToken(flickrUser);


   if(accessOAuthTokens is not null)
    {
        flickrUser.OAuthToken = accessOAuthTokens.Value.Item1;
        flickrUser.OAuthTokenSecret = accessOAuthTokens.Value.Item2;
    } else
    {
        Console.WriteLine("Problem authenticating Flickr User, check your appsettings.json and ensure you've properly set your Consumer Key, Secret, Email, and Password. Terminating...");
        Environment.Exit(1);
    }

} else
{
    Console.WriteLine("Problem authenticating Flickr User, check your appsettings.json and ensure you've properly set your Consumer Key, Secret, Email, and Password. Terminating...");
    Environment.Exit(1);
}



