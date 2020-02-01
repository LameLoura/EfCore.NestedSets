using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace EfCore.NestedSets
{
    public class NestedSetManager<TDbContext, TNodeStructure, TNode, TKey, TNullableKey>
        where TNodeStructure : class, INestedSet<TNodeStructure, TNode, TKey, TNullableKey>
        where TNode : class, INode, new()
        where TDbContext : DbContext
    {
        private readonly DbContext _db;
        private readonly DbSet<TNodeStructure> _nodeStructuresSet;
        private readonly DbSet<TNode> _nodesSet;


        private static IQueryable<TNodeStructure> QueryById(IQueryable<TNodeStructure> nodes, TKey id)
        {
            return nodes.Where(_PropertyEqualsExpression(nameof(INestedSet<TNodeStructure, TNode, TKey, TNullableKey>.Id), id));
        }

        private IQueryable<TNodeStructure> GetNodes(TNullableKey rootId)
        {
            return _nodeStructuresSet.Where(PropertyEqualsExpression(nameof(INestedSet<TNodeStructure, TNode, TKey, TNullableKey>.RootId), rootId));
        }

        public NestedSetManager(TDbContext dbContext, 
            Expression<Func<TDbContext, DbSet<TNodeStructure>>> nodesStructureSourceExpression,
            Expression<Func<TDbContext, DbSet<TNode>>> nodesSourceExpression)
        {
            _db = dbContext;
            var propertyInfo = new PropertySelectorVisitor(nodesStructureSourceExpression).Property;
            _nodeStructuresSet = (DbSet<TNodeStructure>)propertyInfo.GetValue(dbContext);


            var nodePropertyInfo = new PropertySelectorVisitor(nodesSourceExpression).Property;
            _nodesSet = (DbSet<TNode>)nodePropertyInfo.GetValue(dbContext);
            string potato = "portao";
        }

        private Expression<Func<TNodeStructure, bool>> PropertyEqualsExpression(string propertyName, TKey key)
        {
            return _PropertyEqualsExpression(propertyName, key);
        }

        private Expression<Func<TNodeStructure, bool>> PropertyEqualsExpression(string propertyName, TNullableKey key)
        {
            return _PropertyEqualsExpression(propertyName, key);
        }

        private static Expression<Func<TNodeStructure, bool>> _PropertyEqualsExpression<TField>(string propertyName, TField key)
        {
            var parameterExpression = Expression.Parameter(typeof(TNodeStructure), "entity");
            if (string.IsNullOrEmpty(propertyName))
                throw new NotSupportedException();
            return Expression.Lambda<Func<TNodeStructure, bool>>(
                Expression.Equal(Expression.Property(parameterExpression, typeof(TNodeStructure), propertyName), Expression.Convert(Expression.Constant(key), typeof(TField))),
                parameterExpression);
        }

        public List<TNodeStructure> Delete(TKey nodeId, bool soft = false)
        {
            var nodeToDelete = GetNode(nodeId);
            var nodeToDeleteLeft = nodeToDelete.Left;
            var difference = nodeToDelete.Right - nodeToDelete.Left + 1;
            var rootId = nodeToDelete.RootId;
            var deleted = GetNodes(rootId).Where(s => s.Left >= nodeToDelete.Left && s.Right <= nodeToDelete.Right).ToList();
            if (soft)
                foreach (var node in deleted)
                    node.Moving = true;
            else
                foreach (var node in deleted)
                    _nodeStructuresSet.Remove(node);
            var nodesToUpdate = GetNodes(rootId).Where(s => s.Left > nodeToDelete.Left || s.Right >= nodeToDelete.Left).ToList();
            foreach (var nodeToUpdate in nodesToUpdate)
            {
                if (nodeToUpdate.Moving)
                    continue;
                if (nodeToUpdate.Left >= nodeToDeleteLeft)
                    nodeToUpdate.Left -= difference;
                nodeToUpdate.Right -= difference;
            }
            var minLeft = deleted.Min(s => s.Left) - 1;
            // Reset to 1
            foreach (var deletedNode in deleted)
            {
                deletedNode.Left -= minLeft;
                deletedNode.Right -= minLeft;
                deletedNode.ParentId = default(TNullableKey);
                deletedNode.EntryKey = default(TNullableKey);
            }
            if (!soft)
            {
                _db.SaveChanges();
                foreach (var deletedSite in deleted)
                    deletedSite.Id = default(TKey);
            }
            return deleted;
        }

        public void MoveToParent(TKey nodeId, TNullableKey parentId,
            NestedSetInsertMode insertMode)
        {
            Move(nodeId, parentId, default(TNullableKey), insertMode);
        }

        public void MoveToSibling(TKey nodeId, TNullableKey siblingId,
            NestedSetInsertMode insertMode)
        {
            Move(nodeId, default(TNullableKey), siblingId, insertMode);
        }

        private void Move(TKey nodeId, TNullableKey toParentId, TNullableKey toSiblingId,
            NestedSetInsertMode insertMode)
        {
            //var node = _nodes.Single(KeyEqualsExpression(nodeId));
            var deletedNodes = Delete(nodeId);
            Insert(toParentId, toSiblingId, deletedNodes, insertMode);
        }

        //TODO change entryKey type to ME
        public TNodeStructure InsertRoot(TNodeStructure node, TNullableKey entryKey,
            NestedSetInsertMode insertMode)
        {
            node.EntryKey = entryKey;
            return Insert(default(TNullableKey), default(TNullableKey), new[] { node }, insertMode).First();
        }

        // Multiple insert - Has not been tested yet. Might not be fully supported.
        public List<TNodeStructure> InsertRoot(IEnumerable<TNodeStructure> nodeTree,
            NestedSetInsertMode insertMode)
        {
            return Insert(default(TNullableKey), default(TNullableKey), nodeTree, insertMode);
        }

        public TNodeStructure InsertBelow(TNullableKey parentId, TNodeStructure node,
            NestedSetInsertMode insertMode)
        {
            return Insert(parentId, default(TNullableKey), new[] { node }, insertMode).First();
        }

        public List<TNodeStructure> InsertBelow(TNullableKey parentId, IEnumerable<TNodeStructure> nodeTree,
            NestedSetInsertMode insertMode)
        {
            return Insert(parentId, default(TNullableKey), nodeTree, insertMode);
        }

        public TNodeStructure InsertNextTo(TNullableKey siblingId, TNodeStructure node,
            NestedSetInsertMode insertMode)
        {
            return Insert(default(TNullableKey), siblingId, new[] { node }, insertMode).First();
        }

        public TNodeStructure InsertNextTo(TNullableKey siblingId, List<TNodeStructure> nodeTree,
            NestedSetInsertMode insertMode)
        {
            return Insert(default(TNullableKey), siblingId, nodeTree, insertMode).First();
        }

        private List<TNodeStructure> Insert(TNullableKey parentId, TNullableKey siblingId, IEnumerable<TNodeStructure> nodeTree,
            NestedSetInsertMode insertMode)
        {
            //for all new node, create associated node instace
            foreach(TNodeStructure nodeStructure in nodeTree)
            {
                //if doesn't have a valid instance yet, create them
                if(!Equals(nodeStructure.NodeInstanceId, default(TKey)))
                {
                    //TODO provide the ID later
                    TNode item = new TNode  { Label = "Test" };
                    _nodesSet.Add(item);
                    _db.SaveChanges();  //TODO improve performance
                    nodeStructure.NodeInstanceId = item.Id;
                }
            }

            var nodeArray = nodeTree as TNodeStructure[] ?? nodeTree.ToArray();
            var lowestLeft = nodeArray.Min(n => n.Left);
            var highestRight = nodeArray.Max(n => n.Right);
            if (lowestLeft == 0 && highestRight == 0)
            {
                if (nodeArray.Length == 1)
                {
                    var node = nodeArray.Single();
                    node.Left = 1;
                    node.Right = 2;
                    lowestLeft = 1;
                    highestRight = 2;
                }
                else
                {
                    throw new ArgumentException("Node tree must have left right values", nameof(nodeTree));
                }
            }
            var difference = highestRight - lowestLeft;
            var nodeTreeRoot = nodeArray.Single(n => n.Left == lowestLeft);
            TNodeStructure parent = null;
            TNodeStructure sibling = null;
            var isRoot = Equals(parentId, default(TNullableKey)) && Equals(siblingId, default(TNullableKey));
            if (!Equals(parentId, default(TNullableKey)) &&
                insertMode == NestedSetInsertMode.Right)
            {
                parent = GetNode(parentId);
                if (parent == null)
                {
                    throw new ArgumentException(string.Format("Unable to find node parent with ID of {0}", parentId));
                }
                var parent1 = parent;
                var rightMostImmediateChild = GetNodes(parent.RootId)
                    .Where(s => s.Left >= parent1.Left && s.Right <= parent1.Right && s.Level == parent1.Level + 1)
                    .OrderByDescending(s => s.Right)
                    .ToList()
                    .FirstOrDefault(n => !n.Moving)
                    ;
                sibling = rightMostImmediateChild;
                if (sibling != null)
                {
                    siblingId = (TNullableKey)(object)sibling.Id;
                }
            }
            int? siblingLeft = null;
            int? siblingRight = null;
            var rootId = default(TNullableKey);
            var entryKey = default(TNullableKey);
            if (!Equals(siblingId, default(TNullableKey)))
            {
                if (sibling == null)
                {
                    sibling = GetNode(siblingId);
                }
                siblingLeft = sibling.Left;
                siblingRight = sibling.Right;
                parentId = sibling.ParentId;
                rootId = sibling.RootId;
                entryKey = sibling.EntryKey;
            }
            int? parentLeft = null;
            if (!Equals(parentId, default(TNullableKey)))
            {
                if (parent == null)
                {
                    parent = GetNode(parentId);
                }
                parentLeft = parent.Left;
                rootId = parent.RootId;
                entryKey = parent.EntryKey;
            }
            var minLevel = nodeArray.Min(n => n.Level);
            foreach (var node in nodeArray)
            {
                node.Level -= minLevel;
                if (parent != null)
                {
                    node.Level += parent.Level + 1;
                }
            }
            var left = 0;
            var right = 0;
            switch (insertMode)
            {
                case NestedSetInsertMode.Left:
                    {
                        IEnumerable<TNodeStructure> nodes;
                        if (sibling != null)
                        {
                            nodes = GetNodes(rootId)
                                .Where(s => s.Left >= siblingLeft || s.Right >= siblingRight).ToList()
                                .Where(n => !n.Moving)
                                .ToList();
                            left = sibling.Left;
                            right = sibling.Left + difference;
                            foreach (var nodeToUpdate in nodes)
                            {
                                if (nodeToUpdate.Left >= siblingLeft)
                                    nodeToUpdate.Left += difference + 1;
                                nodeToUpdate.Right += difference + 1;
                            }
                        }
                        else if (parent != null)
                        {
                            nodes = GetNodes(rootId).Where(s => s.Right >= parentLeft).ToList()
                                .Where(n => !n.Moving)
                                .ToList();
                            left = parent.Left + 1;
                            right = left + difference;
                            foreach (var nodeToUpdate in nodes)
                            {
                                if (nodeToUpdate.Left > parentLeft)
                                    nodeToUpdate.Left += difference + 1;
                                nodeToUpdate.Right += difference + 1;
                            }
                        }
                        else
                        {
                            left = 1;
                            right = 1 + difference;
                        }
                    }
                    break;
                case NestedSetInsertMode.Right:
                    {
                        List<TNodeStructure> nodes;
                        if (sibling != null)
                        {
                            nodes = GetNodes(rootId)
                                .Where(s => s.Left > siblingRight || s.Right > siblingRight)
                                .ToList()
                                .Where(n => !n.Moving)
                                .ToList();
                            left = sibling.Right + 1;
                            right = sibling.Right + 1 + difference;
                            foreach (var nodeToUpdate in nodes)
                            {
                                if (nodeToUpdate.Left > siblingLeft)
                                    nodeToUpdate.Left += difference + 1;
                                nodeToUpdate.Right += difference + 1;
                            }
                        }
                        else if (parent != null)
                        {
                            nodes = GetNodes(rootId)
                                .Where(s => s.Right >= parentLeft).ToList()
                                .Where(n => !n.Moving)
                                .ToList();
                            left = parent.Left + 1;
                            right = left + difference;
                            foreach (var nodeToUpdate in nodes)
                            {
                                if (nodeToUpdate.Left > parentLeft)
                                    nodeToUpdate.Left += difference + 1;
                                nodeToUpdate.Right += difference + 1;
                            }
                        }
                        else
                        {
                            left = 1;
                            right = 1 + difference;
                        }
                    }
                    break;
            }
            var leftChange = left - nodeTreeRoot.Left;
            var rightChange = right - nodeTreeRoot.Right;
            foreach (var node in nodeArray)
            {
                node.Left += leftChange;
                node.Right += rightChange;
            }
            nodeTreeRoot.ParentId = parentId;
            var newNodes = nodeArray.Where(n => !n.Moving).ToList();
            if (newNodes.Any())
            {
                _nodeStructuresSet.AddRange(newNodes);
            }
            var movingNodes = nodeArray.Where(n => n.Moving).ToList();
            foreach (var node in movingNodes)
            {
                node.Moving = false;
            }
            _db.SaveChanges();
            // Update the root ID
            if (isRoot)
            {
                nodeTreeRoot.RootId = ToNullableKey(nodeTreeRoot.Id);
                nodeTreeRoot.Root = nodeTreeRoot;
                nodeTreeRoot.EntryKey = nodeTreeRoot.EntryKey;
                _db.SaveChanges();
            }
            // insert a child node
            else if (Equals(rootId, default(TNullableKey)))
            {
                var rootIds = newNodes.Select(n => n.RootId).Distinct().ToArray();
                if (rootIds.Length > 1)
                {
                    throw new ArgumentException("Unable to identify root node ID of node tree as multiple have been supplied.");
                }
                if (Equals(rootId, default(TNullableKey)) &&
                    rootIds.Length == 0 || (rootIds.Length == 1 && Equals(rootIds[0], default(TNullableKey))))
                {
                    rootId = rootIds[0];
                    //nodeTreeRoot.RootId = rootId;//ToNullableKey(GetNodes(rootId).Single(n => n.Left == 1).Id);
                }
            }
            //if rootId exist, applies them to all of the new node
            if (!Equals(rootId, default(TNullableKey)))
            {
                foreach (var newNode in newNodes)
                {
                    newNode.RootId = rootId;
                    newNode.EntryKey = entryKey;
                }
                _db.SaveChanges();
            }
            else if (!isRoot)
            {
                throw new Exception("Unable to determine root ID of non-root node");
            }
            // Update the parent IDs now we have them
            foreach (var newNode in newNodes)
            {
                if (newNode != nodeTreeRoot)
                {
                    var path = GetPathToNode(newNode, newNodes).Reverse();
                    var current = newNode;
                    foreach (var ancestor in path)
                    {
                        current.ParentId = (TNullableKey)(object)ancestor.Id;
                        current = ancestor;
                    }
                }
            }
            _db.SaveChanges();
            return newNodes;
        }

        private static TNullableKey ToNullableKey(TKey id)
        {
            return (TNullableKey)(object)id;
        }

        /// <summary>
        /// Returns all descendants of a node
        /// </summary>
        /// <param name="nodeId">The node for which to find the path to</param>
        /// <returns></returns>
        public IQueryable<TNodeStructure> GetDescendants(TKey nodeId, int? depth = null)
        {
            var node = GetNodeData(nodeId);
            var query = _nodeStructuresSet.Where(n => n.Left > node.Left && n.Right < node.Right);
            if (depth.HasValue)
            {
                query = query.Where(n => n.Level <= node.Level + depth.Value);
            }
            return query;
        }

        private NodeData<TNullableKey> GetNodeData(TKey nodeId)
        {
            var node = QueryById(_nodeStructuresSet, nodeId)
                .Select(n => new NodeData<TNullableKey> {Level = n.Level, Left = n.Left, Right = n.Right, RootId = n.RootId}).Single();
            return node;
        }

        private class NodeData<TNullableKey>
        {
            public int Level { get; set; }
            public int Left { get; set; }
            public int Right { get; set; }
            public TNullableKey RootId { get; set; }
        }

        /// <summary>
        /// Returns the immediate children of a given node, i.e. its ancestors
        /// </summary>
        /// <param name="nodeId">The node for which to find the path to</param>
        /// <returns></returns>
        public IQueryable<TNodeStructure> GetImmediateChildren(TKey nodeId)
        {
            return _nodeStructuresSet.Where(PropertyEqualsExpression(nameof(INestedSet<TNodeStructure, TNode, TKey, TNullableKey>.ParentId), (TNullableKey)(object)nodeId));
        }

        /// <summary>
        /// Returns the path to a given node, i.e. its ancestors
        /// </summary>
        /// <param name="nodeId">The node for which to find the path to</param>
        /// <returns></returns>
        public IOrderedEnumerable<TNodeStructure> GetPathToNode(TKey nodeId)
        {
            var node = GetNodeData(nodeId);
            return GetPathToNode(node, GetNodes(node.RootId));
        }

        /// <summary>
        /// Returns the path to a given node, i.e. its ancestors
        /// </summary>
        /// <param name="node">The node for which to find the path to</param>
        /// <returns></returns>
        public IOrderedEnumerable<TNodeStructure> GetPathToNode(TNodeStructure node)
        {
            return GetPathToNode(node, GetNodes(node.RootId));
        }

        /// <summary>
        /// Returns the path to a given node within a set of nodes, i.e. its ancestors
        /// </summary>
        /// <param name="node">The node for which to find the path to</param>
        /// <param name="nodeSet">The set of nodes to limit the search to</param>
        /// <returns></returns>
        public static IOrderedEnumerable<TNodeStructure> GetPathToNode(TNodeStructure node, IEnumerable<TNodeStructure> nodeSet)
        {
            return GetPathToNode(AsNodeData(node), nodeSet);
        }

        private static NodeData<TNullableKey> AsNodeData(TNodeStructure node)
        {
            return new NodeData<TNullableKey> {Left = node.Left, Right = node.Right, RootId = node.RootId};
        }

        /// <summary>
        /// Returns the path to a given node within a set of nodes, i.e. its ancestors
        /// </summary>
        /// <param name="node">The node for which to find the path to</param>
        /// <param name="nodeSet">The set of nodes to limit the search to</param>
        /// <returns></returns>
        private static IOrderedEnumerable<TNodeStructure> GetPathToNode(NodeData<TNullableKey> node, IEnumerable<TNodeStructure> nodeSet)
        {
            return nodeSet
                    .Where(n => n.Left < node.Left && n.Right > node.Right)
                    .OrderBy(n => n.Left)
                ;
        }

        private TNodeStructure GetNode(TNullableKey id)
        {
            return GetNode((TKey)(object)id);
        }

        private TNodeStructure GetNode(TKey id)
        {
            return _nodeStructuresSet.Single(PropertyEqualsExpression(nameof(INestedSet<TNodeStructure, TNode, TKey, TNullableKey>.Id), id));
        }
    }
}