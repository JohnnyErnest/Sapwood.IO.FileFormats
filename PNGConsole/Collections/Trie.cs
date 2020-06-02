using System;
using System.Collections.Generic;
using System.Text;

namespace Sapwood.IO.FileFormats.Collections
{
    public class Trie<KeyType, ValueType> where ValueType : IComparable
    {
        public class Node
        {
            public Node LeftChild { get; set; }
            public Node RightChild { get; set; }
            public Node Parent { get; set; }
            public KeyType Key { get; set; }
            public ValueType Value { get; set; }
        }

        public Node RootNode { get; set; }
    }

    public class BinaryTrie
    {
        public Trie<int, int> TrieCollection { get; set; }
        public BinaryTrie(int levels)
        {
            TrieCollection = new Trie<int, int>();
            Trie<int, int>.Node rootNode = new Trie<int, int>.Node();
            rootNode.Key = -1;
            rootNode.Value = -1;
            rootNode.Parent = null;
            rootNode.LeftChild = new Trie<int, int>.Node() { Key = 0 };
            rootNode.RightChild = new Trie<int, int>.Node() { Key = 1 };
            for(int z=1;z<=levels;z++)
            {

            }
        }
    }
}