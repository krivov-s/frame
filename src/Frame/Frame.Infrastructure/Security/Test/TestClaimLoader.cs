
namespace Frame.Infrastructure.Security.Test
{
    public class TestClaimLoader : IClaimsLoader
    {
        public Task<List<string>> LoadClaimsAsync(string username)
        {
            List<string> result = new List<string>();
            result.Add("Administrator");
            result.Add("User");
            return Task.FromResult(result);
        }
    }
}
