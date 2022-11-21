﻿using IdentityUyelikSistemi_DotNet6.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityUyelikSistemi_DotNet6.Controllers
{
    public class BaseController : Controller
    {
        protected UserManager<AppUser> _userManager;
        protected SignInManager<AppUser> _signInManager;

        protected AppUser CurrentUser => _userManager.FindByNameAsync(User.Identity.Name).Result; 

        public BaseController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public void AddModelError(IdentityResult result)
        {
            foreach (var item in result.Errors)
            {
                ModelState.AddModelError("",item.Description);
            }
        }

       
    }
}