﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityUyelikSistemi_DotNet6.Models
{
    public class AppIdentityDbContext:IdentityDbContext<AppUser,AppRole,string>
    {
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options):base(options)
        {
            

        }

       

    }
}
