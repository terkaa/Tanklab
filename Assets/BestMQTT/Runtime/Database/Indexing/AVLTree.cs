using System;
using System.Collections.Generic;

namespace BestMQTT.Databases.Indexing
{
    public sealed class Node<Key, Value>
    {
        public Node<Key, Value> Parent, Left, Right;
        public KeyValuePair<Key, List<Value>> Values;

        public int Height /*{ get; private set; } //*/{ get { return Math.Max(this.LeftHeight, this.RightHeight) + 1; } }

        public int BalanceFactor { get { return this.LeftHeight - this.RightHeight; } }
        public int LeftHeight { get { return this.Left == null ? -1 : this.Left.Height; } }
        public int RightHeight { get { return this.Right == null ? -1 : this.Right.Height; } }

        public bool IsRoot { get { return this.Parent == null; } }

        public int ChildCount { get { return (this.Left == null ? 0 : 1) + (this.Right == null ? 0 : 1); } }

        public Node(Node<Key, Value> parent, Key key, Value value)
        {
            this.Parent = parent;
            this.Values = new KeyValuePair<Key, List<Value>>(key, new List<Value>(1));
            this.Values.Value.Add(value);

            //this.Height = -1;
        }

        public void RecalculateHeight()
        {
            //int oldHeight = this.Height;
            //this.Height = Math.Max(this.LeftHeight, this.RightHeight) + 1;
            //
            //if (oldHeight != this.Height && this.Parent != null)
            //    this.Parent.RecalculateHeight();
        }

        public override string ToString()
        {
            return $"{this.Left?.Values.Key.ToString()} <- {this.Values.Key.ToString()} -> {this.Right?.Values.Key.ToString()}";
        }
    }

    // https://www.codesdope.com/course/data-structures-avl-trees/

    public sealed class AVLTree<Key, Value>
    {
        public int ElemCount { get; private set; }
        public int NodeCount { get; private set; }
        public IComparer<Key> Comparer;

        public Node<Key, Value> root = null;

        public AVLTree(IComparer<Key> comparer)
        {
            this.Comparer = comparer;
        }

        public void Add(Key key, Value item, bool clearValues = false)
        {
            if (this.root == null) {
                this.NodeCount++;
                this.ElemCount++;
                this.root = new Node<Key, Value>(null, key, item);
                return;
            }

            var current = this.root;
            do
            {
                // +--------------------+-----------------------+
                // |        Value       |     Meaning           |
                // +--------------------+-----------------------+
                // | Less than zero     |  x is less than y.    |
                // | Zero               |  x equals y.          |
                // | Greater than zero  |  x is greater than y. |
                // +--------------------------------------------+
                int comp = this.Comparer.Compare(/*x: */ current.Values.Key, /*y: */ key);

                // equals
                if (comp == 0)
                {
                    if (clearValues)
                    {
                        this.ElemCount -= current.Values.Value.Count;
                        current.Values.Value.Clear();
                    }

                    current.Values.Value.Add(item);
                    break;
                }

                // current's key > key
                if (comp > 0)
                {
                    // insert new node
                    if (current.Left == null)
                    {
                        current.Left = new Node<Key, Value>(current, key, item);
                        current.Left.RecalculateHeight();

                        current = current.Left;

                        this.NodeCount++;
                        break;
                    }
                    else
                    {
                        current = current.Left;
                        continue;
                    }
                }

                // current's key < key
                if (comp < 0)
                {
                    // insert new node
                    if (current.Right == null)
                    {
                        current.Right = new Node<Key, Value>(current, key, item);
                        current.Right.RecalculateHeight();

                        current = current.Right;

                        this.NodeCount++;
                        break;
                    }
                    else
                    {
                        current = current.Right;
                        continue;
                    }
                }
            } while (true);

            this.ElemCount++;

            while (RebalanceFrom(current) != null)
                ;
        }

        enum Side
        {
            Left,
            Right
        }

        List<Side> path = new List<Side>(2);

        private Node<Key, Value> RebalanceFrom(Node<Key, Value> newNode)
        {
            if (newNode.IsRoot || newNode.Parent.IsRoot)
                return null;

            path.Clear();

            // find first unbalanced node or exit when found the root node (root still can be unbalanced!)
            var current = newNode;
            while (!current.IsRoot && Math.Abs(current.BalanceFactor) <= 1)
            {
                if (current.Parent.Left == current)
                    path.Add(Side.Left);
                else
                    path.Add(Side.Right);

                current = current.Parent;
            }

            // it's a balanced tree
            if (Math.Abs(current.BalanceFactor) <= 1)
                return null;

            Side last = path[path.Count - 1];// path[path.StartIdx];
            Side prev = path[path.Count - 2];// path[path.EndIdx];

            if (last == Side.Left && prev == Side.Left)
            {
                // insertion to a left child of a left child
                RotateRight(current);
            }
            else if (last == Side.Right && prev == Side.Right)
            {
                // insertion to a right child of a right child
                RotateLeft(current);
            }
            else if (last == Side.Right && prev == Side.Left)
            {
                // insertion to a left child of a right child
                RotateRight(current.Right);
                RotateLeft(current);
            }
            else if (last == Side.Left && prev == Side.Right)
            {
                // insertion to a right child of a left child
                RotateLeft(current.Left);
                RotateRight(current);
            }

            return current;
        }

