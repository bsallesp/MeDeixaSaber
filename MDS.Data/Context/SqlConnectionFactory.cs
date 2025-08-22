using System.Data.Common;
using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;

namespace MDS.Data.Context;

public sealed class SqlConnectionFactory(string server, string database) : IDbConnectionFactory
{
    readonly TokenCredential _credential = new ManagedIdentityCredential();

    public async Task<DbConnection> GetOpenConnectionAsync(CancellationToken ct = default)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            Encrypt = true,
            TrustServerCertificate = false,
            ConnectTimeout = 30
        };

        var conn = new SqlConnection(builder.ConnectionString);

        conn.RetryLogicProvider = SqlConfigurableRetryFactory.CreateFixedRetryProvider(
            new SqlRetryLogicOption
            {
                NumberOfTries = 3,
                DeltaTime = TimeSpan.FromSeconds(5),
                TransientErrors = new List<int> { 4060, 40197, 40501, 49918, 49919, 49920 }
            });

        var token = await _credential.GetTokenAsync(
            new TokenRequestContext(["https://database.windows.net/.default"]), ct);

        conn.AccessToken = token.Token;

        await conn.OpenAsync(ct);
        return conn;
    }
}