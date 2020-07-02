using LBV6.Models;
using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Models;
using LBV6Library.Models.Google;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace LBV6.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        #region members
        private ApplicationUserManager _userManager;
        private ApplicationSignInManager _signInManager;
        #endregion

        #region constructors
        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }
        #endregion

        #region accessors
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set { _signInManager = value; }
        }
        #endregion

        // GET: /account/sign-in
        [AllowAnonymous]
        [ActionName("sign-in")]
        public ActionResult Signin(string returnUrl)
        {
            // there's no point allowing authenticated users here.
            if (Request.IsAuthenticated)
                return RedirectToAction("index", "home");

            TrackPreAuthenticationPage();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /account/sign-in
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ActionName("sign-in")]
        public async Task<ActionResult> Signin(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            // find out if the username we've been supplied is the UserName or the Email address for an account on record.
            ApplicationUser applicationUser;
            var usernameAccount = await SignInManager.UserManager.FindByNameAsync(model.Username);
            if (usernameAccount != null)
            {
                applicationUser = usernameAccount;
            }
            else
            {
                var emailAccount = await SignInManager.UserManager.FindByEmailAsync(model.Username);
                if (emailAccount != null)
                {
                    applicationUser = emailAccount;
                }
                else
                {
                    ModelState.AddModelError("", @"Invalid username/email or password.");
                    Logging.LogDebug(GetType().FullName, "Invalid username or password.");
                    Helpers.Telemetry.TrackEvent("Local Login: Invalid username or password.", new Dictionary<string, string> { { "Username", model.Username } });
                    return View(model);
                }
            }

            // validate the user's status - are they authorised to sign-in?
            switch (applicationUser.Status)
            {
                case UserStatus.Banned:
                    Logging.LogInfo(GetType().FullName, "Local Login: Banned user: " + applicationUser.UserName);
                    ModelState.AddModelError("", @"Sorry, you're banned and cannot sign-in.");
                    Helpers.Telemetry.TrackEvent("Local Login: Banned user.", new Dictionary<string, string> { { "Username", model.Username } });
                    return RedirectToAction("CannotSignin", "Home");
                case UserStatus.Suspended:
                    Logging.LogInfo(GetType().FullName, "Local Login: Suspended user: " + applicationUser.UserName);
                    Helpers.Telemetry.TrackEvent("Local Login: Suspended user.", new Dictionary<string, string> { { "Username", model.Username } });
                    return RedirectToAction("CannotSignin", "Home");
                case UserStatus.Active:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!applicationUser.EmailConfirmed)
            {
                // legacy Apollo accounts were not email-confirmed so we need to ensure they are now
                // anyone signing in using a social account will get their email confirmed by that process.
                // when someone tries to sign-in locally who isn't email confirmed we should prompt the user they need confirming
                // and that if they haven't received a confirmation email, they can request another. this will handle the migrated accounts

                // store the user id in session for the resend-confirmation-link page to securely know the user id
                Session["confirmation_link_user"] = applicationUser;
                return View("sign-in-not-confirmed");
            }

            // good to try and sign-in now
            var result = await SignInManager.PasswordSignInAsync(applicationUser.UserName, model.Password, model.RememberMe, true);
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (result)
            {
                case SignInStatus.Success:
                    Logging.LogDebug(GetType().FullName, "SignInStatus.Success");
                    var user = ForumServer.Instance.Users.GetUser(applicationUser.Id);

                    // temporary debug code to try and nail down a session-set-up issue
                    if (user == null)
                        Logging.LogError(GetType().FullName, "Sign-in: Couldn't retrieve user with id: " + applicationUser.Id);
                    else
                        await Helpers.LogUserVisitAsync(user);

                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    Logging.LogInfo(GetType().FullName, "SignInStatus.LockedOut. User: " + applicationUser.UserName);
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    Logging.LogInfo(GetType().FullName, "SignInStatus.RequiresVerification. User: " + applicationUser.UserName);
                    return RedirectToAction("send-code", new { ReturnUrl = returnUrl, model.RememberMe });
                default:
                    Logging.LogInfo(GetType().FullName, "Invalid username/email or password. User: " + applicationUser.UserName);
                    ModelState.AddModelError("", @"Invalid username/email or password.");
                    return View(model);
            }
        }

        // GET: /account/verify-code
        [AllowAnonymous]
        [ActionName("verify-code")]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
                return View("Error");

            var user = await UserManager.FindByIdAsync(await SignInManager.GetVerifiedUserIdAsync());
            if (user != null)
            {
                // what is going on here? why is code not used??
                // ReSharper disable once UnusedVariable
                var code = await UserManager.GenerateTwoFactorTokenAsync(user.Id, provider);
            }

            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        // POST: /account/verify-code
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ActionName("verify-code")]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                default:
                    ModelState.AddModelError("", @"Invalid code.");
                    return View(model);
            }
        }

        // GET: /account/register
        [AllowAnonymous]
        public ActionResult Register()
        {
            // there's no point allowing authenticated users here.
            if (Request.IsAuthenticated)
                return RedirectToAction("index", "home");

            return View();
        }

        // POST: /account/register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                // extended username validation
                var userNameValidationResult = await ForumServer.Instance.Users.IsUserNameValidAsync(model.Username);
                if (userNameValidationResult != UserNameValidationResult.Valid)
                {
                    string invalidUserNameReason = null;
                    switch (userNameValidationResult)
                    {
                        case UserNameValidationResult.InvalidCharacters:
                            invalidUserNameReason = "Sorry, username must contain letters, numbers, spaces or dashes only.";
                            break;
                        case UserNameValidationResult.InvalidTooLong:
                            invalidUserNameReason = "Sorry, usernames cannot be longer than " + ConfigurationManager.AppSettings["LB.Usernames.MaxLength"] + " characters long.";
                            break;
                        case UserNameValidationResult.InvalidTooShort:
                            invalidUserNameReason = "Sorry, usernames have to be " + ConfigurationManager.AppSettings["LB.Usernames.MinLength"] + " or more characters long.";
                            break;
                        case UserNameValidationResult.InvalidAlreadyInUse:
                            invalidUserNameReason = "Sorry, that username is already in use.";
                            break;
                        case UserNameValidationResult.InvalidReservedWord:
                            invalidUserNameReason = "Sorry, that username contains prohibited words";
                            break;
                        case UserNameValidationResult.Valid:
                            break;
                        default:
                            invalidUserNameReason = "Sorry, please choose another username.";
                            break;
                    }

                    ModelState.AddModelError("", invalidUserNameReason);
                    Helpers.Telemetry.TrackEvent("Register: Invalid username", new Dictionary<string, string> { { "Username", model.Username } });
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Username.Trim(),
                    Email = model.Email
                };

                #region StopForumSpam check
                if (bool.Parse(ConfigurationManager.AppSettings["LB.EnableRegistrationForumSpamCheck"]))
                {
                    if (Helpers.IsUserBlackListed(Request.UserHostAddress, user.Email))
                    {
                        Logging.LogInfo(GetType().FullName, "Black-listed registration attempt. Email: " + user.Email);
                        ModelState.AddModelError("", @"Sorry, we cannot register you, you're black-listed.");
                        Helpers.Telemetry.TrackEvent("Register: StopForumSpam Blacklisted IP", new Dictionary<string, string> { { "Username", user.UserName }, { "Email", model.Email }, { "IP", Request.UserHostAddress } });
                        return View(model);
                    }
                }
                #endregion

                //if (bool.Parse(ConfigurationManager.AppSettings["LB.EnableIpBanCheck"]) && await ForumServer.Instance.Users.IsUserBannedByIpAsync(Request.UserHostAddress))
                //{
                //    ModelState.AddModelError("", @"Sorry, we cannot register you, you're banned or suspended already.");
                //    Helpers.Telemetry.TrackEvent("Register: Banned or Suspended by IP", new Dictionary<string, string> { { "Username", model.Username }, { "Email", model.Email }, { "IP", Request.UserHostAddress } });
                //    return View(model);
                //}

                #region username validation
                // perform some basic validation on usernames so we don't end up with rubbish names.
                // ideally this would be done on the client side in real-time in JS!
                if (!Regex.IsMatch(user.UserName, "^[0-9a-zA-Z -]{3,}$"))
                {
                    Logging.LogInfo(GetType().FullName, "Registration failure: Username too short: UserName: " + user.UserName);
                    ModelState.AddModelError("", @"Usernames must be three characters or more and can only contain letters, numbers, spaces or hyphens, sorry.");
                    return View(model);
                }

                if (ConfigurationManager.AppSettings["LB.ProhibitedUsernames"] != null)
                {
                    var prohibitedUsernames = ConfigurationManager.AppSettings["LB.ProhibitedUsernames"].Split(',').ToList();
                    if (prohibitedUsernames.Any(q => q.Equals(user.UserName, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        Logging.LogInfo(GetType().FullName, "Registration failure: Username in prohibited list: UserName: " + user.UserName);
                        ModelState.AddModelError("", $@"Sorry, '{user.UserName}' cannot be used as a username.");
                        return View(model);
                    }
                }
                #endregion

                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // let the forum server know about the new user
                    await ForumServer.Instance.Users.NotifyOfNewUserAsync(user.Id);

                    var code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var scheme = Request.Url?.Scheme ?? "http";
                    var callbackUrl = Url.Action("confirm-email", "account", new { userId = user.Id, code }, scheme);
                    var emailParams = new List<object> { user.UserName, callbackUrl }.ToArray();

                    // send an email with the email confirmation link on a background thread
                    BackgroundTaskScheduler.QueueBackgroundWorkItem(async ctx => await ForumServer.Instance.Emails.SendTemplatedEmailAsync(EmailTemplate.RegistrationEmailConfirmationRequired, user.Email, emailParams));

                    Helpers.Telemetry.TrackEvent("Local registration", new Dictionary<string, string> { { "Username", user.UserName } });
                    return View("register-awaiting-confirmation");
                }

                AddErrors(result);

                // If we got this far, something failed, redisplay form
                return View(model);
            }
            catch (Exception ex)
            {
                Logging.LogError(GetType().FullName, ex);
                Helpers.Telemetry.TrackException(ex);
                throw;
            }
        }

        // GET: /account/resend-confirmation-link
        [AllowAnonymous]
        [ActionName("resend-confirmation-link")]
        public async Task<ActionResult> ResendConfirmationLink()
        {
            if (Session["confirmation_link_user"] == null)
                return View("sign-in-not-confirmed-error");

            var user = (ApplicationUser)Session["confirmation_link_user"];
            var code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
            var scheme = Request.Url?.Scheme ?? "http";
            var callbackUrl = Url.Action("confirm-email", "account", new { userId = user.Id, code }, scheme);
            var emailParams = new List<object> { user.UserName, callbackUrl }.ToArray();

            // send an email with the email confirmation link
            // run on a background thread so we don't slow the UX
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ctx => await ForumServer.Instance.Emails.SendTemplatedEmailAsync(EmailTemplate.EmailConfirmationRequired, user.Email, emailParams));

            return View("Email-Confirmation-Link-Sent");
        }

        // GET: /account/confirm-email
        [AllowAnonymous]
        [ActionName("confirm-email")]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
                return View("Error");

            var result = await UserManager.ConfirmEmailAsync(userId, code);
            if (!result.Succeeded)
                return View("Error");

            var user = await UserManager.FindByIdAsync(userId);
            if (user.WelcomeEmailSent)
                return View();

            // welcome emails are only sent once they confirm their email address during registration.
            // send an email to the user welcoming them to LB; run on a background thread so we don't impact the UX
            var emailParams = new List<object> { user.UserName }.ToArray();
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ctx => await ForumServer.Instance.Emails.SendTemplatedEmailAsync(EmailTemplate.Welcome, user.Email, emailParams));

            user.WelcomeEmailSent = true;
            await UserManager.UpdateAsync(user);
            await ForumServer.Instance.Users.NotifyOfUpdatedUserAsync(user.Id);

            if (Session["confirmation_link_user"] != null)
                Session.Remove("confirmation_link_user");

            return View();
        }

        // GET: /account/forgot-password
        [AllowAnonymous]
        [ActionName("forgot-password")]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /account/forgot-password
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ActionName("forgot-password")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await UserManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                // don't reveal that the user does not exist as this information could aid hack attempts
                Logging.LogInfo(GetType().FullName, "User not found for password reset: " + model.Email);
                return View("forgot-password-confirmation");
            }

            if (!user.EmailConfirmed && user.Created > DateTime.Parse(ConfigurationManager.AppSettings["LB.V6GoLiveDate"]))
            {
                // users created before V6 went live can do password reset without their email address being confirmed.
                // this is necessary to allow people to sign-in manually for the first time.
                Logging.LogInfo(GetType().FullName, "User's email address is not validated for password reset: " + model.Email);
                return View("forgot-password-confirmation");
            }

            // validate the user's status - are they authorised to reset their password?
            switch (user.Status)
            {
                case UserStatus.Banned:
                    Logging.LogInfo(GetType().FullName, "ForgotPassword: Banned user. Username: " + user.UserName);
                    Helpers.Telemetry.TrackEvent("ForgotPassword: Banned user.", new Dictionary<string, string> { { "Username", user.UserName } });
                    return View("forgot-password-confirmation");
                case UserStatus.Suspended:
                    Logging.LogInfo(GetType().FullName, "ForgotPassword: Suspended user. Username: " + user.UserName);
                    Helpers.Telemetry.TrackEvent("ForgotPassword: Suspended user.", new Dictionary<string, string> { { "Username", user.UserName } });
                    return View("forgot-password-confirmation");
                case UserStatus.Active:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // send an email with this link
            var code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
            var scheme = Request.Url?.Scheme ?? "http";
            var callbackUrl = Url.Action("reset-password", "account", new { userId = user.Id, code }, scheme);
            var emailParams = new List<object> { user.UserName, callbackUrl }.ToArray();
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ctx => await ForumServer.Instance.Emails.SendTemplatedEmailAsync(EmailTemplate.ResetPasswordLink, user.Email, emailParams));

            Logging.LogInfo(GetType().FullName, "Sent password reset link to: " + user.Email);
            return RedirectToAction("forgot-password-confirmation", "account");
        }

        // GET: /account/forgot-password-confirmation
        [AllowAnonymous]
        [ActionName("forgot-password-confirmation")]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /account/reset-password
        [AllowAnonymous]
        [ActionName("reset-password")]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        // POST: /account/reset-password
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ActionName("reset-password")]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // don't reveal that the user does not exist as this information could aid hack attempts
                Logging.LogInfo(GetType().FullName, "User not found when attempting password reset: " + model.Email);
                return RedirectToAction("reset-password-confirmation", "account");
            }

            if (!user.EmailConfirmed && user.Created < DateTime.Parse(ConfigurationManager.AppSettings["LB.V6GoLiveDate"]))
            {
                // users created before V6 went live can do password reset without their email address being confirmed.
                // this is necessary to allow people to sign-in manually for the first time.
                // now that they've followed a link from their email address we can mark the email as confirmed.
                Logging.LogInfo(GetType().FullName, "Marking a pre-V6 user's email address as confirmed: " + model.Email);
                user.EmailConfirmed = true;
                UserManager.Update(user);
            }

            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                Helpers.Telemetry.TrackEvent("Password reset", new Dictionary<string, string> { { "Username", user.UserName } });
                return RedirectToAction("reset-password-confirmation", "account");
            }

            AddErrors(result);
            return View();
        }

        // GET: /account/reset-password-confirmation
        [AllowAnonymous]
        [ActionName("reset-password-confirmation")]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // GET: /account/send-code
        [AllowAnonymous]
        [ActionName("send-code")]
        public async Task<ActionResult> SendCode(bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
                return View("Error");

            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, RememberMe = rememberMe });
        }

        // POST: /account/send-code
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ActionName("send-code")]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
                return View();

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
                return View("Error");

            return RedirectToAction("verify-code", new { Provider = model.SelectedProvider, model.RememberMe });
        }

        // POST: /account/external-login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ActionName("external-login")]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            try
            {
                TrackPreAuthenticationPage();

                // Request a redirect to the external login provider
                return new ChallengeResult(provider, Url.Action("external-login-callback", "account", new { ReturnUrl = returnUrl }));
            }
            catch (Exception ex)
            {
                Logging.LogError(GetType().FullName, ex);
                Helpers.Telemetry.TrackException(ex);
                throw;
            }
        }

        // GET: /account/external-login-callback
        [AllowAnonymous]
        [ActionName("external-login-callback")]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            try
            {
                var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (loginInfo == null)
                {
                    Logging.LogWarning(GetType().FullName, "No login info supplied! URL called was: " + Request.Url);
                    return RedirectToAction("sign-in");
                }

                #region verify if existing user can sign-in
                ApplicationUser applicationUser = null;
                if (!string.IsNullOrEmpty(loginInfo.Email))
                {
                    var emailAccount = await SignInManager.UserManager.FindByEmailAsync(loginInfo.Email);
                    if (emailAccount != null)
                        applicationUser = emailAccount;
                }
                else if (!string.IsNullOrEmpty(loginInfo.DefaultUserName))
                {
                    var usernameAccount = await SignInManager.UserManager.FindByNameAsync(loginInfo.DefaultUserName);
                    if (usernameAccount != null)
                        applicationUser = usernameAccount;
                }

                if (applicationUser != null)
                {
                    //  are they authorised to sign-in?
                    switch (applicationUser.Status)
                    {
                        case UserStatus.Banned:
                            Logging.LogInfo(GetType().FullName, "Banned user. Username: " + applicationUser.UserName);
                            Helpers.Telemetry.TrackEvent("ExternaLogin: Banned user.", new Dictionary<string, string> { { "Username", applicationUser.UserName }, { "Id", applicationUser.Id } });
                            return RedirectToAction("CannotSignin", "Home");
                        case UserStatus.Suspended:
                            Logging.LogInfo(GetType().FullName, "Suspended user. Username: " + applicationUser.UserName);
                            Helpers.Telemetry.TrackEvent("ExternaLogin: Suspended user.", new Dictionary<string, string> { { "Username", applicationUser.UserName }, { "Id", applicationUser.Id } });
                            return RedirectToAction("CannotSignin", "Home");
                        case UserStatus.Active:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                #endregion

                #region existing user, existing external login
                var loginResult = await SignInExternalLogin(loginInfo);
                if (loginResult != null)
                {
                    Logging.LogDebug(GetType().FullName, "Existing external login found for user, logging in: " + loginInfo.Email);
                    Helpers.Telemetry.TrackEvent("External Login: Existing external login", new Dictionary<string, string> { { "Email", loginInfo.Email } });

                    var domainUser = await ForumServer.Instance.Users.GetUserByEmailAsync(loginInfo.Email);

                    // temporary debug code to try and nail down a session-set-up issue
                    if (domainUser == null)
                        Logging.LogError(GetType().FullName, "Couldn't retrieve user via GetUserByEmailAsync() with id: " + loginInfo.Email);
                    else
                        await Helpers.LogUserVisitAsync(domainUser);
                    
                    return loginResult;
                }
                #endregion

                #region existing user, new external login used
                // so we've not found an existing user who has used an external login with us before.
                // let's try and match the new external login with an existing user to see if they're logging in with a new external login
                // i.e. they've logged in with Facebook before, but now they're logging in with Google.
                // many websites fail to do this and the experience is infuriating for a user.
                Logging.LogDebug(GetType().FullName, "No existing login for an existing user found, checking if we have an email from the provider...");
                if (!string.IsNullOrEmpty(loginInfo.Email))
                {
                    var emailUser = await UserManager.FindByEmailAsync(loginInfo.Email);
                    if (emailUser != null)
                    {
                        // the external login email matches an existing user.
                        // add this login to our user.
                        Logging.LogDebug(GetType().FullName, "Found an existing user with that email address.");
                        var linkingResult = await UserManager.AddLoginAsync(emailUser.Id, loginInfo.Login);
                        if (!linkingResult.Succeeded)
                        {
                            Logging.LogDebug(GetType().FullName, $"Couldn't link a new external login for {loginInfo.Login.LoginProvider} to user: {emailUser.UserName}");
                            return RedirectAfterAuthentication();
                        }

                        Logging.LogDebug(GetType().FullName, "About to try and add the new login claim to our existing user...");
                        await StoreAuthTokenClaims(emailUser);
                        Helpers.Telemetry.TrackEvent("External Login: New external login for existing user", new Dictionary<string, string> { { "Username", emailUser.UserName } });

                        #region update profile data
                        // if we have missing information in our user profile then we can try and supplant it from the external site.
                        Logging.LogDebug(GetType().FullName, "About to try and fill out our user from the provider...");
                        var domainUser = await ForumServer.Instance.Users.GetUserAsync(emailUser.Id);

                        // temporary debug code to try and nail down a session-set-up issue
                        if (domainUser == null)
                            Logging.LogError(GetType().FullName, "Couldn't retrieve user via GetUserAsync() with emailUser.Id: " + emailUser.Id);

                        if (loginInfo.Login.LoginProvider.Equals("Facebook", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var accessToken = loginInfo.ExternalIdentity.Claims.Single(q => q.Type.Equals("urn:facebook:access_token", StringComparison.InvariantCultureIgnoreCase)).Value;
                            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ctx => await ForumServer.Instance.Users.UpdateUserFromFacebookAsync(domainUser, accessToken));
                        }
                        else if (loginInfo.Login.LoginProvider.Equals("GooglePlus", StringComparison.InvariantCultureIgnoreCase))
                        {
                            // yes, that typo is correct. don't change it!
                            var accessToken = loginInfo.ExternalIdentity.Claims.Single(q => q.Type.Equals("urn:tokens:gooleplus:accesstoken", StringComparison.InvariantCultureIgnoreCase)).Value;
                            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ctx => await ForumServer.Instance.Users.UpdateUserFromGoogleProfileAsync(domainUser, accessToken));
                        }
                        #endregion

                        if (!emailUser.EmailConfirmed)
                        {
                            emailUser.EmailConfirmed = true;
                            await UserManager.UpdateAsync(emailUser);
                        }

                        // notify the forum server of the update to the user
                        await ForumServer.Instance.Users.NotifyOfUpdatedUserAsync(emailUser.Id);

                        Logging.LogDebug(GetType().FullName, "About to sign the user in...");
                        Helpers.SetEventMessage("Got it!", "We've added that login to your account.", Helpers.EventMessageType.Success);
                        var newExternalLoginResult = await SignInExternalLogin(loginInfo);
                        if (newExternalLoginResult != null)
                        {
                            await Helpers.LogUserVisitAsync(domainUser);
                            return newExternalLoginResult;
                        }
                    }
                }
                #endregion

                #region check new external user
                // at this point we're looking at a new user registering via an external identity provider
                // so we need to make sure they're not a banned or suspended user trying to create a new account

                // this seems to be generating more false positives than I'd like, so disabling it for now
                //if (await ForumServer.Instance.Users.IsUserBannedByIpAsync(Request.UserHostAddress))
                //{
                //    Helpers.Telemetry.TrackEvent("ExternalLoginCallback: External-Register Attempt - IP.", new Dictionary<string, string> { { "Email", loginInfo.Email }, { "DefaultUserName", loginInfo.DefaultUserName }, { "ProviderKey", loginInfo.Login.ProviderKey } });
                //    return RedirectToAction("CannotSignin", "Home");
                //}
                if (await ForumServer.Instance.Users.IsUserBannedByProviderKey(loginInfo.Login.ProviderKey))
                {
                    Helpers.Telemetry.TrackEvent("External-Register Attempt - ProviderKey.", new Dictionary<string, string> { { "Email", loginInfo.Email }, { "DefaultUserName", loginInfo.DefaultUserName }, { "ProviderKey", loginInfo.Login.ProviderKey } });
                    return RedirectToAction("CannotSignin", "Home");
                }
                #endregion
                
                ViewBag.LoginProvider = loginInfo.Login.LoginProvider;

                #region process new Facebook user
                // show the final registration info collection page for Facebook
                if (loginInfo.Login.LoginProvider.Equals("Facebook", StringComparison.InvariantCultureIgnoreCase))
                    return View("external-login-confirmation-googlefacebook", new ExternalLoginConfirmationViewModel { UserName = loginInfo.DefaultUserName });
                #endregion

                #region process new Google user
                // show the final registration info collection page for Google
                if (!loginInfo.Login.LoginProvider.Equals("GooglePlus", StringComparison.InvariantCultureIgnoreCase))
                    return View("Error");

                var suggestedUsername = loginInfo.DefaultUserName;
                var googleAccessTokenClaim = loginInfo.ExternalIdentity.Claims.SingleOrDefault(q => q.Type.Equals("urn:tokens:gooleplus:accesstoken", StringComparison.InvariantCultureIgnoreCase));
                if (googleAccessTokenClaim == null)
                {
                    Logging.LogDebug(GetType().FullName, "Didn't get an access token for a GooglePlus user: " + loginInfo.Email);
                }
                else
                {
                    var profile = await ForumServer.Instance.Users.GetUserProfileFromGoogleAsync(googleAccessTokenClaim.Value);
                    if (profile == null)
                        return View("external-login-confirmation-googlefacebook", new ExternalLoginConfirmationViewModel { UserName = suggestedUsername });

                    // keep the profile aside for use when we create the user but for now we're just interested in whether
                    // or not they have a nickname that would be a better suggestion for a username.
                    Session["GooglePlusUserProfile"] = profile;
                    if (!string.IsNullOrEmpty(profile.Nickname))
                        suggestedUsername = profile.Nickname;
                }

                return View("external-login-confirmation-googlefacebook", new ExternalLoginConfirmationViewModel { UserName = suggestedUsername });
                #endregion

                // if we got this far something's wrong, could be an unsupported provider.
            }
            catch (Exception ex)
            {
                Helpers.Telemetry.TrackException(ex);
                throw;
            }
        }

        // POST: /account/external-login-confirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ActionName("external-login-confirmation")]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                    return RedirectToAction("index", "manage");

                // extended username validation
                var userNameValidationResult = await ForumServer.Instance.Users.IsUserNameValidAsync(model.UserName);
                if (userNameValidationResult != UserNameValidationResult.Valid)
                {
                    string invalidUserNameReason = null;
                    switch (userNameValidationResult)
                    {
                        case UserNameValidationResult.InvalidCharacters:
                            invalidUserNameReason = "Sorry, username must contain letters, numbers, spaces or dashes only.";
                            break;
                        case UserNameValidationResult.InvalidTooLong:
                            invalidUserNameReason = "Sorry, usernames cannot be longer than " + ConfigurationManager.AppSettings["LB.Usernames.MaxLength"] + " characters long.";
                            break;
                        case UserNameValidationResult.InvalidTooShort:
                            invalidUserNameReason = "Sorry, usernames have to be " + ConfigurationManager.AppSettings["LB.Usernames.MinLength"] + " or more characters long.";
                            break;
                        case UserNameValidationResult.InvalidAlreadyInUse:
                            invalidUserNameReason = "Sorry, that username is already in use.";
                            break;
                        case UserNameValidationResult.InvalidReservedWord:
                            invalidUserNameReason = "Sorry, that username contains prohibited words";
                            break;
                        case UserNameValidationResult.Valid:
                            break;
                        default:
                            invalidUserNameReason = "Sorry, please choose another username.";
                            break;
                    }

                    ModelState.AddModelError("", invalidUserNameReason);
                    Helpers.Telemetry.TrackEvent("ExternalLoginConfirmation: Invalid username", new Dictionary<string, string> { { "Username", model.UserName } });
                    return View(model);
                }

                if (!ModelState.IsValid)
                    return View(model);

                // get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                    return View("external-login-failure");

                if (!info.Login.LoginProvider.Equals("GooglePlus", StringComparison.InvariantCultureIgnoreCase) && !info.Login.LoginProvider.Equals("Facebook", StringComparison.InvariantCultureIgnoreCase))
                    return View("Error");

                // providers supply different types and amounts of data. 
                // we want to collect as much domain relevant data as possible from each provider.
                var user = new ApplicationUser { UserName = model.UserName, Email = info.Email, EmailConfirmed = true };

                // perform some basic validation on usernames so we don't end up with rubbish names.
                // ideally this would be done on the client side in real-time in JS!
                if (!Regex.IsMatch(user.UserName, "^[0-9a-zA-Z -]{3,}$"))
                {
                    Logging.LogInfo(GetType().FullName, "External Sign-in Registration: Username invalid: " + user.UserName);
                    ModelState.AddModelError("", @"Usernames must be three characters or more and can only contain letters, numbers, spaces or hyphens, sorry.");
                    return View("External-Login-Confirmation-GoogleFacebook", model);
                }

                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        // notify the forum server of the new user
                        await ForumServer.Instance.Users.NotifyOfNewUserAsync(user.Id);

                        await StoreAuthTokenClaims(user);
                        await SignInExternalLogin(info);
                        Helpers.Telemetry.TrackEvent("External Login: New user", new Dictionary<string, string> { { "Username", user.UserName } });

                        Helpers.SetEventMessage("Hey", "Welcome to londonbikers - check your email!", Helpers.EventMessageType.Success);
                        var domainUser = await ForumServer.Instance.Users.GetUserAsync(user.Id);

                        // temporary debug code to try and nail down a session-set-up issue
                        if (domainUser == null)
                            Logging.LogError(GetType().FullName, "Couldn't retrieve user via GetUserAsync() with user.Id: " + user.Id);

                        await Helpers.LogUserVisitAsync(domainUser);

                        #region Facebook
                        if (info.Login.LoginProvider.Equals("Facebook", StringComparison.InvariantCultureIgnoreCase))
                        {
                            // now add the user data to our user we have requested from Facebook previously during the sign-up workflow
                            // run on a background thread
                            var accessToken = info.ExternalIdentity.Claims.Single(q => q.Type.Equals("urn:facebook:access_token", StringComparison.InvariantCultureIgnoreCase)).Value;
                            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ctx => await ForumServer.Instance.Users.UpdateUserFromFacebookAsync(domainUser, accessToken));
                        }
                        #endregion

                        #region Google
                        if (!info.Login.LoginProvider.Equals("GooglePlus", StringComparison.InvariantCultureIgnoreCase))
                            return RedirectAfterAuthentication();

                        if (Session["GooglePlusUserProfile"] is GooglePlusUserProfile profile)
                        {
                            // now add the user data to our user we have requested from Google previously during the sign-up workflow
                            // run on a background thread
                            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ctx => await ForumServer.Instance.Users.UpdateUserFromGoogleProfileAsync(domainUser, profile));
                        }
                        else
                        {
                            Logging.LogDebug(GetType().FullName, "Expected a Google plus user profile after user create for user: " + user.Email);
                        }
                        #endregion

                        return RedirectAfterAuthentication();
                    }
                }

                AddErrors(result);

                // if we got this far then something went wrong.
                return View("External-Login-Confirmation-GoogleFacebook");
            }
            catch (Exception ex)
            {
                Logging.LogError(GetType().FullName, ex);
                Helpers.Telemetry.TrackException(ex);
                throw;
            }
        }

        // POST: /account/sign-out
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        [ActionName("sign-out")]
        public ActionResult SignOut()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            Session.Abandon();
            return RedirectToAction("index", "home");
        }

        // GET: /account/external-login-failure
        [AllowAnonymous]
        [ActionName("external-login-failure")]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("index", "home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri) : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                    properties.Dictionary[XsrfKey] = UserId;

                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        /// <summary>
        /// Keeps a track of where the user was on the site before they authenticated.
        /// </summary>
        private void TrackPreAuthenticationPage()
        {
            // is there a referrer and is it local?
            if (!string.IsNullOrEmpty(Request?.UrlReferrer?.Host) && !string.IsNullOrEmpty(Request.UrlReferrer.AbsoluteUri) && ConfigurationManager.AppSettings["LB.Domain"].StartsWith(Request.UrlReferrer.Host))
                Session["PreAuthenticationPage"] = Request.UrlReferrer.AbsoluteUri;
        }

        /// <summary>
        /// Returns the user to the site page they were at before they authenticated.
        /// </summary>
        private ActionResult RedirectAfterAuthentication()
        {
            var preAuthorisationPage = (string)Session["PreAuthenticationPage"];
            if (string.IsNullOrEmpty(preAuthorisationPage))
                return RedirectToAction("index", "home");

            Session.Remove("PreAuthenticationPage");
            return Redirect(preAuthorisationPage);
        }

        /// <summary>
        /// Stores the external identity provider authentication token as a claim against the user.
        /// From the GooglePlusAuthProvider package, extended to support Facebook too.
        /// </summary>
        private async Task StoreAuthTokenClaims(IUser<string> user)
        {
            // Get the claims identity
            var claimsIdentity = await AuthenticationManager.GetExternalIdentityAsync(DefaultAuthenticationTypes.ExternalCookie);
            if (claimsIdentity != null)
            {
                // Retrieve the existing claims
                var currentClaims = await UserManager.GetClaimsAsync(user.Id);

                // Get the list of access token related claims from the identity
                var tokenClaims = claimsIdentity.Claims.Where(c => c.Type.StartsWith("urn:tokens:", StringComparison.InvariantCultureIgnoreCase) || c.Type.Equals("urn:facebook:access_token", StringComparison.InvariantCultureIgnoreCase));

                // Save the access token related claims
                foreach (var tokenClaim in tokenClaims.Where(tokenClaim => !currentClaims.Contains(tokenClaim)))
                    await UserManager.AddClaimAsync(user.Id, tokenClaim);
            }
        }

        private async Task<ActionResult> SignInExternalLogin(ExternalLoginInfo externalLoginInfo)
        {
            var result = await SignInManager.ExternalSignInAsync(externalLoginInfo, isPersistent: true);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectAfterAuthentication();
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("send-code", new { RememberMe = false });
                case SignInStatus.Failure:
                    // failure means we don't have an external login, so can be expected behaviour
                    return null;
                default:
                    return null;
            }
        }
        #endregion
    }
}