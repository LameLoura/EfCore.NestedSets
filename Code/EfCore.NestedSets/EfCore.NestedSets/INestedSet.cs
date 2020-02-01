using System.Collections.Generic;

namespace EfCore.NestedSets
{
    public interface INestedSet<TNodeStructure, TNode, TKey, TNullableKey>
        where TNodeStructure : INestedSet<TNodeStructure, TNode, TKey, TNullableKey> // circular AF
    {
        TKey Id { get; set; }
        int? NodeInstanceId { get; set; } //TODO change to TKey
        TNode NodeInstance { get; set; }
        TNodeStructure Parent { get; set; }
        TNullableKey ParentId { get; set; }
        int Level { get; set; }
        int Left { get; set; }
        int Right { get; set; }
        bool Moving { get; set; }
        TNodeStructure Root { get; set; }
        TNullableKey RootId { get; set; }
        List<TNodeStructure> Children { get; set; }
        List<TNodeStructure> Descendants { get; set; }
        TNullableKey EntryKey { get; set; }
    }
}