        public void Clear()
        {
            this.root = null;
            this.ElemCount = 0;
            this.NodeCount = 0;
        }

        public List<Value> Remove(Key key)
        {
            if (this.root == null)
                return null;

            var current = this.root;
            do
            {
                int comp = this.Comparer.Compare(current.Values.Key, key);

                // equals
                if (comp == 0)
                {
                    this.NodeCount--;
                    this.ElemCount -= current.Values.Value.Count;

                    // remove current node from the tree
                    RemoveNode(current);

                    return new List<Value>(current.Values.Value);
                }

                // current's key > key
                if (comp > 0)
                {
                    if (current.Left == null)
                        return null;
                    else
                    {
                        current = current.Left;
                        continue;
                    }
                }

                // current's key < key
                if (comp < 0)
                {
                    if (current.Right == null)
                        return null;
                    else
                    {
                        current = current.Right;
                        continue;
                    }
                }
            } while (true);
        }

        public void Remove(Key key, Value value)
        {
            if (this.root == null)
                return;

            var current = this.root;
            do
            {
                int comp = this.Comparer.Compare(current.Values.Key, key);

                // equals
                if (comp == 0)
                {
                    if (current.Values.Value.Remove(value))
                        this.ElemCount--;

                    if (current.Values.Value.Count == 0)
                    {
                        // remove current node from the tree
                        RemoveNode(current);

                        this.NodeCount--;
                    }

                    return;
                }

                // current's key > key
                if (comp > 0)
                {
                    if (current.Left == null)
                        return ;
                    else
                    {
                        current = current.Left;
                        continue;
                    }
                }

                // current's key < key
                if (comp < 0)
                {
                    if (current.Right == null)
                        return ;
                    else
                    {
                        current = current.Right;
                        continue;
                    }
                }
            } while (true);
        }

        private void RemoveNode(Node<Key, Value> node)
        {
            var parent = node.Parent;
            Side side = parent?.Left == node ? Side.Left : Side.Right;
            int childCount = node.ChildCount;

            var testForRebalanceNode = parent;

            switch(childCount)
            {
                case 0:
                    // node has no child

                    if (parent == null)
                    {
                        this.root = null;
                    }
                    else
                    {
                        if (parent.Left == node)
                            parent.Left = null;
                        else
                            parent.Right = null;
                    }

                    node.Parent = null;
                    break;

                case 1:
                    // re-parent the only child

                    var child = node.Left ?? node.Right;

                    if (parent == null)
                    {
                        this.root = child;
                        this.root.Parent = null;
                    }
                    else
                    {
                        if (parent.Left == node)
                            parent.Left = child;
                        else
                            parent.Right = child;
                        child.Parent = parent;
                    }
                    break;

                default:
                    // two child

                    // 1: Replace 20 with 25
                    //
                    //      20                     20
                    //  15      25             15      25
                    //                                     30
                    //  

                    // 2: Replace 20 with 22
                    // 
                    //      20
                    //  15      25
                    //       22

                    // 3: Re-parent 23 for 25, replace 20 with 22
                    //
                    //      20
                    //  15      25
                    //       22
                    //          23

                    // Cases 1 and 3 are the same, both 25 and 22 has a right child. But in case 3, 22 isn't first child of 20!

                    var nodeToReplaceWith = FindMin(node.Right);

                    testForRebalanceNode = nodeToReplaceWith;
                    side = Side.Right;

                    // re-parent 23 in case 3:
                    if (nodeToReplaceWith.Parent != node)
                    {
                        testForRebalanceNode = nodeToReplaceWith.Parent;
                        if (nodeToReplaceWith.Parent.Left == nodeToReplaceWith)
                        {
                            nodeToReplaceWith.Parent.Left = nodeToReplaceWith.Right;
                            side = Side.Left;
                        }
                        else
                        {
                            nodeToReplaceWith.Parent.Right = nodeToReplaceWith.Right;
                            side = Side.Right;
                        }

                        if (nodeToReplaceWith.Right != null)
                            nodeToReplaceWith.Right.Parent = nodeToReplaceWith.Parent;
                    }

                    if (parent == null)
                        this.root = nodeToReplaceWith;
                    else
                    {
                        if (parent.Left == node)
                            parent.Left = nodeToReplaceWith;
                        else
                            parent.Right = nodeToReplaceWith;
                    }
                    nodeToReplaceWith.Parent = parent;

                    nodeToReplaceWith.Left = node.Left;
                    node.Left.Parent = nodeToReplaceWith;

                    if (node.Right != nodeToReplaceWith)
                    {
                        nodeToReplaceWith.Right = node.Right;
                        node.Right.Parent = nodeToReplaceWith;
                    }
                    else
                        nodeToReplaceWith.Right = null;

                    break;
            }

            while (RebalanceForRemoval(testForRebalanceNode, side) != null)
                ;
        }

