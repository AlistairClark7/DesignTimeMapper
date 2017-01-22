using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace DesignTimeMapper.Engine.Model
{
    [DebuggerDisplay("Value = {Value}")]
    public class MapTreeNode<T>
    {
        private readonly T _value;
        private readonly List<MapTreeNode<T>> _children = new List<MapTreeNode<T>>();

        public MapTreeNode(T value)
        {
            _value = value;
        }

        public MapTreeNode<T> this[int i]
        {
            get { return _children[i]; }
        }

        public MapTreeNode<T> Parent { get; private set; }

        public T Value { get { return _value; } }

        public MapTreeNode<T> MapsTo { get; private set; }

        public void AddMapping(MapTreeNode<T> mapped)
        {
            MapsTo = mapped;
            mapped.MapsTo = this;
        }

        public ReadOnlyCollection<MapTreeNode<T>> Children
        {
            get { return _children.AsReadOnly(); }
        }

        public MapTreeNode<T> AddChild(T value)
        {
            var node = new MapTreeNode<T>(value) { Parent = this };
            _children.Add(node);
            return node;
        }

        public MapTreeNode<T> AddOrGetChild(T value)
        {
            foreach (var mapTreeNode in Children)
            {
                if (mapTreeNode.Value.Equals(value))
                    return mapTreeNode;
            }

            var node = new MapTreeNode<T>(value) { Parent = this };
            _children.Add(node);
            return node;
        }

        public MapTreeNode<T>[] AddChildren(params T[] values)
        {
            return values.Select(AddChild).ToArray();
        }

        public bool RemoveChild(MapTreeNode<T> node)
        {
            return _children.Remove(node);
        }

        public void Traverse(Action<T> action)
        {
            action(Value);
            foreach (var child in _children)
                child.Traverse(action);
        }
        
        public void TraverseAncestors(Action<T> action)
        {
            if(Value!= null)
                action(Value);

            Parent?.TraverseAncestors(action);
        }

        public IEnumerable<T> Flatten()
        {
            return new[] { Value }.Union(_children.SelectMany(x => x.Flatten()));
        }
    }
}