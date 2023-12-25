using Isa.Flow.Interact.Entities;
using System.Diagnostics.CodeAnalysis;

namespace Isa.Flow.Interact.Utils
{
    /// <summary>
    /// Функционал определения эквивалентности двух объектов типа <seealso cref="ActorInfo"/>.
    /// </summary>
    public class ActorInfoEqualityComparer : IEqualityComparer<ActorInfo>
    {
        /// <summary>
        /// Метод определения эквивалентности.
        /// </summary>
        /// <param name="x">Первый объект.</param>
        /// <param name="y">Второй объект.</param>
        /// <returns>True, если объекты эквивалентны, иначе - false.</returns>
        public bool Equals(ActorInfo? x, ActorInfo? y)
        {
            if (x == null || y == null)
                return false;

            return x.Id == y.Id;
        }

        /// <summary>
        /// Метод вычисления хеш-кода объекта.
        /// </summary>
        /// <param name="obj">Объект для вычисления хеш-кода.</param>
        /// <returns>Хеш-кода объекта</returns>
        public int GetHashCode([DisallowNull] ActorInfo obj) => obj.Id.GetHashCode();
    }
}