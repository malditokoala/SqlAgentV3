using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Synonym
    {
        public Guid Id { get; set; }
        public Guid FieldId { get; set; }
        public string Term { get; set; } = null!;

        public Field Field { get; set; } = null!;
    }
}
