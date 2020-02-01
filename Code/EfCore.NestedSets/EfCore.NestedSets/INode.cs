using System;
using System.Collections.Generic;
using System.Text;

namespace EfCore.NestedSets
{
    public interface INode
    {
       // INode createNewNodeInstance(string label);
        int Id { get; set; }
        string Label { get; set; }
    }
}
