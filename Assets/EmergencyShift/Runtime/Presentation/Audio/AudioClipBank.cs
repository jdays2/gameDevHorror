using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EmergencyShift
{
    /// <summary>Read-only access to an authored clip array; identity retains per-bank playback history.</summary>
    public readonly struct AudioClipBank : IReadOnlyList<AudioClip>, IEquatable<AudioClipBank>
    {
        private readonly AudioClip[] clips;
        public int Count => clips != null ? clips.Length : 0;
        public AudioClip this[int index] => index >= 0 && index < Count ? clips[index] : throw new ArgumentOutOfRangeException(nameof(index));
        public AudioClipBank(AudioClip[] clips) => this.clips = clips;
        public bool Equals(AudioClipBank other) => ReferenceEquals(clips, other.clips);
        public override bool Equals(object value) => value is AudioClipBank other && Equals(other);
        public override int GetHashCode() => clips != null ? RuntimeHelpers.GetHashCode(clips) : 0;
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<AudioClip> IEnumerable<AudioClip>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public static implicit operator AudioClipBank(AudioClip[] clips) => new AudioClipBank(clips);

        public struct Enumerator : IEnumerator<AudioClip>
        {
            private readonly AudioClipBank bank;
            private int index;
            public AudioClip Current => index >= 0 && index < bank.Count ? bank[index] : throw new InvalidOperationException();
            object IEnumerator.Current => Current;

            public Enumerator(AudioClipBank bank)
            {
                this.bank = bank;
                index = -1;
            }

            public bool MoveNext()
            {
                if (index < bank.Count)
                {
                    index++;
                }

                return index < bank.Count;
            }
            public void Reset() => index = -1;
            public void Dispose()
            {
            }
        }
    }
}
