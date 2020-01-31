﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace EfCore.NestedSets.Tests
{
    public class ModuleStructure : INestedSet<ModuleStructure, int, int?>
    {
        public int Id { get; set; }
        public ModuleStructure Parent { get; set; }
        public List<ModuleStructure> Children { get; set; }
        public List<ModuleStructure> Descendants { get; set; }
        public int? ParentId { get; set; }
        public int Level { get; set; }
        public int Left { get; set; }
        public int Right { get; set; }
        public string Name { get; set; }
        [NotMapped]
        public bool Moving { get; set; }
        public ModuleStructure Root { get; set; }
        public int? RootId { get; set; }

        public ModuleStructure() { }

        public ModuleStructure(string name, int? parentId, int level, int left, int right)
            : this(0, parentId, level, left, right, name)
        {
        }

        public ModuleStructure(int id, int? parentId, int level, int left, int right, string name)
        {
            Id = id;
            ParentId = parentId;
            Level = level;
            Left = left;
            Right = right;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}