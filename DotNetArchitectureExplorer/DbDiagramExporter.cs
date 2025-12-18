using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace DotNetArchitectureExplorer;



sealed record ColumnInfo
{
    //@formatter:off
    
    public string Schema { get; init; }
    
    public string Table { get; init; }
    
    public string Column { get; init; }
    
    public string DataType { get; init; }
    
    public bool   IsPrimaryKey { get; init; }

    public string TableKey => $"{Schema}.{Table}";
    
    public string ColumnKey => $"{Schema}.{Table}.{Column}";
    
    //@formatter:on
}


 static class DbDiagramExporter
 {

     extension(ColumnInfo column)
     {
         public Node AsTableNode => new Node
         {
             Id    = $"{column.Schema}.{column.Table}",
             Label =  $"{column.Schema}.{column.Table}",
             Icon  = IconClass,
             Group = Collapsed
         };
         
         public Node AsColumnNode => new Node
         {
             Id         = $"{column.Schema}.{column.Table}.{column.Column}",
             Label      = $"{column.Column}({column.DataType})",
             Icon       = IconField,
             Background = "#e5e9ee"
         };
         
        
     }
    
     public static string BuildDatabaseDgml()
     {
         
         
         
         var columns = Db.LoadColumns();
         
         var dgml = new DirectedGraph();

        foreach (var column in from x in columns where x.Schema == "INT" select x)
        {
            
            dgml.Add(new Link
            {
                Source = column.AsTableNode, 
                Target = column.AsColumnNode,
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

            if (!column.Column.EndsWith("Id"))
            {
                return null;
            }

            if (column.Table + "Id" == column.Column)
            {
                return null;
            }
            
            foreach (var (schemaName, columnsInSchema) in allColumns.GroupBy(x=>x.Schema).Select(x=>(schemaName: x.Key, columnsInSchema: x.ToList())))
            {
                foreach (var (tableName, columnsInTable) in columnsInSchema.GroupBy(x=>x.Table).Select(x=>(tableName: x.Key, columnsInTable: x.ToList())))
                {
                    if (tableName == column.Table)
                    {
                        continue;
                    }

                    if (columnsInTable.Count(x=>x.IsPrimaryKey) > 1)
                    {
                        continue;
                    }
                    
                    var foreignKeyColumn = columnsInTable.FirstOrDefault(c => c.IsPrimaryKey && c.Column == column.Column && columnsInTable.Count(x=>x.IsPrimaryKey) == 1);
                    if (foreignKeyColumn is not null)
                    {
                        return foreignKeyColumn;
                    }
                
                }
            }
            
            return null;
        }

     }


     class Db
     {
           public static IReadOnlyList<ColumnInfo> LoadColumns()
     {
         const string sql = @"
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
ORDER BY s.name, t.name, c.column_id;";

         var list = new List<ColumnInfo>();

      string ConnectionString =
             "Data Source=srvtest\\atlas;Initial Catalog=BOAWeb;Integrated Security=True;TrustServerCertificate=True";

         
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
                 Schema       = schema,
                 Table        = table,
                 Column       = column,
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
                 if (maxLength == -1) return $"{typeName}(MAX)";
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
     
   

  



 }
