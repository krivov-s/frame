
namespace Frame.Infrastructure.Security
{
    public interface IClaimsLoader
    {
        public Task<List<string>> LoadClaimsAsync(string username);
    }
}
