using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using W10Home.NetCoreDevicePortal.DataAccess.Interfaces;
using W10Home.NetCoreDevicePortal.Security;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [ApiKeyAuthentication()]
    [Produces("application/json")]
    [Route("api/ApiAuthentication")]
    public class ApiAuthenticationController : Controller
    {
        private IConfiguration _configuration;
        private IDeviceService _deviceService;

        public ApiAuthenticationController(IConfiguration configuration, IDeviceService deviceService)
        {
            _configuration = configuration;
            _deviceService = deviceService;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateToken([FromBody] ApiAuthenticationRequestModel model)
        {
            if (ModelState.IsValid)
            {
                var device = await _deviceService.GetWithApiKeyAsync(model.DeviceId, model.ApiKey);

                if (device != null)
                {
                    if (device.ApiKey == model.ApiKey)
                    {
                        var claims = new[]
                        {
                            new Claim(JwtRegisteredClaimNames.Sub, device.RowKey),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                        };

                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Authentication:Tokens:Key"]));
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                        var token = new JwtSecurityToken(_configuration["Authentication:Tokens:Issuer"],
                            _configuration["Authentication:Tokens:Issuer"],
                            claims,
                            expires: DateTime.Now.AddMinutes(30),
                            signingCredentials: creds);

                        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
                    }
                }
            }

            return BadRequest("Could not create token");
        }
    }


}