using System.Reflection;
using Frame.Domain.Entities.Core;

namespace Frame.Domain.Entities.Metadata;

public class FieldMetaInfo
{
    public required PropertyInfo DotNetField { get; set; }
    public required IField FrameField { get; set; }
}