using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace EmergencyShift.Core.Composition
{
    /// <summary>Owned membership and transient ordering; ordinals are never persistent entity IDs.</summary>
    public sealed class SceneRegistry<T> : IDisposable where T : class
    {
        private readonly List<T> members = new List<T>();
        private readonly ReadOnlyCollection<T> view;
        private int sequence;
        private bool disposed;

        public IReadOnlyList<T> Members => view;

        public SceneRegistry()
        {
            view = members.AsReadOnly();
        }

        public int Register(T member)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SceneRegistry<T>));
            }

            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (members.Contains(member))
            {
                return 0;
            }

            members.Add(member);
            return ++sequence;
        }

        public void Remove(T member) => members.Remove(member);
        public int IndexOf(T member) => members.IndexOf(member);

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            members.Clear();
        }
    }
}
