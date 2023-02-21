using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bldrs.Hashing
{
    /// <summary>
    /// Minimal perfect hash
    /// </summary>
    public class MinimalPerfectHash
    {
        public readonly ReadOnlyCollection< int >    GMap;
        public readonly ReadOnlyCollection< byte[] > Keys;
        public readonly ReadOnlyCollection< int >    SlotMap;

        private MinimalPerfectHash(int[] gMap, byte[][] keys, int[] slotMap )
        {
            GMap   = new ReadOnlyCollection< int >( gMap );
            Keys   = new ReadOnlyCollection< byte[] >( keys );
            SlotMap = new ReadOnlyCollection<int>(slotMap);
        }

        public bool TryGetSlot( Span< byte > key, out uint slot )
        {
            uint ghash = FNV1A.Hash(key) % (uint)GMap.Count;

            int d = GMap[(int)ghash];

            slot = (uint)Math.Abs(d);

            if (d > 0)
            {
                slot = FNV1A.Hash24(key, (uint)d) % (uint)Keys.Count;
            }

            var matchedKey = Keys[(int)slot];

            if (matchedKey != null)
            {
                var spanKey = new Span<byte>(matchedKey);

                return spanKey.SequenceEqual(key);
            }

            return false;
        }

        public static MinimalPerfectHash Create(byte[][] keys, uint? size = null)
        {
            uint actualSize = size ?? (uint)keys.Length;

            if ( actualSize < keys.Length )
            {
                throw new Exception("Perfect hash creation failed due to specified size being smaller than actual size");
            }

            if ( actualSize > 0xFFFFFF )
            {
                throw new Exception("Perfect hash creation failed due to table requiring more than (2^24)-1 elements");
            }

            uint gSize = Math.Max( Primes.Next((uint)keys.Length + (uint)(keys.Length / 4)) / 5, 1 );
            var buckets = new List<(byte[], uint)>[gSize];
            var gMap = new int[gSize];
            var slots = new int[actualSize];

            Array.Fill(slots, -1);

            for (int gIndex = 0; gIndex < buckets.Length; ++gIndex)
            {
                buckets[gIndex] = new List<(byte[], uint)>();
            }

            uint keyIndex = 0;

            foreach (byte[] key in keys)
            {
                uint bucketHash = FNV1A.Hash(new Span<byte>(key)) % gSize;

                buckets[bucketHash].Add((key, keyIndex++));
            }

            // Sort buckets to descending by size
            Array.Sort(buckets, (a, b) => b.Count.CompareTo(a.Count));

            uint bucketIndex = 0;

            var currentSet = new HashSet<uint>();

            while (bucketIndex < buckets.Length)
            {
                var pattern = buckets[bucketIndex];

                if (pattern.Count <= 1)
                {
                    break;
                }

                uint d = 1;

                // While D would be a positive integer
                for (; d < 0x80000000; ++d)
                {
                    currentSet.Clear();

                    foreach (var (key, index) in pattern)
                    {
                        uint slot = FNV1A.Hash24(new Span<byte>(key), d) % actualSize;

                        if (slots[slot] >= 0 || currentSet.Contains(slot))
                        {
                            break;
                        }

                        currentSet.Add(slot);
                    }

                    if (currentSet.Count == pattern.Count)
                    {
                        uint g = FNV1A.Hash(pattern[0].Item1) % gSize;

                        gMap[g] = (int)d;

                        foreach (var (key, index) in pattern)
                        {
                            uint slot = FNV1A.Hash24(new Span<byte>(key), d) % actualSize;

                            slots[slot] = (int)index;
                        }

                        break;
                    }
                }

                if (d == 0x80000000)
                {
                    throw new Exception("Perfect hash creation failed due to unfound bucket probe, may be due to duplicate keys");
                }

                ++bucketIndex;
            }

            for ( int slot = 0; slot < slots.Length && bucketIndex < buckets.Length; ++slot )
            {
                if ( slots[slot] < 0 )
                {
                    var pattern = buckets[bucketIndex];

                    if ( pattern.Count == 0 )
                    {
                        break;
                    }

                    uint g = FNV1A.Hash(pattern[0].Item1) % gSize;

                    // Note, we handle zero here by giving 0 to this case, so the <= 0 is a direct slot, and d must be between 1 and 0x7FFFFFFF inclusive.
                    gMap[g] = -slot;
                    slots[slot] = (int)pattern[0].Item2;

                    ++bucketIndex;
                }
            }

            byte[][] keyReordering = new byte[actualSize][];

            for ( int slot = 0; slot < slots.Length; ++slot )
            {
                int slotIndex = slots[slot];

                if ( slotIndex < 0)
                {
                    continue;
                }

                keyReordering[slot] = keys[slotIndex];
            }

            return new MinimalPerfectHash(gMap, keyReordering, slots);
        }
    }
}
