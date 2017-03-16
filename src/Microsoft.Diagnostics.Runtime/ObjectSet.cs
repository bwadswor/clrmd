﻿using System.Collections;

namespace Microsoft.Diagnostics.Runtime
{
    /// <summary>
    /// This class is a HashSet of ulong for object addresses.
    /// </summary>
    public class ObjectSet
    {
        /// <summary>
        /// The ClrHeap this is an object set over.
        /// </summary>
        protected ClrHeap _heap;

        /// <summary>
        /// The minimum object size for this particular heap.
        /// </summary>
        protected readonly int _minObjSize;

        /// <summary>
        /// The collection of segments and associated objects.
        /// </summary>
        protected HeapHashSegment[] _segments;

        /// <summary>
        /// The last segment found.
        /// </summary>
        protected HeapHashSegment _lastSegment;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="heap">A ClrHeap to add objects from.</param>
        public ObjectSet(ClrHeap heap)
        {
            _heap = heap;
            _minObjSize = heap.PointerSize * 3;

            _segments = new HeapHashSegment[_heap.Segments.Count];

            for (int i = 0; i < _segments.Length; i++)
            {
                ulong start = _heap.Segments[i].Start;
                ulong end = _heap.Segments[i].End;
                _segments[i] = new HeapHashSegment()
                {
                    StartAddress = start,
                    EndAddress = end,
                    Objects = new BitArray(checked((int)(end - start) / _minObjSize), false)
                };
            }
        }

        /// <summary>
        /// Returns true if this set contains the given object, false otherwise.  The behavior of this function is undefined if
        /// obj lies outside the GC heap.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if this set contains the given object, false otherwise.</returns>
        public virtual bool Contains(ulong obj)
        {
            if (GetSegment(obj, out HeapHashSegment seg))
            {
                int offset = GetOffset(obj, seg);
                return seg.Objects[offset];
            }

            return false;
        }

        /// <summary>
        /// Adds the given object to the set.
        /// </summary>
        /// <param name="obj">The object to add to the set.</param>
        public virtual void Add(ulong obj)
        {
            if (GetSegment(obj, out HeapHashSegment seg))
            {
                int offset = GetOffset(obj, seg);
                seg.Objects.Set(offset, true);
            }
        }


        /// <summary>
        /// Adds the given object to the set.  Returns true if the value was changed, returns false if the object was already in the set.
        /// </summary>
        /// <param name="obj">The object to add to the set.</param>
        /// <returns>True if the value was changed, returns false if the object was already in the set.</returns>
        public virtual bool TryAdd(ulong obj)
        {
            if (GetSegment(obj, out HeapHashSegment seg))
            {
                int offset = GetOffset(obj, seg);
                if (seg.Objects[offset])
                {
                    return false;
                }
                else
                {
                    seg.Objects.Set(offset, true);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the given object from the set.
        /// </summary>
        /// <param name="obj">The object to remove from the set.</param>
        public virtual void Remove(ulong obj)
        {
            if (GetSegment(obj, out HeapHashSegment seg))
            {
                int offset = GetOffset(obj, seg);
                seg.Objects.Set(offset, false);
            }
        }

        /// <summary>
        /// Empties the set.
        /// </summary>
        public virtual void Clear()
        {
            for (int i = 0; i < _segments.Length; i++)
                _segments[i].Objects.SetAll(false);
        }


        private int GetOffset(ulong obj, HeapHashSegment seg)
        {
            return checked((int)(obj - seg.StartAddress) / _minObjSize);
        }

        private bool GetSegment(ulong obj, out HeapHashSegment seg)
        {
            if (obj != 0)
            {
                if (_lastSegment.StartAddress <= obj && obj < _lastSegment.EndAddress)
                {
                    seg = _lastSegment;
                    return true;
                }


                int i = _segments.Length >> 1;
                int mid, lower = 0, upper = _segments.Length - 1;

                while (lower <= upper)
                {
                    mid = (lower + upper) >> 1;

                    if (obj < _segments[mid].StartAddress)
                    {
                        upper = mid - 1;
                    }
                    else if (obj >= _segments[mid].EndAddress)
                    {
                        lower = mid + 1;
                    }
                    else
                    {
                        _lastSegment = _segments[mid];
                        seg = _segments[mid];
                        return true;
                    }
                }
            }

            seg = new HeapHashSegment();
            return false;
        }
        
        /// <summary>
        /// A segment of memory in the heap.
        /// </summary>
        protected struct HeapHashSegment
        {
            /// <summary>
            /// The the objects in the memory range.
            /// </summary>
            public BitArray Objects;

            /// <summary>
            /// The start address of the segment.
            /// </summary>
            public ulong StartAddress;

            /// <summary>
            /// The end address of the segment.
            /// </summary>
            public ulong EndAddress;
        }
    }
}
