using IdentityOAuth2.Models.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MSC.Identity.Models;
using MSC.Identity.Models.Entities;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace MSC.Identity.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeedController : ControllerBase
    {
        readonly ApplicationDbContext _context;
        readonly UserManager<IdentityUser> _userManager;
        readonly IOpenIddictApplicationManager _openIddictApplicationManager;
        readonly IConfiguration _configuration;

        public SeedController(
            ApplicationDbContext context, 
            UserManager<IdentityUser> userManager, 
            IOpenIddictApplicationManager openIddictApplicationManager,
            IConfiguration configuration
            )
        {
            _configuration = configuration;
            _context = context;
            _userManager = userManager;
            _openIddictApplicationManager = openIddictApplicationManager;
        }

        [HttpPost("seed")]
        public async Task<IActionResult> Seed()
        {
            List<ApplicationClient> applicationClients = _configuration.GetSection("ApplicationClients").Get<List<ApplicationClient>>();
            foreach (var client in applicationClients)
            {
                await SeedApp(client);
            }
            return Ok(new { ok = true });
        }
    
        async Task SeedApp(ApplicationClient client)
        {
            var clientData = await _openIddictApplicationManager.FindByClientIdAsync(client.ClientId);
            if (clientData is null)
            {
                await _openIddictApplicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = client.ClientId,
                    ClientSecret = client.Secret,
                    DisplayName = client.ClientId,
                    ClientType = ClientTypes.Confidential,
                    RedirectUris = { new Uri($"{client.BEUrl}/swagger/oauth2-redirect.html"), new Uri($"{client.FEUrl}/oauth2-callback") },
                    PostLogoutRedirectUris = { new Uri($"{client.FEUrl}/signout-callback-oidc") },
                    Permissions =
                    {
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.Token,
                        Permissions.Endpoints.Introspection,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,
                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.ResponseTypes.Code,
                        Permissions.Prefixes.Scope + "openid",
                        Permissions.Prefixes.Scope + "profile",
                        Permissions.Prefixes.Scope + "api",
                        Permissions.Prefixes.Scope + "offline_access"
                    }
                });
            }
            else
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await _openIddictApplicationManager.PopulateAsync(descriptor, clientData);

                descriptor.ClientSecret = client.Secret;
                descriptor.RedirectUris.Clear();
                descriptor.RedirectUris.Add(new Uri($"{client.BEUrl}/swagger/oauth2-redirect.html"));
                descriptor.RedirectUris.Add(new Uri($"{client.FEUrl}/oauth2-callback"));
                descriptor.PostLogoutRedirectUris.Clear();
                descriptor.PostLogoutRedirectUris.Add(new Uri($"{client.FEUrl}/signout-callback-oidc"));

                // Ghi lại vào DB
                await _openIddictApplicationManager.UpdateAsync(clientData, descriptor);
            }
        }
    }
}
