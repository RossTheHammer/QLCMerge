using System;
using System.Collections.Generic;
using System.Text;

namespace QLCMerge.Common
{
    public class FunctionDef
    {
        public int? Id { get; set; }
        public int? MapTo { get; set; }
        public string Name { get; set; }
        public string? Rename { get; set; }
        public string ElemType { get; set; }
        public string? Inner { get; set; }
        public DefinitionMatchType Matched { get; set; }
        public FunctionDef(string elemType, string name, int? id = null, string? inner = null, int? mapTo = null, string? rename = null)
        {
            Id = id;
            MapTo = mapTo;
            Name = name;
            Rename = rename;
            ElemType = elemType;
            Inner = inner;
        }

        public IList<FunctionDef> Children => new List<FunctionDef>();
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
