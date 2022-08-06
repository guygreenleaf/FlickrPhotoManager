using System.Security.Cryptography;
using System.Text;
using AutoPhotoUploader.Models;
using AutoPhotoUploader.Utils;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace AutoPhotoUploader.Services
{
    public class FlickrService : IFlickrService
    {

        public static string? ConsumerKey;
        public static string? Secret;
        public static string? RequestToken = "";

        public FlickrService(string consumerApiKey, string consumerApiSecret)
        {
            ConsumerKey = consumerApiKey;
            Secret = consumerApiSecret;
        }

        public async Task<(string, string)?> GetUserOathTokenSecret()
        {
            string requestString = "http://www.flickr.com/services/oauth/request_token";

            //generate a random nonce and a timestamp
            Random rand = new Random();
            string nonce = rand.Next(999999).ToString();
            string timestamp = GetTimestamp();

            //create the parameter string in alphabetical order
            string parameters = "oauth_callback=" + UrlHelper.Encode("http://www.example.com");
            parameters += "&oauth_consumer_key=" + ConsumerKey;
            parameters += "&oauth_nonce=" + nonce;
            parameters += "&oauth_signature_method=HMAC-SHA1";
            parameters += "&oauth_timestamp=" + timestamp;
            parameters += "&oauth_version=1.0";

            //generate a signature base on the current requeststring and parameters
            string signature = generateSignature("GET", requestString, parameters);

            //add the parameters and signature to the requeststring
            string url = requestString + "?" + parameters + "&oauth_signature=" + signature;

            //test the request
            HttpClient web = new HttpClient();
            string res = string.Empty;
            using (HttpResponseMessage response = await web.GetAsync(url))
            {
                using (HttpContent content = response.Content)
                {
                    res = content.ReadAsStringAsync().Result;
                }
            }

            if (!string.IsNullOrEmpty(res))
            {
                if ( res.Contains("oauth_callback_confirmed=true") && res.Contains("oauth_token") && res.Contains("oauth_token_secret"))
                {
                    int indexOauthStart = res.IndexOf("oauth_token");
                    int indexOAuthSecretStart = res.IndexOf("oauth_token_secret");

                    //jump to after equals
                    string oAuthToken = res.Substring(indexOauthStart, (res.Length - 1) - indexOauthStart);
                    oAuthToken = oAuthToken.Substring(oAuthToken.IndexOf('=')+1, (oAuthToken.IndexOf('&') - (oAuthToken.IndexOf('=')+1)));

                    string oAuthTokenSecret = res.Substring(indexOAuthSecretStart, (res.Length) - indexOAuthSecretStart);
                    oAuthTokenSecret = oAuthTokenSecret.Substring(oAuthTokenSecret.IndexOf("=") + 1);

                    if(!string.IsNullOrEmpty(oAuthToken) && !string.IsNullOrEmpty(oAuthTokenSecret))
                    {
                        return (oAuthToken, oAuthTokenSecret);
                    } else
                    {
                        //Problem parsing oauth token and token secret
                        return null;
                    }
                } else
                {
                    //problem with response
                    return null;
                }
            }
            else
            {
                //problem with response
                return null;
            }
        }


        private static string generateSignature(string httpMethod, string ApiEndpoint, string parameters)
        {
            //url encode the API endpoint and the parameters 

            //IMPORTANT NOTE:
            //encoded text should contain uppercase characters: '=' => %3D !!! (not %3d )
            //the HtmlUtility.UrlEncode creates lowercase encoded tags!
            //Here I use a urlencode class by Ian Hopkins
            string encodedUrl = UrlHelper.Encode(ApiEndpoint);
            string encodedParameters = UrlHelper.Encode(parameters);

            //generate the basestring
            string basestring = httpMethod + "&" + encodedUrl + "&";
            parameters = UrlHelper.Encode(parameters);
            basestring = basestring + parameters;

            //hmac-sha1 encryption:

            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

            //create key (request_token can be an empty string)
            string key = Secret + "&" + RequestToken;
            byte[] keyByte = encoding.GetBytes(key);

            //create message to encrypt
            byte[] messageBytes = encoding.GetBytes(basestring);

            //encrypt message using hmac-sha1 with the provided key
            HMACSHA1 hmacsha1 = new HMACSHA1(keyByte);
            byte[] hashmessage = hmacsha1.ComputeHash(messageBytes);

            //signature is the base64 format for the genarated hmac-sha1 hash
            string signature = System.Convert.ToBase64String(hashmessage);

            //encode the signature to make it url safe and return the encoded url
            return UrlHelper.Encode(signature);

        }


        //generator of unix epoch time
        public static string GetTimestamp()
        {
            int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            return epoch.ToString();


        }


        public string? GetUserAuthorization(string? OAuthToken, string? email, string? password)
        {
            string url = $"https://www.flickr.com/services/oauth/authorize?oauth_token={OAuthToken}";

            IWebDriver driver = new ChromeDriver();
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            driver.Navigate().GoToUrl(url);

            pageLoadWaiter(wait, driver, "login-email", 1, 2000);
            var emailField = driver.FindElement(By.Id("login-email"));           
            emailField.SendKeys(email);

            var nextButton = driver.FindElement(By.TagName("button"));
            nextButton.Click();

            pageLoadWaiter(wait, driver, "login-password", 1, 2000);
            var pwField = driver.FindElement(By.Id("login-password"));
            pwField.SendKeys(password);

            var loginButton = driver.FindElement(By.TagName("button"));
            loginButton.Click();

            pageLoadWaiter(wait, driver, @"input[value*=""OK, I'LL AUTHORIZE IT""", 2, 4000);
            var authButton = driver.FindElement(By.CssSelector(@"input[value*=""OK, I'LL AUTHORIZE IT"""));

            authButton.Click();

            string responseURL = driver.Url;

            driver.Quit();

            if (!string.IsNullOrEmpty(responseURL) && responseURL.Contains("oauth_verifier="))
            {
                int indexOAuthVerifyStart = responseURL.IndexOf("oauth_verifier=")+15;
                return responseURL.Substring(indexOAuthVerifyStart);
            }
            else
            {
                return null;
            }
        }


        public void pageLoadWaiter(WebDriverWait wait, IWebDriver driver, string waitValue, int waitType, int sleepTime)
        {
            Thread.Sleep(sleepTime);
            switch (waitType)
            {
                case 1:   
                    wait.Until(driver => driver.FindElement(By.Id(waitValue)));
                    break;

                case 2:
                    wait.Until(driver => driver.FindElement(By.CssSelector(waitValue)));
                    break;
                    
                default:
                    break;
            }
        }


        private static string generateSignatureAccessToken(string httpMethod, string ApiEndpoint, string parameters, string? tokenSecret)
        {
            //url encode the API endpoint and the parameters 

            //IMPORTANT NOTE:
            //encoded text should contain uppercase characters: '=' => %3D !!! (not %3d )
            //the HtmlUtility.UrlEncode creates lowercase encoded tags!
            //Here I use a urlencode class by Ian Hopkins
            string encodedUrl = UrlHelper.Encode(ApiEndpoint);
            string encodedParameters = UrlHelper.Encode(parameters);

            //generate the basestring
            string basestring = httpMethod + "&" + encodedUrl + "&";
            parameters = UrlHelper.Encode(parameters);
            basestring = basestring + parameters;

            //hmac-sha1 encryption:

            ASCIIEncoding encoding = new ASCIIEncoding();

            //create key (request_token can be an empty string)
            string key = Secret + "&" + tokenSecret;
            byte[] keyByte = encoding.GetBytes(key);

            //create message to encrypt
            byte[] messageBytes = encoding.GetBytes(basestring);

            //encrypt message using hmac-sha1 with the provided key
            HMACSHA1 hmacsha1 = new HMACSHA1(keyByte);
            byte[] hashmessage = hmacsha1.ComputeHash(messageBytes);

            //signature is the base64 format for the genarated hmac-sha1 hash
            string signature = System.Convert.ToBase64String(hashmessage);

            //encode the signature to make it url safe and return the encoded url
            return UrlHelper.Encode(signature);
        }


        public async Task<(string, string)?> GetOAuthAccessToken(FlickrUserModel flickrUser)
        {
            string requestString = "https://www.flickr.com/services/oauth/access_token";

            //generate a random nonce and a timestamp
            Random rand = new Random();
            string nonce = rand.Next(999999).ToString();
            string timestamp = GetTimestamp();

            //create the parameter string in alphabetical order
            string parameters = "oauth_consumer_key=" + ConsumerKey;
            parameters += "&oauth_nonce=" + nonce;
            parameters += "&oauth_signature_method=HMAC-SHA1";
            parameters += "&oauth_timestamp=" + timestamp;
            parameters += "&oauth_token=" + flickrUser.OAuthToken;
            parameters += "&oauth_verifier=" + flickrUser.OAuthVerifier;
            parameters += "&oauth_version=1.0";

            //generate a signature base on the current requeststring and parameters
            string signature = generateSignatureAccessToken("GET", requestString, parameters, flickrUser.OAuthTokenSecret);

            //add the parameters and signature to the requeststring
            string url = requestString + "?" + parameters + "&oauth_signature=" + signature;
       
            HttpClient web = new HttpClient();

            using (HttpResponseMessage response = await web.GetAsync(url))
            {
                using (HttpContent content = response.Content)
                {
                    string res = content.ReadAsStringAsync().Result;

                    if (!string.IsNullOrEmpty(res) && res.Contains("oauth_token") && res.Contains("oauth_token_secret"))
                    {
                        int indexOauthStart = res.IndexOf("oauth_token");
                        int indexOAuthSecretStart = res.IndexOf("oauth_token_secret");

                        string oAuthToken = res.Substring(indexOauthStart, (res.Length - 1) - indexOauthStart);
                        oAuthToken = oAuthToken.Substring(oAuthToken.IndexOf('=') + 1, (oAuthToken.IndexOf('&') - (oAuthToken.IndexOf('=') + 1)));

                        string oAuthTokenSecret = res.Substring(indexOAuthSecretStart, (res.Length) - indexOAuthSecretStart);
                        int indexEndSecret = oAuthTokenSecret.IndexOf('&');

                        oAuthTokenSecret = oAuthTokenSecret.Substring(19, 16);

                        return (oAuthToken, oAuthTokenSecret);
                    } else
                    {
                        //Problem retrieving access token and secret
                        return null;
                    }
                }
            }
        }
    }
}
