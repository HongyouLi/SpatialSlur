﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using SpatialSlur.SlurCore;
using SpatialSlur.SlurData;

/*
 * Notes
 */

namespace SpatialSlur.SlurMesh
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class HeElementList<T> : IHeElementList<T>
        where T : HeElement, IHeElement
    {
        private const int _minCapacity = 4;

        private T[] _items;
        private int _count;
        private int _currTag = int.MinValue;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        internal HeElementList(int capacity)
        {
            _items = new T[Math.Max(capacity, _minCapacity)];
        }


        /// <summary>
        /// 
        /// </summary>
        internal int NextTag
        {
            get
            {
                if (_currTag == int.MaxValue) ResetTags();
                return ++_currTag;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public int Count
        {
            get { return _count; }
        }


        /// <summary>
        /// 
        /// </summary>
        public int Capacity
        {
            get { return _items.Length; }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                if (index >= _count)
                    throw new IndexOutOfRangeException();

                return _items[index];
            }
            internal set
            {
                if (index >= _count)
                    throw new IndexOutOfRangeException();

                _items[index] = value;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private void ResetTags()
        {
            _currTag = int.MinValue;

            for (int i = 0; i < _count; i++)
                _items[i].Tag = int.MinValue;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return _items[i];
        }
        

        /// <summary>
        /// Adds an element to the list.
        /// </summary>
        /// <param name="element"></param>
        internal void Add(T element)
        {
            element.Index = _count;
            element.Tag = _currTag;

            // resize if necessary
            if (_count == _items.Length)
                Array.Resize(ref _items, _items.Length << 1);

            _items[_count++] = element;
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int CountUnused()
        {
            int result = 0;

            for (int i = 0; i < _count; i++)
                if (_items[i].IsUnused) result++;

            return result;
        }


        /// <summary>
        /// Removes all unused elements in the list and re-indexes the remaining.
        /// Does not change the capacity of the list.
        /// If the list has any associated attributes, be sure to compact those first.
        /// </summary>
        public void Compact()
        {
            int marker = 0;

            for (int i = 0; i < _count; i++)
            {
                T element = _items[i];
                if (element.IsUnused) continue; // skip unused elements

                element.Index = marker;
                _items[marker++] = element;
            }
            
            Array.Clear(_items, marker, _count - marker);
            _count = marker;
        }


        /// <summary>
        /// Removes all attributes corresponding with unused elements.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="attributes"></param>
        public void CompactAttributes<U>(List<U> attributes)
        {
            int marker = SwimAttributes(attributes);
            attributes.RemoveRange(marker, attributes.Count - marker);
        }


        /// <summary>
        /// Moves attributes corresponding with used elements to the front of the given list.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="attributes"></param>
        public int SwimAttributes<U>(IList<U> attributes)
        {
            int marker = 0;

            for (int i = 0; i < _count; i++)
            {
                if (_items[i].IsUnused) continue; // skip unused elements
                attributes[marker++] = attributes[i];
            }

            return marker;
        }


        /// <summary>
        /// Reduces the capacity to twice the count.
        /// If the capacity is already less than twice the count, this does nothing.
        /// </summary>
        public void TrimExcess()
        {
            int max = Math.Max(_count << 1, _minCapacity);

            if (_items.Length > max)
                Array.Resize(ref _items, max);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="getKey"></param>
        public void Sort<K>(Func<T, K> getKey)
        {
            Sort(getKey, Comparer<K>.Default);
        }


        /// <summary>
        /// Performs an in-place stable sort of elements.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <param name="getKey"></param>
        /// <param name="keyComparer"></param>
        public virtual void Sort<K>(Func<T, K> getKey, IComparer<K> keyComparer)
        {
            int index = 0;

            // sort
            foreach (var t in this.OrderBy(getKey, keyComparer))
                this[index++] = t;

            // re-index
            for (int i = 0; i < _count; i++)
                _items[i].Index = i;
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public bool Contains(T element)
        {
            return element == _items[element.Index];
        }


        /// <summary>
        /// Throws an exception for elements that don't belong to this list.
        /// </summary>
        /// <param name="element"></param>
        internal void ContainsCheck(T element)
        {
            if (!Contains(element))
                throw new ArgumentException("The given element must belong to this mesh.");
        }


        /// <summary>
        /// Returns unique elements from the given collection (no duplicates).
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        public IEnumerable<T> GetDistinct(IEnumerable<T> elements)
        {
            foreach (var e in elements)
                ContainsCheck(e);

            return GetDistinctImpl(elements);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        internal IEnumerable<T> GetDistinctImpl(IEnumerable<T> elements)
        {
            int currTag = NextTag;

            foreach (var e in elements)
            {
                if (e.Tag == currTag) continue;
                e.Tag = currTag;
                yield return e;
            }
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementsA"></param>
        /// <param name="elementsB"></param>
        /// <returns></returns>
        public IEnumerable<T> GetUnion(IEnumerable<T> elementsA, IEnumerable<T> elementsB)
        {
            return GetDistinct(elementsA.Concat(elementsB));
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementsA"></param>
        /// <param name="elementsB"></param>
        /// <returns></returns>
        public IEnumerable<T> GetDifference(IEnumerable<T> elementsA, IEnumerable<T> elementsB)
        {
            foreach (var e in elementsA.Concat(elementsB))
                ContainsCheck(e);

            return GetDifferenceImpl(elementsA, elementsB);
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementsA"></param>
        /// <param name="elementsB"></param>
        /// <returns></returns>
        internal IEnumerable<T> GetDifferenceImpl(IEnumerable<T> elementsA, IEnumerable<T> elementsB)
        {
            int currTag = NextTag;

            // tag elements in A
            foreach (var eB in elementsB)
                eB.Tag = currTag;

            // return tagged elements in B
            foreach (var eA in elementsA)
                if (eA.Tag != currTag) yield return eA;
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementsA"></param>
        /// <param name="elementsB"></param>
        /// <returns></returns>
        public IEnumerable<T> GetIntersection(IEnumerable<T> elementsA, IEnumerable<T> elementsB)
        {
            foreach (var e in elementsA.Concat(elementsB))
                ContainsCheck(e);

            return GetIntersectionImpl(elementsA, elementsB);
        }


        /// <inheritdoc/>
        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementsA"></param>
        /// <param name="elementsB"></param>
        /// <returns></returns>
        internal IEnumerable<T> GetIntersectionImpl(IEnumerable<T> elementsA, IEnumerable<T> elementsB)
        {
            int currTag = NextTag;

            // tag elements in A
            foreach (var eA in elementsA)
                eA.Tag = currTag;

            // return tagged elements in B
            foreach (var eB in elementsB)
                if (eB.Tag == currTag) yield return eB;
        }


        #region Explicit interface implementations

        /// <summary>
        /// Explicit implementation of non-generic method.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator(); // call generic version
        }

        #endregion
    }
}
