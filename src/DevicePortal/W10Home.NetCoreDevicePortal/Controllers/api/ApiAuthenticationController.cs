using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IoTHs.Api.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace W10Home.NetCoreDevicePortal.Controllers.api
{
    [Produces("application/json")]
    [Route("api/ApiAuthentication")]
    public class ApiAuthenticationController : Controller
    {
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GenerateToken([FromBody] ApiAuthenticationRequestModel model)
        {
            //if (ModelState.IsValid)
            //{
            //    var user = await _userManager.FindByEmailAsync(model.Email);

            //    if (user != null)
            //    {
            //        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            //        if (result.Succeeded)
            //        {

            //            var claims = new[]
            //            {
            //                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            //                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            //            };

            //            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Tokens:Key"]));
            //            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //            var token = new JwtSecurityToken(_config["Tokens:Issuer"],
            //                _config["Tokens:Issuer"],
            //                claims,
            //                expires: DateTime.Now.AddMinutes(30),
            //                signingCredentials: creds);

            //            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
            //        }
            //    }
            //}

            return BadRequest("Could not create token");
        }
    }


}