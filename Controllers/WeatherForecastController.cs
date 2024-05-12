using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace SocialLogin.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("external-login")]
        public IActionResult ExternalLogin()
        {
            string clientId = "21256455001-60sa4bestigp4k2n9sdrj4ik8630c4ms.apps.googleusercontent.com";
            string redirectUri = "https://localhost:7051/WeatherForecast/external-auth-callback"; // Your callback URL
            string scope = "openid profile email";
            string state = Guid.NewGuid().ToString();
            string nonce = "random-nonce";

            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope={scope}&state={state}&nonce={nonce}&prompt=consent";

            // Return the Google authentication URL as a JSON response
            return Ok(new { redirectUrl = authUrl });
        }


        [HttpGet("external-auth-callback")]
        public async Task<IActionResult> ExternalLoginCallback(string code, string state)
        {
            

            // Exchange authorization code for tokens
            string clientId = "21256455001-60sa4bestigp4k2n9sdrj4ik8630c4ms.apps.googleusercontent.com";
            string clientSecret = "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";
            string redirectUri = "https://localhost:7051/WeatherForecast/external-auth-callback"; // Your callback URL

            var tokenRequestContent = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("code", code),
        new KeyValuePair<string, string>("client_id", clientId),
        new KeyValuePair<string, string>("client_secret", clientSecret),
        new KeyValuePair<string, string>("redirect_uri", redirectUri),
        new KeyValuePair<string, string>("grant_type", "authorization_code")
    });

            using (var httpClient = new HttpClient())
            {
                var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequestContent);
                var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();

                // Deserialize JSON to JsonDocument
                JsonDocument tokenResponseData = JsonDocument.Parse(tokenResponseContent);

                // Access access_token property
                string accessToken = tokenResponseData.RootElement.GetProperty("access_token").GetString();
                
                var idToken = tokenResponseData.RootElement.GetProperty("id_token").GetString();
                // Assuming idToken is the ID token string you have
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(idToken);

                // Access claims
                string email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
                string name = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
                var userInfo = new
                {
                    email = email,
                    name = name,
                };
                return Ok(userInfo);
                //    // Validate ID token
                //    var handler = new JwtSecurityTokenHandler();
                //    var validationParameters = new TokenValidationParameters
                //    {
                //        ValidAudience = clientId,
                //        ValidIssuer = "https://accounts.google.com",
                //        IssuerSigningKeys = new[]
                //        {
                //    new SymmetricSecurityKey(Encoding.ASCII.GetBytes(clientSecret))
                //}
                //    };

                //try
                //{
                //    SecurityToken validatedToken;
                //    // Extract user info from ID token
                //    var claimsPrincipal = handler.ValidateToken(idToken, validationParameters,out validatedToken);
                //    var userInfo = new
                //    {
                //        Email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value,
                //        Name = claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value,
                //        // Add more user info as needed
                //    };

                //    // Handle user authentication and further actions
                //    // For example, you can store user info in session or database

                //    return Ok(userInfo);
                //}
                //catch (Exception ex)
                //{
                //    // Handle token validation error
                //    return BadRequest("Token validation failed");
                //}
            }
        }
    }
}
