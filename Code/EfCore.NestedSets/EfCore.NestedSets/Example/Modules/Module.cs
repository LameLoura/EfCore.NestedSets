using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EfCore.NestedSets.Tests
{
    public class Module
    {
        public int Id { get; set; }
        public string Label { get; set; }
        //public bool IsDeleted { get; set; }
        //public bool CreatedDate { get; set; }
        //public bool UpdatedDate { get; set; }
        //public int? EntryKey { get; set; }

        //public INode createNewNodeInstance(string label)
        //{
        //    return new Module() { Label = label };
        //}

        public override string ToString()
        {
            return Label;
        }
    }
}