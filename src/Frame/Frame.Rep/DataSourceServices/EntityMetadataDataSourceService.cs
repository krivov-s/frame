using Frame.Domain.Entities.Core;
using Frame.Domain.Entities.Metadata;
using Frame.Domain.Params;
using Microsoft.Extensions.Logging;

namespace Frame.Rep.DataSourceServices;

public class EntityMetadataDataSourceService(ILogger<EntityMetadataDataSourceService> logger)
{
    public static List<dynamic> GetEntityMetadataList(ParamList? paramList)
    {
        List<Dictionary<string, dynamic>> rows = new ();

        List<EntityMetaInfo> listMeta = EntityMetadata.GetAllEntitiesMeta();
        foreach (var meta in listMeta)
        {
            rows.AddRange(_buildRowsForEntityType(meta));
        }

        return rows.Cast<dynamic>().ToList();
    }

    private static List<Dictionary<string, dynamic>> _buildRowsForEntityType(EntityMetaInfo meta)
    {
        List<Dictionary<string, dynamic>> rows = new ();
        
        foreach (var fieldMetaInfo in meta.Fields)
        {
            Dictionary<string, dynamic> row = new();
            row["EntityMeta"] = meta;
            row["FieldMeta"] = fieldMetaInfo;
            row["EntityHumanName"] = meta.HumanName;
            row["EntityTypeName"] = meta.DotNetType.Name;
            row["FieldName"] = fieldMetaInfo.FrameField.Name;
            row["FieldHumanName"] = fieldMetaInfo.FrameField.HumanName;
            row["FieldTypeName"] = fieldMetaInfo.DotNetField.PropertyType.Name;
            row["IsAssoc"] = (fieldMetaInfo.FrameField.FieldType == EntityFieldType.Ref);
            
            rows.Add(row);
        }

        return rows;
    }

}