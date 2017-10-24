using System;
namespace GoogleMapsUtils.Android.Util
{
    public class LongSparseArray<TItem>
    {
        private Element<TItem>[][] _buckets;
        private uint _capacity;

        public LongSparseArray() : this(214373) // some prime number
        {
        }

        public LongSparseArray(uint capacity)
        {
            _capacity = capacity;
            _buckets = new Element<TItem>[_capacity][];
        }

		public int Count
		{
			get
			{
				int r = 0;
				foreach (var e in _buckets)
				{
					if (e != null)
					{
						r += e.Length;
					}
				}

				return r;
			}
		}

        public void Put(int key, TItem value)
        {
            Put(Convert.ToUInt64(key), value);
        }

        public void Put(ulong key, TItem value)
        {
            var hash = Hash(key);
            Element<TItem>[] e;

            if(_buckets[hash] == null)
            {
                e = new Element<TItem>[1];
                _buckets[hash] = e;
            }
            else
            {
                foreach(var elem in _buckets[hash])
                {
                    if (elem.Key == key)
                    {
                        elem.Value = value;
                        return;
                    }
                }

                e = new Element<TItem>[_buckets[hash].Length + 1];
                Array.Copy(_buckets[hash], 0, e, 1, _buckets[hash].Length);
                _buckets[hash] = e;
            }

            e[0] = new Element<TItem> { Key = key, Value = value };
        }

        public TItem Get(int key)
        {
            return Get(Convert.ToUInt64(key));
        }

        public TItem Get(ulong key)
        {
            var hash = Hash(key);
            Element<TItem>[] e = _buckets[hash];

            if(e == null)
            {
                return default(TItem);
            }

            foreach (var f in e)
            {
                if (f.Key == key)
                {
                    return f.Value;
                }
            }

            return default(TItem);
        }

        public bool Has(ulong key)
        {
            var hash = Hash(key);
            Element<TItem>[] e = _buckets[hash];

            if(e == null)
            {
                return false;
            }

            foreach (var f in e)
            {
                if (f.Key == key)
                {
                    return true;
                }
            }

            return false;
        }

        private uint Hash(ulong key)
        {
            return (uint)(key % _capacity);
        }

        private class Element<TValue>
        {
            public ulong Key { get; set; }
            public TValue Value { get; set; }
        }
    }
}
