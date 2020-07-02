using LBV6.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Facebook;
using Owin;
using Owin.Security.Providers.GooglePlus;
using Owin.Security.Providers.GooglePlus.Provider;
using System;
using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LBV6
{
    public partial class Startup
    {
        #region members
        public const string GooglePlusAccessTokenClaimType = "urn:tokens:gooleplus:accesstoken";
        #endregion

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Configure the database context, user manager and sign-in manager to use a single instance per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/account/sign-in"),
                Provider = new CookieAuthenticationProvider
                {
                    // Enables the application to validate the security stamp when the user logs in.
                    // This is a security feature which is used when you change a password or add an external login to your account.  
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(TimeSpan.FromMinutes(30), (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Enables the application to temporarily store user information when they are verifying the second factor in the two-factor authentication process.
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Enables the application to remember the second login verification factor such as phone or email.
            // Once you check this option, your second step of verification during the login process will be remembered on the device where you logged in from.
            // This is similar to the RememberMe option when you log in.
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            #region Google authentication
            var googleOptions = new GooglePlusAuthenticationOptions
            {
                Caption = "Google+",
                ClientId = ConfigurationManager.AppSettings["Google.ClientId"],
                ClientSecret = ConfigurationManager.AppSettings["Google.ClientSecret"],
                Provider = new GooglePlusAuthenticationProvider { OnAuthenticated = OnAuthenticatedAsync },
                RequestOfflineAccess = true
            };
            googleOptions.Scope.Add("profile");
            googleOptions.Scope.Add("https://www.googleapis.com/auth/plus.login");
            app.UseGooglePlusAuthentication(googleOptions);
            #endregion

            #region Facebook authentication
            var facebookOptions = new FacebookAuthenticationOptions
            {
                AppId = ConfigurationManager.AppSettings["Facebook.AppId"],
                AppSecret = ConfigurationManager.AppSettings["Facebook.Secret"],
                BackchannelHttpHandler = new FacebookBackChannelHandler(),
                UserInformationEndpoint = "https://graph.facebook.com/v2.8/me?fields=id,name,email,first_name,last_name",
                Provider = new FacebookAuthenticationProvider
                {
                    OnAuthenticated = (context) =>
                    {
                        const string xmlSchemaString = "http://www.w3.org/2001/XMLSchema#string";
                        context.Identity.AddClaim(new Claim("urn:facebook:access_token", context.AccessToken, xmlSchemaString, "Facebook"));
                        context.Identity.AddClaim(new Claim("urn:facebook:email", context.Email, xmlSchemaString, "Facebook"));
                        return Task.FromResult(0);
                    }
                }
            };

            facebookOptions.Scope.Add("email");
            facebookOptions.Scope.Add("public_profile");
            //facebookOptions.Scope.Add("user_friends");
            //facebookOptions.Scope.Add("publish_actions");
            app.UseFacebookAuthentication(facebookOptions);
            #endregion
        }

        private static async Task OnAuthenticatedAsync(GooglePlusAuthenticatedContext context)
        {
            await Task.Run(() =>
            {
                context.Identity.AddClaim(new Claim(GooglePlusAccessTokenClaimType, context.AccessToken));
            });
        }
    }
}