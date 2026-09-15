using QLCMerge.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace QLCMergeUI
{
    public class TreeNodeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? Folder { get; set; }
        public DataTemplate? Leaf { get; set; }
        public DataTemplate Default { get; set; } = new DataTemplate();

        //public TreeNodeTemplateSelector() : base()
        //{
        //    Default = new DataTemplate();
        //}

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is FunctionFolder _)
            {
                return Folder ?? Default;
            }
            else if (item is FunctionDef _)
            {
                return Leaf ?? Default;
            }
            return Default;
        }
    }
}
