﻿using Microsoft.AspNetCore.Authorization;

namespace IdentityUyelikSistemi_DotNet6
{
    public class ExpireDateExchangeRequirement : IAuthorizationRequirement
    {

    }


    public class ExpireDateExchangeHandler : AuthorizationHandler<ExpireDateExchangeRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ExpireDateExchangeRequirement requirement)
        {
            if (context.User != null & context.User.Identity != null)
            {
                var claim = context.User.Claims.Where(x => x.Type == "ExpireDateExchange" && x.Value != null)
                    .FirstOrDefault();

                if (claim != null)
                {
                    if (DateTime.Now < Convert.ToDateTime(claim.Value))
                    {
                        context.Succeed(requirement);
                    }
                    else
                    {
                        context.Fail();
                    }
                }

            }
            return Task.CompletedTask;
        }
    }
}
