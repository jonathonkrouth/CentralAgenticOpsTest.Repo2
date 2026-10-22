using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Ingredients.Infrastructure.Tests.Fakes;

internal sealed class FakeDbCommand : DbCommand
{
    private string _commandText = string.Empty;

    public DataTable? ReaderResult { get; set; }

    public object? ScalarResult { get; set; }

    public int NonQueryResult { get; set; }

    [AllowNull]
    public override string CommandText
    {
        get => _commandText;
        set => _commandText = value ?? string.Empty;
    }

    public override int CommandTimeout { get; set; }

    public override CommandType CommandType { get; set; } = CommandType.Text;

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }

    protected override DbParameterCollection DbParameterCollection { get; } = new FakeDbParameterCollection();

    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery()
    {
        EnsureConnectionIsOpen();
        return NonQueryResult;
    }

    public override object? ExecuteScalar()
    {
        EnsureConnectionIsOpen();
        return ScalarResult;
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        EnsureConnectionIsOpen();
        return (ReaderResult ?? new DataTable()).CreateDataReader();
    }

    private void EnsureConnectionIsOpen()
    {
        if (DbConnection?.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("The connection must be open before executing a command.");
        }
    }
}
