using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Isa.Flow.Interact.Utils
{
    /// <summary>
    /// Класс представляет неупорядоченную коллекцию уникальных элементов,
    /// для каждого из которых задано "время жизни".
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    public class TimeToLiveSet<T> : IEnumerable<T>
    {
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="ttl">Время жизни объектов в коллекции по умолчанию в миллисекундах.</param>
        /// <param name="comparer">Объект, предоставляющий функционал определения уникальности объектов; если null - используется функционал по умолчанию.</param>
        /// <exception cref="ArgumentOutOfRangeException">В случае, если время жизни задано меньше или равное нулю.</exception>
        public TimeToLiveSet(int ttl, IEqualityComparer<T>? comparer = null)
        {            
            _ttl = ttl > 0 ? ttl
                : throw new ArgumentOutOfRangeException(nameof(ttl), TTL_MUST_BE_GREATER_THAN_ZERO);

            _store = new(new EntityEqualityComparer<T>(comparer ?? EqualityComparer<T>.Default));
        }

        /// <summary>
        /// Метод добавления элемента в коллекцию
        /// </summary>
        /// <param name="obj">Элемент для добавления.</param>
        /// <param name="ttl">Время, после которого элемент будет считаться просроченным в миллисекундах.</param>
        /// <returns>True, если элемент добавлен в коллекцию, иначе - false</returns>
        /// <remarks>Если элемент не добавлен в коллекцию, это означает, что такой элемент уже есть в коллекции.
        /// В таком случае у этого элемента обновляется время жизни.
        /// После добавления элемента происходит ревизия просроченых элементов в коллекции - такие элементы удаляются.</remarks>
        public bool Add(T obj, int ttl = 0)
        {
            lock (_lock)
            {
                var added = false;
                var newEntity = new Entity<T>(obj, ttl > 0 ? ttl : _ttl);

                var existent = _store.FirstOrDefault(e => _store.Comparer.Equals(e, newEntity));
                if (existent != null)
                {
                    existent.Expired = newEntity.Expired;
                    added = false;
                }
                else
                {
                    added = _store.Add(newEntity);
                }

                if (added)
                    InvokeAppended(new T[] { newEntity.Value });

                RemoveExpired();

                return added;
            }
        }

        /// <summary>
        /// Метод удаления просроченных элементов.
        /// </summary>
        public void ForceRemoveExpired()
        {
            lock(_lock)
            {
                RemoveExpired();
            }
        }

        /// <summary>
        /// Событие появления в коллекции новых элементов.
        /// </summary>
        public event EventHandler<AppendedEventArgs>? Appended;

        /// <summary>
        /// Событие просрочки элементов в коллекции.
        /// </summary>
        public event EventHandler<ExpiredEventArgs>? Expired;

        /// <summary>
        /// Метод удаления просроченных элементов.
        /// </summary>
        private void RemoveExpired()
        {
            var expired = _store.Where(e => e.Expired <= DateTime.Now).ToList();
            _store.ExceptWith(expired);
            if (expired.Any())
                InvokeExpired(expired.Select(e => e.Value).ToList());
        }

        /// <summary>
        /// Инициация события <seealso cref="Appended"/>.
        /// </summary>
        /// <param name="items">Добавленные элементы.</param>
        private void InvokeAppended(IEnumerable<T> items)
        {
            if (Appended != null)
                _ = Task.Run( () => Appended.Invoke(this, new AppendedEventArgs { AppendedItems = items }) );
        }

        /// <summary>
        /// Инициация события <seealso cref="Expired"/>.
        /// </summary>
        /// <param name="items">Просроченные и удаленные элементы.</param>
        private void InvokeExpired(IEnumerable<T> items)
        {
            if (Expired != null)
                _ = Task.Run(() => Expired.Invoke(this, new ExpiredEventArgs { ExpiredItems = items }));
        }

        private readonly HashSet<Entity<T>> _store;
        private readonly int _ttl;
        private readonly object _lock = new();

        #region Реализация IEnumerable<T>

        /// <summary>
        /// Метод возвращает перечислитель, который осуществляет итерацию по коллекции.
        /// </summary>
        /// <returns>Объект для прохода по коллекции.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            ForceRemoveExpired();
            return _store.Select(e => e.Value).GetEnumerator();
        }

        /// <summary>
        /// Метод возвращает перечислитель, который осуществляет итерацию по коллекции.
        /// </summary>
        /// <returns>Объект для прохода по коллекции.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        /// <summary>
        /// Объект-оболочка для хранения элементов коллекции.
        /// </summary>
        /// <typeparam name="TValue">Тип элемента.</typeparam>
        private class Entity<TValue>
        {
            /// <summary>
            /// Конструктор.
            /// </summary>
            /// <param name="value">Элемент коллекции.</param>
            /// <param name="ttl">Время, после которого элемент будет считаться просроченным в милисекундах.</param>
            /// <exception cref="ArgumentNullException">В случае, если элемент коллекции null.</exception>
            /// <exception cref="ArgumentOutOfRangeException">В случае, если время жизни указано меньше или равное нулю.</exception>
            public Entity(TValue value, int ttl)
            {
                Value = value ?? throw new ArgumentNullException(nameof(value));
                Expired = DateTime.Now.AddMilliseconds(ttl > 0 ? ttl : throw new ArgumentOutOfRangeException(nameof(ttl), TTL_MUST_BE_GREATER_THAN_ZERO));
            }

            /// <summary>
            /// Время, после которого элемент будет считаться просроченным.
            /// </summary>
            public DateTime Expired { get; set; }

            /// <summary>
            /// Элемент коллекции.
            /// </summary>
            public TValue Value { get; set; }
        }

        private class EntityEqualityComparer<TValue> : IEqualityComparer<Entity<TValue>>
        {
            private readonly IEqualityComparer<TValue> _comparer;

            public EntityEqualityComparer(IEqualityComparer<TValue> comparer)
            {
                _comparer = comparer;
            }

            public bool Equals(TimeToLiveSet<T>.Entity<TValue>? x, TimeToLiveSet<T>.Entity<TValue>? y)
            {
                if (x == null || y == null)
                    return false;

                return _comparer.Equals(x.Value, y.Value);
            }

            public int GetHashCode([DisallowNull] TimeToLiveSet<T>.Entity<TValue> obj) => obj?.Value?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Параметры события просрочки элементов в коллекции.
        /// </summary>
        public class ExpiredEventArgs : System.EventArgs
        {
            /// <summary>
            /// Просроченные элементы.
            /// </summary>
            public IEnumerable<T>? ExpiredItems { get; set; }
        }

        /// <summary>
        /// Параметры события появления новых элементов в коллекции.
        /// </summary>
        public class AppendedEventArgs : System.EventArgs
        {
            /// <summary>
            /// Новые элементы.
            /// </summary>
            public IEnumerable<T>? AppendedItems { get; set; }
        }

        private const string TTL_MUST_BE_GREATER_THAN_ZERO = "Time to live parameter must be greater than zero";
    }
}