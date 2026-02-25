using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAgent.Domain.Entities
{
    public static class VersionStatus
    {
        public const string Draft = "Draft";
        public const string Published = "Published";
        public const string Archived = "Archived";
    }

    public static class EntityCategory
    {
        public const string Fact = "fact";
        public const string Dimension = "dimension";
        public const string Bridge = "bridge";
        public const string Config = "config";
    }
}
