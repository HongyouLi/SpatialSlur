﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SpatialSlur.SlurCore;

/*
 * Notes
 */

namespace SpatialSlur.SlurMesh
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="E"></typeparam>
    [Serializable]
    public class HalfedgeList<E> : HeElementList<E>
        where E : Halfedge<E>, IHalfedge<E>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        internal HalfedgeList(int capacity)
            : base(capacity)
        {
        }


        /// <inheritdoc/>
        /// <summary>
        /// Halfedges are sorted pairwise where the key is taken from the first halfedge of each pair.
        /// </summary>
        public sealed override void Sort<K>(Func<E, K> getKey, IComparer<K> keyComparer)
        {
            int index = 0;

            // sort in pairs
            foreach (var he0 in this.TakeEveryNth(2).OrderBy(getKey, keyComparer))
            {
                this[index++] = he0;
                this[index++] = he0.Twin;
            }

            // re-index
            for (int i = 0; i < Count; i++)
                this[i].Index = i;
        }
    }
}
