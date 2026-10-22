using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Ingredients.Infrastructure.Data;

public sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public DbConnection CreateConnection() => new SqlConnection(connectionString);
}
