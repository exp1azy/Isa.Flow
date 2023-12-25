using System.Text.RegularExpressions;

namespace Isa.Flow.EsIndexer.Dto
{
    /// <summary>
    /// Описание индекса ES.
    /// </summary>
    internal class IndexDescription
    {
        /// <summary>
        /// Наименование индекса.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Год начала включительно.
        /// </summary>
        public int YearFrom { get; set; }

        /// <summary>
        /// Год окончания включительно.
        /// </summary>
        public int YearTo { get; set; }

        /// <summary>
        /// Оператор преобразования имени индекса в его описание.
        /// </summary>
        /// <param name="index">Имя индекса.</param>
        /// <remarks>Если преобразование не удалось, возвращается описание, в котором свойства <see cref="YearFrom"/> и <see cref="YearTo"/> равны 0.</remarks>
        public static implicit operator IndexDescription(string index)
        {
            var descr = new IndexDescription { Name = index, YearFrom = 0, YearTo = 0 };

            var parts = index.Split('.');
            if (parts.Length < 3)
                return descr;

            if (new Regex("\\s*\\d+-\\d+\\s*").IsMatch(parts[2]))
            {
                var years = parts[2].Trim().Split('-').Select(y => int.Parse(y)).ToList();
                descr.YearFrom = years.Min();
                descr.YearTo = years.Max();
            }
            else if (new Regex("\\s*-\\d+\\s*").IsMatch(parts[2]))
            {
                descr.YearFrom = int.MinValue;
                descr.YearTo = int.Parse(parts[2].Trim(' ', '-'));
            }
            else if (new Regex("\\s*\\d+-\\s*").IsMatch(parts[2]))
            {
                descr.YearFrom = int.Parse(parts[2].Trim(' ', '-'));
                descr.YearTo = int.MaxValue;
            }
            else if (new Regex("\\s*\\d+\\s*").IsMatch(parts[2]))
            {
                descr.YearFrom = int.Parse(parts[2].Trim());
                descr.YearTo = descr.YearFrom;
            }

            return descr;
        }
    }
}