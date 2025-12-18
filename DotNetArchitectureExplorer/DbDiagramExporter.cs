using System.Data;
using Microsoft.Data.SqlClient;

namespace DotNetArchitectureExplorer;

sealed record ColumnInfo
{
    //@formatter:off

    public string SchemaName { get; init; }

    public string TableName { get; init; }

    public string ColumName { get; init; }

    public string DataType { get; init; }

    public bool IsPrimaryKey { get; init; }

    //@formatter:on
}

static class DbDiagramExporter
{
    public static string BuildDatabaseDgml()
    {
        var columns = Db.LoadColumns();

        var dgml = new DirectedGraph();

        foreach (var column in from x in columns select x)
        {
            dgml.Add(new Link
            {
                Source   = column.AsTableNode,
                Target   = column.AsColumnNode,
                Category = Contains
            });

            var foreignKeyColumn = FindForeignKeyColumn(columns, column);
            if (foreignKeyColumn is not null)
            {
                dgml.Add(new Link
                {
                    Source = column.AsColumnNode,
                    Target = foreignKeyColumn.AsColumnNode
                });
            }
        }

        return dgml.ToDirectedGraphElement().ToString();

        static ColumnInfo FindForeignKeyColumn(IReadOnlyList<ColumnInfo> allColumns, ColumnInfo column)
        {
            if (column.IsPrimaryKey)
            {
                return null;
            }

            if (!column.ColumName.EndsWith("Id"))
            {
                return null;
            }

            if (column.TableName + "Id" == column.ColumName)
            {
                return null;
            }

            var foreignKeyColumns = (from x in allColumns where IsForeignKey(allColumns, column, x) select x).ToList();
            if (foreignKeyColumns.Count == 0)
            {
                return null;
            }

            if (foreignKeyColumns.Count == 1)
            {
                return foreignKeyColumns[0];
            }

            if (column.ColumName == "UserId")
            {
                return (from x in allColumns where x.SchemaName == "INT" && x.TableName == "WebUser" && x.ColumName == "UserId" select x).FirstOrDefault();
            }

            return null;

            static bool IsForeignKey(IReadOnlyList<ColumnInfo> allColumns, ColumnInfo a, ColumnInfo maybeForeignKeyColumnOfA)
            {
                if (a.TableName == maybeForeignKeyColumnOfA.TableName)
                {
                    return false;
                }

                if (a.ColumName != maybeForeignKeyColumnOfA.ColumName)
                {
                    return false;
                }

                var primaryKeysInTable = (from x in allColumns where x.TableName == maybeForeignKeyColumnOfA.TableName && x.IsPrimaryKey select x).ToList();

                return primaryKeysInTable.Count == 1 && primaryKeysInTable[0].ColumName == maybeForeignKeyColumnOfA.ColumName;
            }
        }
    }

    class Db
    {
        public static IReadOnlyList<ColumnInfo> LoadColumns()
        {
            const string sql
                = """

                  SELECT
                      s.name              AS SchemaName,
                      t.name              AS TableName,
                      c.name              AS ColumnName,
                      ty.name             AS TypeName,
                      c.max_length,
                      c.precision,
                      c.scale,
                      c.is_nullable,
                      -- PK tespiti
                      CASE WHEN kc.type = 'PK' THEN 1 ELSE 0 END AS IsPrimaryKey
                  FROM sys.schemas s
                  JOIN sys.tables t           ON t.schema_id = s.schema_id
                  JOIN sys.columns c          ON c.object_id = t.object_id
                  JOIN sys.types ty           ON ty.user_type_id = c.user_type_id
                  LEFT JOIN sys.index_columns ic
                      ON ic.object_id = t.object_id AND ic.column_id = c.column_id
                  LEFT JOIN sys.key_constraints kc
                      ON kc.parent_object_id = t.object_id
                     AND kc.unique_index_id  = ic.index_id
                     AND kc.type = 'PK'
                  WHERE t.is_ms_shipped = 0
                    AND s.name NOT IN ('sys', 'INFORMATION_SCHEMA')
                  ORDER BY s.name, t.name, c.column_id;
                  """;

            var list = new List<ColumnInfo>();

            var ConnectionString =
                "Data Source=srvtest\\atlas;Initial Catalog=BOA;Integrated Security=True;TrustServerCertificate=True";

            using var conn = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandType = CommandType.Text;
            conn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var schema = rdr.GetString(rdr.GetOrdinal("SchemaName"));
                var table = rdr.GetString(rdr.GetOrdinal("TableName"));
                var column = rdr.GetString(rdr.GetOrdinal("ColumnName"));

                var typeName = rdr.GetString(rdr.GetOrdinal("TypeName"));
                var maxLength = rdr.GetInt16(rdr.GetOrdinal("max_length")); // smallint
                var precision = rdr.GetByte(rdr.GetOrdinal("precision"));
                var scale = rdr.GetByte(rdr.GetOrdinal("scale"));
                var isNullable = rdr.GetBoolean(rdr.GetOrdinal("is_nullable"));
                var isPk = rdr.GetInt32(rdr.GetOrdinal("IsPrimaryKey")) == 1;

                var dataType = FormatSqlType(typeName, maxLength, precision, scale);

                list.Add(new ColumnInfo
                {
                    SchemaName   = schema,
                    TableName    = table,
                    ColumName    = column,
                    DataType     = dataType + (isNullable ? "?" : ""),
                    IsPrimaryKey = isPk
                });
            }

            return list;
        }

        static string FormatSqlType(string typeName, short maxLength, byte precision, byte scale)
        {
            var t = typeName.ToLowerInvariant();
            switch (t)
            {
                case "varchar":
                case "char":
                case "varbinary":
                case "binary":
                    return maxLength == -1 ? $"{typeName}(MAX)" : $"{typeName}({maxLength})";

                case "nvarchar":
                case "nchar":
                    if (maxLength == -1)
                    {
                        return $"{typeName}(MAX)";
                    }

                    // nvarchar/nchar byte cinsinden tutulur, karakter sayısı = maxLength / 2
                    return $"{typeName}({maxLength / 2})";

                case "decimal":
                case "numeric":
                    return $"{typeName}({precision},{scale})";

                case "datetime2":
                case "time":
                case "datetimeoffset":
                    return $"{typeName}({scale})";

                default:
                    return typeName;
            }
        }
    }

    extension(ColumnInfo column)
    {
        public Node AsTableNode => new()
        {
            Id    = $"{column.SchemaName}.{column.TableName}",
            Label = $"{column.SchemaName}.{column.TableName}",
            Icon  = IconClass,
            Group = Collapsed
        };

        public Node AsColumnNode => new()
        {
            Id         = $"{column.SchemaName}.{column.TableName}.{column.ColumName}",
            Label      = $"{column.ColumName}({column.DataType})",
            Icon       = IconField,
            Background = "#e5e9ee"
        };
    }
}