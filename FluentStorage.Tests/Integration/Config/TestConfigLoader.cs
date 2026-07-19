using YamlDotNet.Serialization;

namespace FluentStorage.Tests.Integration.Config {
	public static class TestConfigLoader {
		private static ITestConfig _config;

		/// <summary>
		/// Loads the test config YAML file and returns the settings in a typed object (`ITestConfig`)
		/// </summary>
		public static ITestConfig Config {
			get {
				if (_config == null) {

					// get the YAML test config file at the repo root
					string projectDir = Directory.GetParent(AppContext.BaseDirectory)!.Parent!.Parent!.FullName;
					string configPath = Path.Combine(projectDir, "fluentstorage.yaml");

					// load it if it exists
					if (!File.Exists(configPath)) {
						throw new Exception("Test config file `fluentstorage.yaml` does not exist at the project root, and is required for testing! Please create it using the `fluentstorage.yaml.template` and fill in the required settings.");
					}

					var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

					try {
						using var reader = File.OpenText(configPath);
						_config = deserializer.Deserialize<ITestConfig>(reader);
					}
					catch (Exception ex) {
						throw new Exception("Failed to read test config file `fluentstorage.yaml`! Maybe it is corrupt or invalid!");
					}
				}

				return _config;
			}
		}
	}
}
