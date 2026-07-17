using Config.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentStorage.Tests.Integration.Config {
	public static class TestConfigLoader {
		private static ITestConfig _instance;

		public static ITestConfig Instance {
			get {
				if (_instance == null) {
					_instance = new ConfigurationBuilder<ITestConfig>()
					   .UseIniFile("c:\\tmp\\integration-tests.ini")
					   .UseEnvironmentVariables()
					   .Build();

					_instance = new ConfigurationBuilder<ITestConfig>()
					   .UseIniFile("c:\\tmp\\integration-tests.ini")
					   //.UseAzureDevOpsVariableSet(_instance.DevOpsOrgName, _instance.DevOpsProject, _instance.DevOpsPat, _instance.DevOpsVariableSetId)
					   .UseEnvironmentVariables()
					   .Build();

				}

				return _instance;
			}
		}
	}
}
