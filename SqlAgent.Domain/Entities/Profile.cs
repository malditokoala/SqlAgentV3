using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class Profile
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public Guid? ActiveVersionId { get; set; }

        public ICollection<ProfileVersion> Versions { get; set; } = new List<ProfileVersion>();
    }
}
