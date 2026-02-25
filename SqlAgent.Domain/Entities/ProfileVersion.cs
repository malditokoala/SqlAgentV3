using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public class ProfileVersion
    {
        public Guid Id { get; set; }
        public Guid ProfileId { get; set; }
        public string VersionName { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft"; // Draft, Published, Archived

        public Profile Profile { get; set; } = null!;
        public DataSource DataSource { get; set; } = null!;
        public ICollection<Entity> Entities { get; set; } = new List<Entity>();
        public ICollection<Relationship> Relationships { get; set; } = new List<Relationship>();
        public ICollection<Metric> Metrics { get; set; } = new List<Metric>();
        public ICollection<Policy> Policies { get; set; } = new List<Policy>();
    }
}

