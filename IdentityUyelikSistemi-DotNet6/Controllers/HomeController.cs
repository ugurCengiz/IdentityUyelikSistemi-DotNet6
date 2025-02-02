﻿using System.Security.Claims;
using IdentityUyelikSistemi_DotNet6.Models;
using IdentityUyelikSistemi_DotNet6.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace IdentityUyelikSistemi_DotNet6.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ILogger<HomeController> logger) : base(userManager, signInManager)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Member");
            }
            return View();
        }

        public IActionResult Login(string ReturnUrl)
        {
            TempData["ReturnUrl"] = ReturnUrl;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel userLogin)
        {
            if (ModelState.IsValid)
            {
                AppUser user = await _userManager.FindByEmailAsync(userLogin.Email);
                if (user != null)
                {
                    if (await _userManager.IsLockedOutAsync(user))
                    {
                        ModelState.AddModelError("", "Hesabınız Bir Süreliğine Kitlenmiştir. Lütfen daha sonra tekrar deneyiniz.");
                        return View(userLogin);
                    }

                    if (await _userManager.IsEmailConfirmedAsync(user) == false)
                    {
                        ModelState.AddModelError("", "Email adresiniz onaylanmamıştır. Lütfen e postanızı kontrol ediniz.");
                        return View(userLogin);
                    }


                    await _signInManager.SignOutAsync();

                    SignInResult signInResult = await _signInManager.PasswordSignInAsync(user, userLogin.Password, userLogin.RememberMe, false);

                    if (signInResult.Succeeded)
                    {
                        await _userManager.ResetAccessFailedCountAsync(user);

                        if (TempData["ReturnUrl"] != null)
                        {
                            return Redirect(TempData["ReturnUrl"].ToString());
                        }
                        return RedirectToAction("Index", "Member");

                    }
                    else
                    {
                        await _userManager.AccessFailedAsync(user);


                        int fail = await _userManager.GetAccessFailedCountAsync(user);
                        ModelState.AddModelError("", $"{fail} kez başarısız giriş.");

                        if (fail == 3)
                        {
                            await _userManager.SetLockoutEndDateAsync(user, new DateTimeOffset(DateTime.Now.AddMinutes(20)));

                            ModelState.AddModelError("", "Hesabınız 3 başarısız girişten dolayı 20 dakika süreyle kitlenmiştir. Lütfen daha sonra tekrar deneyiniz.");
                        }
                        else
                        {
                            ModelState.AddModelError("", "Email adresiniz veya şifresi yanlış.");
                        }
                    }

                }
                else
                {
                    ModelState.AddModelError("", "Bu email adresine kayıtlı kullanıcı bulunamamıştır.");

                }
            }

            return View(userLogin);
        }


        public IActionResult SignUp()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> SignUp(UserViewModel userViewModel)
        {
            if (_userManager.Users.Any(x=>x.PhoneNumber==userViewModel.PhoneNumber))
            {
                ModelState.AddModelError("","Bu telefon numarası kayıtlıdır.");
                return View(userViewModel);
            }

            AppUser user = new AppUser();
            user.UserName = userViewModel.UserName;
            user.Email = userViewModel.Email;
            user.PhoneNumber = userViewModel.PhoneNumber;

            IdentityResult result = await _userManager.CreateAsync(user, userViewModel.Password);

            if (result.Succeeded)
            {
                string confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                string link = Url.Action("ConfirmEmail", "Home", new
                {
                    userId = user.Id,
                    token = confirmationToken,

                }, protocol: HttpContext.Request.Scheme);

                Helper.EmailConfirmation.SendEmail(link, user.Email);
                _logger.LogInformation($"{user.Id} - {user.UserName} eklendi.");
                return RedirectToAction("Login");

            }

            else
            {
                _logger.LogError($"Hata {result.ToString()}");

                AddModelError(result);
            }


            return View(userViewModel);
        }

        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(PasswordResetViewModel passwordResetViewModel)
        {
            AppUser user = _userManager.FindByEmailAsync(passwordResetViewModel.Email).Result;
            if (user != null)
            {
                string passwordResetToken = _userManager.GeneratePasswordResetTokenAsync(user).Result;

                string passwordResetLink = Url.Action("ResetPasswordConfirm", "Home", new
                {
                    userId = user.Id,
                    token = passwordResetToken,

                }, HttpContext.Request.Scheme);

                Helper.PasswordReset.PasswordResetSendEmail(passwordResetLink, user.Email);

                ViewBag.status = "success";

            }
            else
            {
                ModelState.AddModelError("", "Sistemde kayıtlı email adresi bulunamadı.");

            }

            return View(passwordResetViewModel);
        }

        public IActionResult ResetPasswordConfirm(string userId, string token)
        {
            TempData["userId"] = userId;
            TempData["token"] = token;

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> ResetPasswordConfirm([Bind("PasswordNew")] PasswordResetViewModel passwordResetViewModel)
        {
            string userId = TempData["userId"].ToString();
            string token = TempData["token"].ToString();

            AppUser user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                IdentityResult result =
                    await _userManager.ResetPasswordAsync(user, token, passwordResetViewModel.PasswordNew);

                if (result.Succeeded)
                {
                    await _userManager.UpdateSecurityStampAsync(user);
                    ViewBag.status = "success";

                }
                else
                {
                    AddModelError(result);
                }
            }
            else
            {
                ModelState.AddModelError("", "Bir hata meydana geldi lütfen daha sonra tekrar deneyiniz.");
            }


            return View(passwordResetViewModel);


        }


        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var usera = await _userManager.FindByIdAsync(userId);

            IdentityResult result = await _userManager.ConfirmEmailAsync(usera, token);

            if (result.Succeeded)
            {
                ViewBag.status = "Email Adresiniz onaylanmıştır.";
            }
            else
            {
                ViewBag.status = "Email Adresiniz onaylanamadı. Lütfen tekrar deneyiniz.";
            }

            return View();
        }

        public IActionResult FacebookLogin(string ReturnUrl)
        {
            string redirectUrl = Url.Action("ExternalResponse", "Home", new { ReturnUrl = ReturnUrl });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Facebook", redirectUrl);

            return new ChallengeResult("Facebook", properties);
        }


        public async Task<IActionResult> ExternalResponse(string ReturnUrl = "/")
        {
            ExternalLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login");
            }
            else
            {
                SignInResult result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, true);

                if (result.Succeeded)
                {
                    return Redirect(ReturnUrl);
                }
                else
                {
                    AppUser user = new AppUser();

                    user.Email = info.Principal.FindFirst(ClaimTypes.Email).Value;
                    string ExternalUserId = info.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;

                    if (info.Principal.HasClaim(x => x.Type == ClaimTypes.Name))
                    {
                        string userName = info.Principal.FindFirst(ClaimTypes.Name).Value;

                        userName = userName.Replace(" ", "-").ToLower() + ExternalUserId.Substring(0, 5).ToString();

                        user.UserName = userName;
                    }
                    else
                    {
                        user.UserName = info.Principal.FindFirst(ClaimTypes.Email).Value;
                    }

                    IdentityResult createResult = await _userManager.CreateAsync(user);

                    if (createResult.Succeeded)
                    {
                        IdentityResult loginResult = await _userManager.AddLoginAsync(user, info);

                        if (loginResult.Succeeded)
                        {
                           // await _signInManager.SignInAsync(user, true);

                           await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, true);
                            return Redirect(ReturnUrl);
                        }
                        else
                        {
                            AddModelError(loginResult);
                        }
                    }
                    else
                    {
                        AddModelError(createResult);
                    }
                }
            }

            List<string> errors = ModelState.Values.SelectMany(x => x.Errors).Select(y => y.ErrorMessage).ToList();

            return View("Error", errors);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
