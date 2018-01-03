using System.Threading.Tasks;

namespace IoTHs.Core.Authentication
{
    public interface IApiAuthenticationService
    {
        Task<string> GetTokenAsync();
    }
}