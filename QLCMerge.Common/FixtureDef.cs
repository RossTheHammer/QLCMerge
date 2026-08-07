using System;
using System.Collections.Generic;
using System.Text;

namespace QLCMerge.Common
{
    public class FixtureDef
    {
        public int? Id { get; set; }
        public int? MapTo { get; set; }
        public string? Name { get; set; }
        public string? Rename { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public int? Address { get; set; }
        public int? Channels { get; set; }
        public string? Inner { get; set; }

    }
}
