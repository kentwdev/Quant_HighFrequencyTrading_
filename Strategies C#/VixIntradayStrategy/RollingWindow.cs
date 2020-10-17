using System;
using System.Collections;
using System.Collections.Generic;
using QuantConnect.Indicators;

namespace Strategies.VixIntradayStrategy
{
    public class RollingWindow<T> : IReadOnlyWindow<T>
    {
        // the backing list object used to hold the data
        public List<T> _list;
        // read-write lock used for controlling access to the underlying list data structure
        //private readonly ReaderWriterLockSlim _listLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        // the most recently removed item from the window (fell off the back)
        private T _mostRecentlyRemoved;
        // the total number of samples taken by this indicator
        private decimal _samples;
        // used to locate the last item in the window as an indexer into the _list
        private int _tail;

        /// <summary>
        /// Initializes a new instance of the RollwingWindow class with the specified window size.
        /// </summary>
        /// <param name="size">The number of items to hold in the window</param>
        public RollingWindow(int size)
        {
            if (size < 1)
            {
                throw new ArgumentException("RollingWindow must have size of at least 1.", nameof(size));
            }

            _list = new List<T>(size);
        }

        public int Size => _list.Capacity;

        public int Count => _list.Count;

        /// <summary>
        /// Gets the number of samples that have been added to this window over its lifetime
        /// </summary>
        public decimal Samples => _samples;

        /// <summary>
        ///     Gets the most recently removed item from the window. This is the
        ///     piece of data that just 'fell off' as a result of the most recent
        ///     add. If no items have been removed, this will throw an exception.
        /// </summary>
        public T MostRecentlyRemoved
        {
            get
            {
                if (!IsReady)
                {
                    throw new InvalidOperationException("No items have been removed yet!");
                }

                return _mostRecentlyRemoved;
            }
        }

        /// <summary>
        ///     Indexes into this window, where index 0 is the most recently
        ///     entered value
        /// </summary>
        /// <param name="i">the index, i</param>
        /// <returns>the ith most recent entry</returns>
        public T this[int i]
        {
            get
            {
                //_listLock.EnterReadLock();

                if (i >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(i), i, $"Must be between 0 and Count {Count}");
                }

                return _list[(Count + _tail - i - 1) % Count];

            }
            set
            {
                if (i >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(i), i, $"Must be between 0 and Count {Count}");
                }

                _list[(Count + _tail - i - 1) % Count] = value;

            }
        }

        /// <summary>
        ///     Gets a value indicating whether or not this window is ready, i.e,
        ///     it has been filled to its capacity and one has fallen off the back
        /// </summary>
        public bool IsReady => Samples > Size;

        public IEnumerator<T> GetEnumerator()
        {
            // we make a copy on purpose so the enumerator isn't tied 
            // to a mutable object, well it is still mutable but out of scope
            var temp = new List<T>(Count);

            for (int i = 0; i < Count; i++)
            {
                temp.Add(this[i]);
            }

            return temp.GetEnumerator();

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Adds an item to this window and shifts all other elements
        /// </summary>
        /// <param name="item">The item to be added</param>
        public void Add(T item)
        {
            _samples++;
            if (Size == Count)
            {
                // keep track of what's the last element
                // so we can reindex on this[ int ]
                _mostRecentlyRemoved = _list[_tail];
                _list[_tail] = item;
                _tail = (_tail + 1) % Size;
            }
            else
            {
                _list.Add(item);
            }
        }

        /// <summary>
        ///     Clears this window of all data
        /// </summary>
        public void Reset()
        {
            _samples = 0;
            _list.Clear();
        }
    }
}