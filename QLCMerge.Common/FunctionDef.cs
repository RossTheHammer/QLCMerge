using System;
using System.Collections.Generic;
using System.Text;

namespace QLCMerge.Common
{
    public class FunctionDef
    {
        public int Id { get; set; }
        public int? MapTo { get; set; }
        public string Name { get; set; }
        public string? Rename { get; set; }
        public string ElemType { get; set; }
        public string Inner { get; set; }
        public FunctionDef(int id, string name, string elemType, string inner, int? mapTo = null, string? rename = null)
        {
            Id = id;
            MapTo = mapTo;
            Name = name;
            Rename = rename;
            ElemType = elemType;
            Inner = inner;
        }
    }
}
