using Azure.Identity;
using System;

namespace FluentStorage.Azure.Blobs
{
    /// <summary>
    /// Provides Azure service endpoint hostnames and authority URIs for different cloud environments.
    /// </summary>
    public static class AzureCloudEndpoints {
        /// <summary>
        /// Container for Blob storage endpoint suffixes.
        /// </summary>
        internal static class Blob {
            /// <summary>Global Azure Blob endpoint suffix.</summary>
            internal const string Global = "core.windows.net";
            /// <summary>Azure China Blob endpoint suffix.</summary>
            internal const string China = "core.chinacloudapi.cn";
            /// <summary>Azure US Government Blob endpoint suffix.</summary>
            internal const string USGovernment = "core.usgovcloudapi.net";
            /// <summary>Azure Germany Blob endpoint suffix.</summary>
            internal const string Germany = "core.cloudapi.de";
        }

        /// <summary>
        /// Container for Data Lake (ADLS Gen2) endpoint suffixes.
        /// </summary>
        public static class DataLake {
            /// <summary>Global Azure Data Lake endpoint suffix.</summary>
            internal const string Global = "dfs.core.windows.net";
            /// <summary>Azure China Data Lake endpoint suffix.</summary>
            internal const string China = "dfs.core.chinacloudapi.cn";
            /// <summary>Azure US Government Data Lake endpoint suffix.</summary>
            internal const string USGovernment = "dfs.core.usgovcloudapi.net";
            /// <summary>Azure Germany Data Lake endpoint suffix.</summary>
            internal const string Germany = "dfs.core.cloudapi.de";
        }

        /// <summary>
        /// Container for Azure authority (token issuer) endpoints.
        /// </summary>
        internal static class Authority {
            /// <summary>Global Azure authority URI.</summary>
            internal static Uri Global = AzureAuthorityHosts.AzurePublicCloud;
            /// <summary>Azure China authority URI.</summary>
            internal static Uri China = AzureAuthorityHosts.AzureChina;
            /// <summary>Azure US Government authority URI.</summary>
            internal static Uri USGovernment = AzureAuthorityHosts.AzureGovernment;
            /// <summary>Azure Germany authority URI (obsolete).</summary>
            [Obsolete]
            internal static Uri Germany = AzureAuthorityHosts.AzureGermany;
        }

        /// <summary>
        /// Gets the Blob storage endpoint suffix for the specified Azure cloud environment.
        /// </summary>
        /// <param name="environment">The Azure cloud environment.</param>
        /// <returns>The Blob endpoint suffix (e.g. "core.windows.net").</returns>
        public static string GetBlobEndpoint(AzureCloudEnvironment environment) => environment switch {
            AzureCloudEnvironment.China => Blob.China,
            AzureCloudEnvironment.USGovernment => Blob.USGovernment,
            AzureCloudEnvironment.Germany => Blob.Germany,
            _ => Blob.Global
        };

        /// <summary>
        /// Gets the Data Lake endpoint suffix for the specified Azure cloud environment.
        /// </summary>
        /// <param name="environment">The Azure cloud environment.</param>
        /// <returns>The Data Lake endpoint suffix (e.g. "dfs.core.windows.net").</returns>
        public static string GetDataLakeEndpoint(AzureCloudEnvironment environment) => environment switch {
            AzureCloudEnvironment.China => DataLake.China,
            AzureCloudEnvironment.USGovernment => DataLake.USGovernment,
            AzureCloudEnvironment.Germany => DataLake.Germany,
            _ => DataLake.Global
        };

        /// <summary>
        /// Gets the authority (STS) endpoint URI for the specified Azure cloud environment.
        /// </summary>
        /// <param name="environment">The Azure cloud environment.</param>
        /// <returns>The authority endpoint URI.</returns>
        public static Uri GetAuthorityEndpoint(AzureCloudEnvironment environment) => environment switch {
            AzureCloudEnvironment.China => Authority.China,
            AzureCloudEnvironment.USGovernment => Authority.USGovernment,
            AzureCloudEnvironment.Germany => Authority.Germany,
            _ => Authority.Global
        };
    }
}
