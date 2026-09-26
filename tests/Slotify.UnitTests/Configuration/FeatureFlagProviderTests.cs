using Microsoft.Extensions.Configuration;
using Slotify.API.Configuration;
using System.Collections.Generic;
using Xunit;

namespace Slotify.UnitTests.Configuration
{
    public class FeatureFlagProviderTests
    {
        [Fact]
        public void IsFeatureEnabled_ReturnsTrue_WhenFeatureIsConfiguredAsTrue()
        {
            // Arrange
            var inMemorySettings = new Dictionary<string, string> {
                {"FeatureFlags:EnableNewDashboard", "true"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var provider = new FeatureFlagProvider(configuration);

            // Act
            var isEnabled = provider.IsFeatureEnabled("EnableNewDashboard");

            // Assert
            Assert.True(isEnabled);
        }

        [Fact]
        public void IsFeatureEnabled_ReturnsFalse_WhenFeatureIsMissing()
        {
            // Arrange
            IConfiguration configuration = new ConfigurationBuilder().Build();
            var provider = new FeatureFlagProvider(configuration);

            // Act
            var isEnabled = provider.IsFeatureEnabled("NonExistentFeature");

            // Assert
            Assert.False(isEnabled);
        }
    }
}
