using System.Data.Common;

namespace Ingredients.Infrastructure.Data;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
