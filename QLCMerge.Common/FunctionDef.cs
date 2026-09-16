using System;
using System.Collections.Generic;
using System.Text;

namespace QLCMerge.Common
{
    public class FunctionDef
    {
        public int? Id { get; set; }
        public int? MapTo { get; set; }
        public int? RefId { get; set; }
        public string Name { get; set; }
        public string? Rename { get; set; }
        public string ElemType { get; set; }
        public string? Inner { get; set; }

        public IDictionary<int, FunctionDef> Children { get; } 
        
        /// <summary>
        /// If it has been matched to the other function set
        /// </summary>
        public DefinitionMatchType Matched { get; set; } = DefinitionMatchType.None;
        
        /// <summary>
        /// Whether it needs to be persisted i.e. was added new here
        /// </summary>
        public bool Pending { get; set; } = false;

        public FunctionDef(string elemType, string name, int? id = null, string? inner = null, int? mapTo = null, string? rename = null)
        {
            Id = id;
            MapTo = mapTo;
            Name = name;
            Rename = rename;
            ElemType = elemType;
            Inner = inner;

            Children = new Dictionary<int, FunctionDef>();
        }
    }

    public enum DefinitionMatchType
    {
        None = 0,
        Matched,
        NameChange,
        Modified,
        Divergent
    }
}
