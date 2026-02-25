using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{

    public class Field
    {
        public Guid Id { get; set; }
        public Guid EntityId { get; set; }
        public string LogicalName { get; set; } = string.Empty;
        public string PhysicalName { get; set; } = string.Empty;
        public string DataType { get; set; } = "string";

        public Entity Entity { get; set; } = null!;
    }
}
