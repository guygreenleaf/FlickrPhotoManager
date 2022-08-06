using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoPhotoUploader.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace AutoPhotoUploader.Services
{
    public interface IFlickrService
    {
        Task<(string, string)?> GetUserOathTokenSecret();
        string? GetUserAuthorization(string? OAuthToken, string? email, string? password);

        void pageLoadWaiter(WebDriverWait wait, IWebDriver driver, string waitValue, int waitType, int sleepTime);

        Task<(string, string)?> GetOAuthAccessToken(FlickrUserModel flickrUser);
    }
}
