﻿using AuthorizationServer.Constants;
using AuthorizationServer.Data;
using OpenIddict.Abstractions;
using Microsoft.AspNetCore.Identity;
using AuthorizationServer.Models;

namespace AuthorizationServer
{
    public class SeedDataService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public SeedDataService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            // Create database if needed
            var userDbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
            await userDbContext.Database.EnsureCreatedAsync(cancellationToken);

            // Register new clients
            var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            await RegisterClientsAsync(applicationManager, cancellationToken);

            // Create roles
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = { UserRoles.AuthorizationAdmin, UserRoles.ApplicationAdmin, UserRoles.User };

            IdentityResult roleResult;

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create AuthorizationAdmin and ApplicationAdmin
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await CreateAuthorizationAdminAsync(userManager);
            await CreateApplicationAdminAsync(userManager);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task RegisterClientsAsync(
            IOpenIddictApplicationManager applicationManager,
            CancellationToken cancellationToken)
        {
            // You can register your own client(s). SPA app, for instance
            await AddSpaClientAsync(applicationManager, cancellationToken);
            await AddBackendClientAsync(applicationManager, cancellationToken);
        }

        private static async Task AddSpaClientAsync(IOpenIddictApplicationManager applicationManager, CancellationToken cancellationToken)
        {
            if (await applicationManager.FindByClientIdAsync(ClientNames.SpaClient, cancellationToken) == null)
            {
                await applicationManager.CreateAsync(
                    new OpenIddictApplicationDescriptor
                    {
                        ClientId = ClientNames.SpaClient,

                        // Specify URLs application can redirect to
                        RedirectUris =
                        {
                            new Uri("http://localhost:3000/authCallback")
                        },
                        Permissions =
                        {
                            OpenIddictConstants.Permissions.Endpoints.Token,
                            OpenIddictConstants.Permissions.Endpoints.Revocation,
                            OpenIddictConstants.Permissions.Endpoints.Authorization,
                            OpenIddictConstants.Permissions.ResponseTypes.Code,
                            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                            OpenIddictConstants.Permissions.GrantTypes.Password,
                            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                            OpenIddictConstants.Permissions.Prefixes.GrantType + CustomGrantTypes.TwoFactorAuthentication,
                            OpenIddictConstants.Permissions.Scopes.Email
                        }
                    },
                    cancellationToken);
            }
        }

        private static async Task AddBackendClientAsync(
            IOpenIddictApplicationManager applicationManager,
            CancellationToken cancellationToken)
        {
            if (await applicationManager.FindByClientIdAsync(ClientNames.BackendClient, cancellationToken) == null)
            {
                await applicationManager.CreateAsync(
                    new OpenIddictApplicationDescriptor
                    {
                        ClientId = ClientNames.BackendClient,
                        ClientSecret = "secret",
                        Permissions =
                        {
                            OpenIddictConstants.Permissions.Endpoints.Introspection
                        }
                    },
                    cancellationToken);
            }
        }

        private async Task CreateAuthorizationAdminAsync(UserManager<ApplicationUser> userManager)
        {
            var user = await userManager.FindByNameAsync(AuthorizationAdminUser.UserName);

            if (user != null)
            {
                return;
            }

            user = new ApplicationUser
            {
                UserName = AuthorizationAdminUser.UserName,
                Email = AuthorizationAdminUser.Email
            };

            var result = await userManager.CreateAsync(user, AuthorizationAdminUser.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, UserRoles.AuthorizationAdmin);
            }
        }

        private async Task CreateApplicationAdminAsync(UserManager<ApplicationUser> userManager)
        {
            var user = await userManager.FindByNameAsync(ApplicationAdminUser.UserName);

            if (user != null)
            {
                return;
            }

            user = new ApplicationUser
            {
                UserName = ApplicationAdminUser.UserName,
                Email = ApplicationAdminUser.Email
            };

            var result = await userManager.CreateAsync(user, ApplicationAdminUser.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, UserRoles.ApplicationAdmin);
            }
        }
    }
}