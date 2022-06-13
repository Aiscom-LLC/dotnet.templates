using System.Security.Authentication;
using AuthorizationServer.Interfaces.Handlers.Actions;
using AuthorizationServer.Interfaces.Services;
using AuthorizationServer.Models;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Server.AspNetCore;

namespace AuthorizationServer.Handlers.Actions;

public class ExternalProviderHandler : BaseUserPrincipalHandler, IExternalProviderHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserCreatorService _userCreatorService;

    public ExternalProviderHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserCreatorService userCreatorService)
        : base(signInManager)
    {
        _userManager = userManager;
        _userCreatorService = userCreatorService;
    }

    public async Task SignInAsync(HttpContext context, string provider, string redirectUrl)
    {
        var externalLoginInfo = await SignInManager.GetExternalLoginInfoAsync();
        if (externalLoginInfo == null || externalLoginInfo.LoginProvider != provider)
        {
            var existingProviders = (await SignInManager.GetExternalAuthenticationSchemesAsync()).Select(x => x.Name);
            if (!existingProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
            {
                await ForbidAsync(context, "Required provider is not specified.");
                return;
            }

            var properties = SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            await context.ChallengeAsync(provider, properties);
            return;
        }

        var user = await CreateUserIfNotExist(externalLoginInfo);
        var principal = await CreateUserPrincipalAsync(context.GetOpenIddictServerRequest(), user);
        await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, principal);
    }

    private static async Task ForbidAsync(HttpContext context, string message)
    {
        var authenticationProperties = new AuthenticationProperties(
            new Dictionary<string, string>
            {
                { OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription, message }
            });
        await context.ForbidAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, authenticationProperties);
    }

    private async Task<ApplicationUser> CreateUserIfNotExist(ExternalLoginInfo externalLoginInfo)
    {
        var user = await _userManager.FindByLoginAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey);
        if (user != null)
        {
            return user;
        }

        var result = await _userCreatorService.CreateUserByExternalInfoAsync(externalLoginInfo);
        if (!result.Succeeded)
        {
            throw new AuthenticationException(string.Join("; ", result.Errors));
        }

        return await _userManager.FindByLoginAsync(externalLoginInfo.LoginProvider, externalLoginInfo.ProviderKey);
    }
}