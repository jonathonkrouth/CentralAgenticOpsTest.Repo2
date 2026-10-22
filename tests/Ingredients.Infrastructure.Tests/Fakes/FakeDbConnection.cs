using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Ingredients.Infrastructure.Tests.Fakes;

internal sealed class FakeDbConnection(FakeDbCommand command) : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;
    private string _connectionString = string.Empty;

    [AllowNull]
    public override string ConnectionString
    {
        get => _connectionString;
        set => _connectionString = value ?? string.Empty;
    }

    public override string Database => "FakeDatabase";

    public override string DataSource => "FakeDataSource";

    public override string ServerVersion => "1.0";

    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

    public override void Open() => _state = ConnectionState.Open;

    public override void Close() => _state = ConnectionState.Closed;

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();

    protected override DbCommand CreateDbCommand()
    {
        command.Connection = this;
        return command;
    }
}
