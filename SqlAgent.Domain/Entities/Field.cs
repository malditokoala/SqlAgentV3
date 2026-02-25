using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Field
    {
        public Guid Id { get; set; }
        public Guid EntityId { get; set; }
        public string LogicalName { get; set; } = null!;
        public string PhysicalName { get; set; } = null!;
        public string DataType { get; set; } = null!;
        public bool IsDefaultGrainField { get; set; }

        public Entity Entity { get; set; } = null!;
        public ICollection<Synonym> Synonyms { get; set; } = new List<Synonym>();
    }
}
