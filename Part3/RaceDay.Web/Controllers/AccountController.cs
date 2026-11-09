using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers
{
    /// <summary>Register, log in, log out and the profile page. All of it goes through the API.</summary>
    public class AccountController : AppController
    {
        public AccountController(ApiService api) : base(api) { }

        // ---------- Login ----------

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.IsLoggedIn()) return RedirectToHome();
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var response = await Api.LoginAsync(model.Email, model.Password);
            if (!response.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, response.IsUnauthorized
                    ? "Your email or password is incorrect."
                    : response.Message);
                return View(model);
            }

            var user = response.Data<LoginResultDto>();
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Something went wrong. Please try again.");
                return View(model);
            }

            // Remember who is logged in, for every page.
            HttpContext.Session.SetInt32(UserSessionExtensions.UserIdKey, user.UserID);
            HttpContext.Session.SetString(UserSessionExtensions.RoleKey, user.Role);
            HttpContext.Session.SetString(UserSessionExtensions.EmailKey, user.Email);
            HttpContext.Session.SetString(UserSessionExtensions.DisplayNameKey, user.Email);

            // Use the person's name in the menu bar.
            var profileResponse = await Api.GetAsync("api/users/me");
            var profile = profileResponse.IsSuccess ? profileResponse.Data<ProfileDto>() : null;
            if (profile is not null)
                HttpContext.Session.SetString(UserSessionExtensions.DisplayNameKey, DisplayNameFor(profile));

            Success("Welcome back!");
            return RedirectToHome();
        }

        // ---------- Register ----------

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.IsLoggedIn()) return RedirectToHome();
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Which extra fields are needed depends on the chosen role.
            if (model.Role == "Participant")
            {
                if (string.IsNullOrWhiteSpace(model.FirstName))
                    ModelState.AddModelError(nameof(model.FirstName), "Please enter your first name.");
                if (string.IsNullOrWhiteSpace(model.LastName))
                    ModelState.AddModelError(nameof(model.LastName), "Please enter your last name.");
                if (model.DateOfBirth is null)
                    ModelState.AddModelError(nameof(model.DateOfBirth), "Please enter your date of birth.");
                else if (model.DateOfBirth.Value.Date >= DateTime.Today)
                    ModelState.AddModelError(nameof(model.DateOfBirth), "Your date of birth must be in the past.");
            }
            else if (model.Role == "Organiser")
            {
                if (string.IsNullOrWhiteSpace(model.OrganisationName))
                    ModelState.AddModelError(nameof(model.OrganisationName), "Please enter your organisation name.");
            }
            else
            {
                ModelState.AddModelError(nameof(model.Role), "Please choose Participant or Organiser.");
            }

            if (!ModelState.IsValid) return View(model);

            var response = await Api.PostAsync("api/auth/register", new
            {
                email = model.Email,
                password = model.Password,
                role = model.Role,
                firstName = model.FirstName,
                lastName = model.LastName,
                dateOfBirth = model.DateOfBirth,
                phoneNumber = model.PhoneNumber,
                organisationName = model.OrganisationName,
                contactNumber = model.ContactNumber
            });

            if (!response.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View(model);
            }

            Success("Your account has been created. Please log in.");
            return RedirectToAction(nameof(Login));
        }

        // ---------- Logout ----------

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            if (HttpContext.Session.IsLoggedIn())
                await Api.PostAsync("api/auth/logout");

            Api.ForgetApiSession();
            HttpContext.Session.Clear();

            Success("You have been logged out.");
            return RedirectToAction(nameof(Login));
        }

        // ---------- Profile ----------

        [RequireLogin]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var response = await Api.GetAsync("api/users/me");
            if (SessionExpired(response, out var redirect)) return redirect;

            var profile = response.IsSuccess ? response.Data<ProfileDto>() : null;
            if (profile is null)
            {
                Failure(response.Message);
                return RedirectToAction("Index", "Home");
            }

            return View(ToViewModel(profile));
        }

        [RequireLogin]
        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var role = HttpContext.Session.GetRole();
            model.Role = role ?? string.Empty;
            model.Email = HttpContext.Session.GetString(UserSessionExtensions.EmailKey) ?? string.Empty;

            if (!ModelState.IsValid)
            {
                model.ProfilePictureUrl = await CurrentPictureUrl();
                return View(model);
            }

            object body = role == "Participant"
                ? new { firstName = model.FirstName, lastName = model.LastName, dateOfBirth = model.DateOfBirth, phoneNumber = model.PhoneNumber }
                : new { organisationName = model.OrganisationName, contactNumber = model.ContactNumber };

            var response = await Api.PutAsync("api/users/me", body);
            if (SessionExpired(response, out var redirect)) return redirect;

            if (!response.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                model.ProfilePictureUrl = await CurrentPictureUrl();
                return View(model);
            }

            var updated = response.Data<ProfileDto>();
            if (updated is not null)
                HttpContext.Session.SetString(UserSessionExtensions.DisplayNameKey, DisplayNameFor(updated));

            Success("Your profile has been updated.");
            return RedirectToAction(nameof(Profile));
        }

        [RequireLogin("Participant")]
        [HttpPost]
        public async Task<IActionResult> ProfilePicture(IFormFile? picture)
        {
            if (picture is null || picture.Length == 0)
            {
                Failure("Please choose an image to upload.");
                return RedirectToAction(nameof(Profile));
            }

            // The file goes to the API, which stores it in Azure Blob Storage.
            var response = await Api.PostFileAsync("api/users/me/profile-picture", picture);
            if (SessionExpired(response, out var redirect)) return redirect;

            if (response.IsSuccess) Success("Your profile picture has been updated.");
            else Failure(response.Message);

            return RedirectToAction(nameof(Profile));
        }

        // ---------- helpers ----------

        private IActionResult RedirectToHome()
        {
            return HttpContext.Session.GetRole() == "Organiser"
                ? RedirectToAction("Dashboard", "Organiser")
                : RedirectToAction("Index", "Home");
        }

        private async Task<string?> CurrentPictureUrl()
        {
            var response = await Api.GetAsync("api/users/me");
            return response.IsSuccess ? response.Data<ProfileDto>()?.ProfilePictureUrl : null;
        }

        private static string DisplayNameFor(ProfileDto profile)
        {
            if (profile.Role == "Participant" && !string.IsNullOrWhiteSpace(profile.FirstName))
                return profile.FirstName;
            if (!string.IsNullOrWhiteSpace(profile.OrganisationName))
                return profile.OrganisationName;
            return profile.Email;
        }

        private static ProfileViewModel ToViewModel(ProfileDto p) => new()
        {
            Email = p.Email,
            Role = p.Role,
            ProfilePictureUrl = p.ProfilePictureUrl,
            FirstName = p.FirstName,
            LastName = p.LastName,
            DateOfBirth = p.DateOfBirth,
            PhoneNumber = p.PhoneNumber,
            OrganisationName = p.OrganisationName,
            ContactNumber = p.ContactNumber
        };
    }
}
