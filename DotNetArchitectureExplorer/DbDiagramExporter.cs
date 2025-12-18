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

 sealed class TableInfo
{
    //@formatter:off
    
    public string Schema { get; init; }
    
    public string Table { get; init; }

    public Node TableNode { get; init; }
    
    public List<ColumnInfo> Columns { get; } = new();
    
    public IReadOnlyList<ColumnInfo> PrimaryKeys => Columns.Where(c => c.IsPrimaryKey).ToList();

    public string Key => $"{Schema}.{Table}";
    
    //@formatter:on
}

 public static class DbDiagramExporter
 {
     
    
     public static string BuildDatabaseDgml()
     {
         
         
         
         var columns = Db.LoadColumns();
         
         var dgml = new DirectedGraph();

        foreach (var column in columns)
        {
            var tableNode = new Node
            {
                Id    = $"{column.Schema}.{column.Table}",
                Label =  $"{column.Schema}.{column.Table}",
                Icon  = IconClass,
                Group = "Collapsed"
            };
            
            var columnNode = new Node
            {
                Id         = $"{column.Schema}.{column.Table}.{column.Column}",
                Label      =  $"{column.Column}({column.DataType})",
                Icon       = IconField,
                Background = "#e5e9ee"
            };
            
            
            dgml.Add(new Link { Source = tableNode, Target = columnNode, Category = "Contains" });
            
            
        }


        return dgml.ToDirectedGraphElement().ToString();

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
                 DataType     = dataType + (isNullable ? " NULL" : " NOT NULL"),
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
     
   

      // Tabloları sözlüğe çeker ve Table Node'larını hazırlar
     private static Dictionary<string, TableInfo> BuildTables(IReadOnlyList<ColumnInfo> columns, HashSet<Node> nodes)
     {
         var tables = new Dictionary<string, TableInfo>(StringComparer.OrdinalIgnoreCase);

         foreach (var grp in columns.GroupBy(c => c.TableKey))
         {
             var first = grp.First();
             var tableNode = new Node
             {
                 Id         = first.TableKey,
                 Label      = $"{first.Schema}.{first.Table}",
                 Group      = "Expanded",
                 Icon       = "Table",
                 Background = "#FFE9F5FF",
                 NodeRadius = 5,
                 FontSize   = 12
             };

             var tableInfo = new TableInfo
             {
                 Schema    = first.Schema,
                 Table     = first.Table,
                 TableNode = tableNode
             };
             tableInfo.Columns.AddRange(grp);

             tables[tableInfo.Key] = tableInfo;
             nodes.Add(tableNode);
         }

         return tables;
     }

     // Kolon Node'u oluşturur (tekil; Id ile eşitlik sağlandığı için HashSet duplicate önler)
     private static Node MakeColumnNode(ColumnInfo c) => new Node
     {
         Id          = c.ColumnKey,
         Label       = c.Column,
         Icon        = c.IsPrimaryKey ? "Key" : "Property",
         Background  = c.IsPrimaryKey ? "#FFFDEBD0" : "#FFFFFFFF",
         Description = $"{c.DataType}",
         FontSize    = 11,
         NodeRadius  = 4
     };

     // Kolon adından (XxxId) hedef tablo ve hedef PK kolonunu bulur
     private static ColumnInfo ResolveForeignKeyTarget(Dictionary<string, TableInfo> tables, ColumnInfo sourceColumn)
     {
         // Sadece ...Id kolonlarını değerlendir
         var m = Regex.Match(sourceColumn.Column, @"^(?<ref>.+)Id$", RegexOptions.CultureInvariant);
         if (!m.Success) return null;

         var refName = m.Groups["ref"].Value; // ör. "User" (UserId)
         // Aday tablo adları: exact, plural forms
         var candidates = CandidateTableNames(refName);

         // Önce aynı şemada bak, sonra tüm şemalarda
         var sameSchema = tables.Values.Where(t => t.Schema.Equals(sourceColumn.Schema, StringComparison.OrdinalIgnoreCase));
         var crossSchema = tables.Values;

         var targetTable =
             sameSchema.FirstOrDefault(t => candidates.Contains(t.Table, StringComparer.OrdinalIgnoreCase)) ??
             crossSchema.FirstOrDefault(t => candidates.Contains(t.Table, StringComparer.OrdinalIgnoreCase));

         if (targetTable is null) return null;

         // Hedef PK kolon tercihi: 1) refName + "Id" (tam ad eşleşmesi), 2) tablonun ilk PK kolonu
         var preferredName = refName + "Id";
         var targetColumn = targetTable.Columns
                                       .FirstOrDefault(c => c.Column.Equals(preferredName, StringComparison.OrdinalIgnoreCase) && c.IsPrimaryKey)
                            ?? targetTable.PrimaryKeys.FirstOrDefault();

         return targetColumn;
     }

     // Basit çoğul adayları: X, Xs, Xes, (Y->ies)
     private static IReadOnlyList<string> CandidateTableNames(string singular)
     {
         var cands = new List<string> { singular };

         if (singular.EndsWith("y", StringComparison.OrdinalIgnoreCase) && singular.Length > 1)
         {
             cands.Add(singular[..^1] + "ies"); // Category -> Categories
         }

         cands.Add(singular + "s"); // User -> Users
         cands.Add(singular + "es"); // Box -> Boxes, Class -> Classes

         return cands;
     }

     // DGML String üretimi
     private static string ToDgml(HashSet<Node> nodes, DirectedGraph graph)
     {
         var sb = new StringBuilder();
         sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
         sb.AppendLine(@"<DirectedGraph xmlns=""http://schemas.microsoft.com/vs/2009/dgml"">");

         // Categories
         sb.AppendLine("  <Categories>");
         sb.AppendLine(@"    <Category Id=""Table"" Label=""Table"" Background=""#FFEEF6FF"" Border=""#FF3A7BD5"" />");
         sb.AppendLine(@"    <Category Id=""Column"" Label=""Column"" Background=""#FFFFFFFF"" Border=""#FF7F7F7F"" />");
         sb.AppendLine(@"    <Category Id=""Contains"" Label=""Contains"" />");
         sb.AppendLine(@"    <Category Id=""ForeignKey"" Label=""ForeignKey"" Stroke=""#FF3A7BD5"" />");
         sb.AppendLine("  </Categories>");

         // Nodes
         sb.AppendLine("  <Nodes>");
         foreach (var n in nodes.OrderBy(n => n.Label))
         {
             // Node kategorisini Label/Group/Icon'a göre tahmin etmeyi deneyelim
             var category = (n.Group?.Equals("Expanded", StringComparison.OrdinalIgnoreCase) ?? false) ? "Table" : "Column";

             sb.Append("    <Node");
             sb.Append($@" Id=""{Xml(n.Id)}""");
             sb.Append($@" Label=""{Xml(n.Label)}""");
             if (!string.IsNullOrWhiteSpace(n.Description)) sb.Append($@" Description=""{Xml(n.Description)}""");
             if (!string.IsNullOrWhiteSpace(n.Group)) sb.Append($@" Group=""{Xml(n.Group)}""");
             if (!string.IsNullOrWhiteSpace(n.Icon)) sb.Append($@" Icon=""{Xml(n.Icon)}""");
             if (!string.IsNullOrWhiteSpace(n.Background)) sb.Append($@" Background=""{Xml(n.Background)}""");
             if (!string.IsNullOrWhiteSpace(n.StrokeDashArray)) sb.Append($@" StrokeDashArray=""{Xml(n.StrokeDashArray)}""");
             sb.Append($@" Category=""{category}""");
             sb.AppendLine(" />");
         }

         sb.AppendLine("  </Nodes>");

         // Links
         sb.AppendLine("  <Links>");
         foreach (var l in graph.Links)
         {
             sb.Append("    <Link");
             sb.Append($@" Source=""{Xml(l.Source.Id)}""");
             sb.Append($@" Target=""{Xml(l.Target.Id)}""");
             if (!string.IsNullOrWhiteSpace(l.Category)) sb.Append($@" Category=""{Xml(l.Category)}""");
             if (!string.IsNullOrWhiteSpace(l.Description)) sb.Append($@" Label=""{Xml(l.Description)}""");
             if (!string.IsNullOrWhiteSpace(l.StrokeDashArray)) sb.Append($@" StrokeDashArray=""{Xml(l.StrokeDashArray)}""");
             sb.AppendLine(" />");
         }

         sb.AppendLine("  </Links>");

         // Styles (opsiyonel)
         sb.AppendLine("  <Styles>");
         sb.AppendLine(@"    <Style TargetType=""Node"" GroupLabel=""Tables"" ValueLabel=""True"">
      <Condition Expression=""HasCategory('Table')"" />
      <Setter Property=""Background"" Value=""#FFE9F5FF""/>
      <Setter Property=""Stroke"" Value=""#FF3A7BD5""/>
      <Setter Property=""Icon"" Value=""Table""/>
    </Style>");
         sb.AppendLine(@"    <Style TargetType=""Node"" GroupLabel=""Columns"" ValueLabel=""True"">
      <Condition Expression=""HasCategory('Column')"" />
      <Setter Property=""Background"" Value=""#FFFFFFFF""/>
      <Setter Property=""Stroke"" Value=""#FF7F7F7F""/>
    </Style>");
         sb.AppendLine(@"    <Style TargetType=""Link"" GroupLabel=""Foreign Keys"" ValueLabel=""True"">
      <Condition Expression=""HasCategory('ForeignKey')"" />
      <Setter Property=""Stroke"" Value=""#FF3A7BD5""/>
      <Setter Property=""StrokeThickness"" Value=""1.5""/>
      <Setter Property=""ArrowSize"" Value=""10""/>
      <Setter Property=""StrokeDashArray"" Value=""2,2""/>
    </Style>");
         sb.AppendLine("  </Styles>");

         // Opsiyonel layout özellikleri
         sb.AppendLine("  <Properties>");
         sb.AppendLine(@"    <Property Id=""Layout"" Value=""Sugiyama""/>");
         sb.AppendLine(@"    <Property Id=""Direction"" Value=""TopToBottom""/>");
         sb.AppendLine("  </Properties>");

         sb.AppendLine("</DirectedGraph>");
         return sb.ToString();
     }


     private static string Xml(string s)
     {
         if (string.IsNullOrEmpty(s)) return string.Empty;

         // Sadece ham karakterleri kaçışla; '&' önce yapılmalı.
         // " &amp; " gibi önceden kaçışlanmış dizgileri tekrar kaçışlama hatasına düşmemek için
         // önce normalize edilmediğinden emin ol. En güvenlisi ham veriyi kaçışlamaktır.
         var sb = new System.Text.StringBuilder(s.Length + 16);
         foreach (var ch in s)
         {
             switch (ch)
             {
                 case '&':  sb.Append("&amp;"); break;
                 case '<':  sb.Append("&lt;"); break;
                 case '>':  sb.Append("&gt;"); break;
                 case '"':  sb.Append("&quot;"); break;
                 case '\'': sb.Append("&apos;"); break; // gerekirse
                 default:   sb.Append(ch); break;
             }
         }

         return sb.ToString();
     }
 }
