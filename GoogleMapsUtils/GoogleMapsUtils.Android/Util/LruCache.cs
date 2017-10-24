using System;
using System.Collections.Generic;

//The MIT License(MIT)

//Copyright(c) 2015 Avetis Ghukasyan

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

namespace GoogleMapsUtils.Android.Util
{
    internal class LruCache<TKey, TValue> where TValue : class
    {
        private readonly int _maxCapacity = 0;
        private readonly Dictionary<TKey, Node<TValue, TKey>> _lruCache;
        private Node<TValue, TKey> _head = null;
        private Node<TValue, TKey> _tail = null;

        public LruCache(int maxCapacity)
        {
            _maxCapacity = maxCapacity;
            _lruCache = new Dictionary<TKey, Node<TValue, TKey>>();
        }

        public void Put(TKey key, TValue value)
        {
            if(_lruCache.ContainsKey(key))
            {
                MakeMostRecentlyUsed(_lruCache[key]);
            }

            if(_lruCache.Count >= _maxCapacity)
            {
                RemoveLeastRecentlyUsed();
            }

            var insertedNode = new Node<TValue, TKey>(value, key);

            if(_head == null)
            {
                _head = insertedNode;
                _tail = _head;
            }
            else
            {
                MakeMostRecentlyUsed(insertedNode);
            }

            _lruCache.Add(key, insertedNode);
        }

        public TValue Get(TKey key)
        {
            if(!_lruCache.ContainsKey(key))
            {
                return null;
            }

            MakeMostRecentlyUsed(_lruCache[key]);
            var node = _lruCache[key];
            return node.Data;
        }

        public int Size()
        {
            return _lruCache.Count;
        }

        public void EvictAll()
        {
            _lruCache.Clear();
        }

        private void RemoveLeastRecentlyUsed()
        {
            _lruCache.Remove(_tail.Key);
            _tail.Previous.Next = null;
            _tail = _tail.Previous;
        }

        private void MakeMostRecentlyUsed(Node<TValue, TKey> foundItem)
        {
            // newly inserted item bring to top
            if (foundItem.Next == null && foundItem.Previous == null)
            {
                foundItem.Next = _head;
                _head.Previous = foundItem;

                if(_head.Next == null)
                {
                    _tail = _head;
                }

                _head = foundItem;
            }
            // if it is the tail then bring it to the top
            else if (foundItem.Next == null && foundItem.Previous != null)
            {
                foundItem.Previous.Next = null;
                _tail = foundItem.Previous;
                foundItem.Next = _head;
                _head.Previous = foundItem;
                _head = foundItem;
            }
            // If it is an element in between then bring it to the top
            else if (foundItem.Next != null && foundItem.Previous != null)
            {
                foundItem.Previous.Next = foundItem.Next;
                foundItem.Next.Previous = foundItem.Previous;
                foundItem.Next = _head;
                _head.Previous = foundItem;
                _head = foundItem;
            }

            // else it is already at the head
        }

        private class Node<TData, TNodeKey>
        {
            public Node(TData data, TNodeKey key)
            {
                Data = data;
                Key = key;
            }

            public TData Data { get; private set; }
            public TNodeKey Key { get; private set; }
            public Node<TData, TNodeKey> Previous { get; set; }
            public Node<TData, TNodeKey> Next { get; set; }
        }
    }
}
