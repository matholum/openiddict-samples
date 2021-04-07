using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Core;
using OpenIddict.EntityFrameworkCore.Models;
using Ralltiir.Server.EF;
using Ralltiir.Server.Models;

namespace Ralltiir.Server
{
    public class DataMigration : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public DataMigration(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            await context.Database.EnsureCreatedAsync(cancellationToken);

            await CreateApplicationsAsync();
            await CreateRoles();
            await CreateUsers();

            async Task CreateApplicationsAsync()
            {
                var manager = scope.ServiceProvider
                    .GetRequiredService<OpenIddictApplicationManager<OpenIddictEntityFrameworkCoreApplication<Guid>>>();

                if (await manager.FindByClientIdAsync("ralltiir-client", cancellationToken) == null)
                {
                    await manager.CreateAsync(new OpenIddictApplicationDescriptor
                    {
                        ClientId = "ralltiir-client",
                        DisplayName = "Ralltiir.Client",
                        Permissions =
                        {
                            OpenIddictConstants.Permissions.Endpoints.Token,
                            OpenIddictConstants.Permissions.GrantTypes.Password,
                            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                            OpenIddictConstants.Permissions.ResponseTypes.IdToken,
                            OpenIddictConstants.Permissions.ResponseTypes.IdTokenToken,
                            OpenIddictConstants.Permissions.ResponseTypes.Token,
                            OpenIddictConstants.Permissions.Scopes.Email,
                            OpenIddictConstants.Permissions.Scopes.Profile,
                            OpenIddictConstants.Permissions.Scopes.Roles
                        }
                    }, cancellationToken);
                }
            }

            async Task CreateRoles()
            {
                var manager = scope.ServiceProvider
                    .GetRequiredService<RoleManager<ApplicationUserRole>>();

                if (await manager.RoleExistsAsync("admin") == false)
                {
                    await manager.CreateAsync(new ApplicationUserRole()
                    {
                        Name = "admin",
                        NormalizedName = "Admin",
                        RoleId = Guid.NewGuid(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    });
                }
                
                if (await manager.RoleExistsAsync("moderator") == false)
                {
                    await manager.CreateAsync(new ApplicationUserRole()
                    {
                        Name = "moderator",
                        NormalizedName = "Moderator",
                        RoleId = Guid.NewGuid(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    });
                }
            }

            async Task CreateUsers()
            {
                var manager = scope.ServiceProvider
                    .GetRequiredService<UserManager<ApplicationUser>>();

                const string adminEmail = "admin@email.com";

                if (await manager.FindByEmailAsync(adminEmail) == null)
                {
                    var user = new ApplicationUser {
                        UserName = adminEmail,
                        Email = adminEmail,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString()
                    };

                    await manager.CreateAsync(user, "Ralltiir~1");
                    await manager.AddToRoleAsync(user, "admin");
                    await manager.AddToRoleAsync(user, "moderator");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}