        private Node<Key, Value> RebalanceForRemoval(Node<Key, Value> removedParentNode, Side side)
        {
            if (removedParentNode == null)
                return null;

            path.Clear();
            path.Add(side);

            // find first unbalanced node or exit when found the root node (root still can be unbalanced!)
            var current = removedParentNode;
            while (!current.IsRoot && Math.Abs(current.BalanceFactor) <= 1)
            {
                if (current.Parent.Left == current)
                    path.Add(Side.Left);
                else
                    path.Add(Side.Right);

                current = current.Parent;
            }

            // it's a balanced tree
            if (Math.Abs(current.BalanceFactor) <= 1)
                return null;

            // from what direction we came from
            Side fromDirection = path[path.Count - 1];

            // check weather it's an inside or outside case
            switch (fromDirection)
            {
                case Side.Right:
                    {
                        bool isOutside = current.Left.LeftHeight >= current.Left.RightHeight;
                        if (isOutside)
                            RotateRight(current);
                        else
                        {
                            RotateLeft(current.Left);
                            RotateRight(current);
                        }
                    }
                    break;

                case Side.Left:
                    {
                        bool isOutside = current.Right.RightHeight >= current.Right.LeftHeight;
                        if (isOutside)
                            RotateLeft(current);
                        else
                        {
                            RotateRight(current.Right);
                            RotateLeft(current);
                        }
                    }
                    break;
            }

            return current;
        }

        private Node<Key, Value> FindMin(Node<Key, Value> node)
        {
            var current = node;

            while (current.Left != null)
                current = current.Left;

            return current;
        }

        private Node<Key, Value> FindMax(Node<Key, Value> node)
        {
            var current = node;
            while (current.Right != null)
                current = current.Right;

            return current;
        }

        public List<Value> Find(Key key) {
            if (this.root == null)
                return null;

            var current = this.root;
            do
            {
                int comp = this.Comparer.Compare(current.Values.Key, key);

                // equals
                if (comp == 0)
                    return new List<Value>(current.Values.Value);

                // current's key > key
                if (comp > 0)
                {
                    if (current.Left == null)
                        return null;
                    else
                    {
                        current = current.Left;
                        continue;
                    }
                }

                // current's key < key
                if (comp < 0)
                {
                    if (current.Right == null)
                        return null;
                    else
                    {
                        current = current.Right;
                        continue;
                    }
                }
            } while (true);
        }

        public bool ContainsKey(Key key)
        {
            if (this.root == null)
                return false;

            var current = this.root;
            do
            {
                int comp = this.Comparer.Compare(current.Values.Key, key);

                // equals
                if (comp == 0)
                    return true;

                // current's key > key
                if (comp > 0)
                {
                    if (current.Left == null)
                        return false;
                    else
                    {
                        current = current.Left;
                        continue;
                    }
                }

                // current's key < key
                if (comp < 0)
                {
                    if (current.Right == null)
                        return false;
                    else
                    {
                        current = current.Right;
                        continue;
                    }
                }
            } while (true);
        }

        private Node<Key, Value> RotateRight(Node<Key, Value> current)
        {
            // Current\        
            //          20              15
            //      15              10      20
            //  10      ?                 ?
            var parent = current.Parent;
            var leftChild = current.Left;

            // re-parent left child
            if (parent != null)
            {
                if (parent.Left == current)
                    parent.Left = leftChild;
                else
                    parent.Right = leftChild;
            }
            else
                this.root = leftChild;
            leftChild.Parent = parent;

            // re-parent left child's right child
            if (leftChild.Right != null)
                leftChild.Right.Parent = current;
            current.Left = leftChild.Right;

            // re-parent current
            current.Parent = leftChild;
            leftChild.Right = current;

            current.RecalculateHeight();
            leftChild.RecalculateHeight();

            // return with the node that took the place of current
            return leftChild;
        }

        private Node<Key, Value> RotateLeft(Node<Key, Value> current)
        {
            //    /Current        
            //  20              15        
            //     15       20      10
            //   ?    10       ?
            var parent = current.Parent;
            var rightChild = current.Right;

            // re-parent right child
            if (parent != null)
            {
                if (parent.Left == current)
                    parent.Left = rightChild;
                else
                    parent.Right = rightChild;
            }
            else
                this.root = rightChild;
            rightChild.Parent = parent;

            // re-parent right child's left child
            if (rightChild.Left != null)
                rightChild.Left.Parent = current;
            current.Right = rightChild.Left;

            // re-parent current
            current.Parent = rightChild;
            rightChild.Left = current;

            current.RecalculateHeight();
            rightChild.RecalculateHeight();

            return rightChild;
        }

    }
}
