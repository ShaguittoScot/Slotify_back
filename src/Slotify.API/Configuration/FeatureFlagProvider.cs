using Microsoft.Extensions.Configuration;

namespace Slotify.API.Configuration
{
    public class FeatureFlagProvider
    {
        private readonly IConfiguration _configuration;

        public FeatureFlagProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsFeatureEnabled(string featureName)
        {
            return _configuration.GetValue<bool>($"FeatureFlags:{featureName}");
        }
    }
}
