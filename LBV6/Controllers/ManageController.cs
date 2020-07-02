using LBV6.Models;
using LBV6ForumApp;
using LBV6Library;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace LBV6.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        #region constructors
        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }
        #endregion

        #region accessors and members
        private ApplicationUserManager _userManager;
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
        #endregion

        // GET: /manage/index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                //PhoneNumber = await UserManager.GetPhoneNumberAsync(User.Identity.GetUserId()),
                //TwoFactor = await UserManager.GetTwoFactorEnabledAsync(User.Identity.GetUserId()),
                Logins = await UserManager.GetLoginsAsync(User.Identity.GetUserId()),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(User.Identity.GetUserId())
            };

            // extension: add the current user as a json payload for client-side code to use
            var user = await ForumServer.Instance.Users.GetUserAsync(User.Identity.GetUserId());
            model.Payload = JsonConvert.SerializeObject(Transformations.ConvertUserToUserProfileDto(user));

            return View(model);
        }

        // WTF - there's no such view
        // GET: /Manage/RemoveLogin
        public ActionResult RemoveLogin()
        {
            var linkedAccounts = UserManager.GetLogins(User.Identity.GetUserId());
            ViewBag.ShowRemoveButton = HasPassword() || linkedAccounts.Count > 1;
            return View(linkedAccounts);
        }

        // POST: /manage/remove-login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("remove-login")]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInAsync(user, isPersistent: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("manage-logins", new { Message = message });
        }

        // GET: /manage/add-phone-number
        [ActionName("add-phone-number")]
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        // POST: /manage/add-phone-number
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("add-phone-number")]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService == null)
                return RedirectToAction("verify-phone-number", new { PhoneNumber = model.Number });

            var message = new IdentityMessage
            {
                Destination = model.Number,
                Body = "Your security code is: " + code
            };

            await UserManager.SmsService.SendAsync(message);
            return RedirectToAction("verify-phone-number", new { PhoneNumber = model.Number });
        }

        // POST: /manage/enable-two-factor-authentication
        [HttpPost]
        [ActionName("enable-two-factor-authentication")]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
                await SignInAsync(user, isPersistent: false);
            return RedirectToAction("index", "manage");
        }

        // POST: /manage/disable-two-factor-authentication
        [HttpPost]
        [ActionName("disable-two-factor-authentication")]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
                await SignInAsync(user, isPersistent: false);
            return RedirectToAction("index", "manage");
        }

        // GET: /manage/verify-phone-number
        [ActionName("verify-phone-number")]
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        // POST: /manage/verify-phone-number
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("verify-phone-number")]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user == null)
                    return RedirectToAction("index", new { Message = ManageMessageId.AddPhoneSuccess });

                await ForumServer.Instance.Users.NotifyOfUpdatedUserAsync(user.Id);
                await SignInAsync(user, isPersistent: false);
                return RedirectToAction("index", new { Message = ManageMessageId.AddPhoneSuccess });
            }

            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", @"Failed to verify phone");
            return View(model);
        }

        // GET: /manage/remove-phone-number
        [ActionName("remove-phone-number")]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
                return RedirectToAction("index", new { Message = ManageMessageId.Error });

            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
                return RedirectToAction("index", new { Message = ManageMessageId.RemovePhoneSuccess });

            await ForumServer.Instance.Users.NotifyOfUpdatedUserAsync(user.Id);
            await SignInAsync(user, isPersistent: false);
            return RedirectToAction("index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        // GET: /manage/change-password
        [ActionName("change-password")]
        public ActionResult ChangePassword()
        {
            return View();
        }

        // POST: /manage/change-password
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("change-password")]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                    await SignInAsync(user, isPersistent: false);

                return RedirectToAction("index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }

            AddErrors(result);
            return View(model);
        }

        // GET: /manage/set-password
        [ActionName("set-password")]
        public ActionResult SetPassword()
        {
            return View();
        }

        // POST: /manage/set-password
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("set-password")]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                    await SignInAsync(user, isPersistent: false);
                return RedirectToAction("index", new { Message = ManageMessageId.SetPasswordSuccess });
            }
            AddErrors(result);

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: /manage/manage-logins
        [ActionName("manage-logins")]
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";

            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
                return View("Error");

            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        // POST: /manage/link-login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("link-login")]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("link-login-callback", "manage"), User.Identity.GetUserId());
        }

        // GET: /manage/link-login-callback
        [ActionName("link-login-callback")]
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
                return RedirectToAction("manage-logins", new { Message = ManageMessageId.Error });

            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            if (!result.Succeeded)
                return RedirectToAction("manage-logins", new { Message = ManageMessageId.Error });

            await ForumServer.Instance.Users.NotifyOfUpdatedUserAsync(User.Identity.GetUserId());
            return RedirectToAction("manage-logins");
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ExternalCookie, DefaultAuthenticationTypes.TwoFactorCookie);
            AuthenticationManager.SignIn(new AuthenticationProperties { IsPersistent = isPersistent }, await user.GenerateUserIdentityAsync(UserManager));
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error);
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            return user?.PasswordHash != null;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            return user?.PhoneNumber != null;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }
        #endregion
    }
}