using System;
using System.Collections.Generic;
using System.Text;

namespace QLCMerge.Common
{
    public interface INode<T,U>
    {
        public string Name { get; }
        public IDictionary<T,U> Children { get; }
        public bool IsLeaf { get; }
    }
}
