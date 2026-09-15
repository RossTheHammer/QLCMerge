using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace QLCMerge.Common
{
    public class FunctionFolder : INode<int, FunctionDef>
    {

        //public string Name { get; set; }
        //public virtual IDictionary<int,FunctionDef> FunctionDefs { get; set; } = new ObservableDictionary<int,FunctionDef>();

        public string Name { get; set; } = "";

        public IDictionary<int, FunctionDef> Children {  get; set; } = new ObservableDictionary<int, FunctionDef>();

        public bool IsLeaf => false;
    }
}
