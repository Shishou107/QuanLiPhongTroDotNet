using System.Data;
using Microsoft.Data.SqlClient;

namespace ApiQuanLyPhongTro.Data;

public class AdoNetDb
{
    private readonly string _connectionString;

    public AdoNetDb(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing connection string: ConnectionStrings:DefaultConnection");
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public SqlCommand CreateStoredProcedureCommand(SqlConnection connection, string procedureName)
    {
        return new SqlCommand(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };
    }

    public DataTable FillDataTable(SqlCommand command)
    {
        using var adapter = new SqlDataAdapter(command);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    public DataSet FillDataSet(SqlCommand command)
    {
        using var adapter = new SqlDataAdapter(command);
        var dataSet = new DataSet();
        adapter.Fill(dataSet);
        return dataSet;
    }
}

public static class SqlDataReaderExtensions
{
    public static Guid GetGuidValue(this SqlDataReader reader, string columnName)
    {
        return reader.GetGuid(reader.GetOrdinal(columnName));
    }

    public static Guid? GetNullableGuidValue(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
    }

    public static string GetStringValue(this SqlDataReader reader, string columnName)
    {
        return reader.GetString(reader.GetOrdinal(columnName));
    }

    public static string? GetNullableStringValue(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static int GetIntValue(this SqlDataReader reader, string columnName)
    {
        return reader.GetInt32(reader.GetOrdinal(columnName));
    }

    public static decimal GetDecimalValue(this SqlDataReader reader, string columnName)
    {
        return reader.GetDecimal(reader.GetOrdinal(columnName));
    }

    public static decimal? GetNullableDecimalValue(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    public static DateOnly GetDateOnlyValue(this SqlDataReader reader, string columnName)
    {
        return DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal(columnName)));
    }

    public static DateOnly? GetNullableDateOnlyValue(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : DateOnly.FromDateTime(reader.GetDateTime(ordinal));
    }

    public static DateTime? GetNullableDateTimeValue(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}

public static class DataRowExtensions
{
    public static Guid GetGuidValue(this DataRow row, string columnName)
    {
        return (Guid)row[columnName];
    }

    public static Guid? GetNullableGuidValue(this DataRow row, string columnName)
    {
        return row.IsNull(columnName) ? null : (Guid)row[columnName];
    }

    public static string GetStringValue(this DataRow row, string columnName)
    {
        return (string)row[columnName];
    }

    public static string? GetNullableStringValue(this DataRow row, string columnName)
    {
        return row.IsNull(columnName) ? null : (string)row[columnName];
    }

    public static int GetIntValue(this DataRow row, string columnName)
    {
        return Convert.ToInt32(row[columnName]);
    }

    public static decimal GetDecimalValue(this DataRow row, string columnName)
    {
        return Convert.ToDecimal(row[columnName]);
    }

    public static decimal? GetNullableDecimalValue(this DataRow row, string columnName)
    {
        return row.IsNull(columnName) ? null : Convert.ToDecimal(row[columnName]);
    }

    public static DateOnly GetDateOnlyValue(this DataRow row, string columnName)
    {
        return DateOnly.FromDateTime(Convert.ToDateTime(row[columnName]));
    }

    public static DateOnly? GetNullableDateOnlyValue(this DataRow row, string columnName)
    {
        return row.IsNull(columnName) ? null : DateOnly.FromDateTime(Convert.ToDateTime(row[columnName]));
    }

    public static DateTime? GetNullableDateTimeValue(this DataRow row, string columnName)
    {
        return row.IsNull(columnName) ? null : Convert.ToDateTime(row[columnName]);
    }
}

public static class SqlParameterValue
{
    public static object FromString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }

    public static object FromNullable<T>(T? value) where T : struct
    {
        return value.HasValue ? value.Value : DBNull.Value;
    }
}
