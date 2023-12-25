namespace Isa.Flow.Interact.Extractor.Rpc
{
    /// <summary>
    /// Перечисление функций экстрактора.
    /// </summary>
    public enum SqlExtractionFunc
    {
        /// <summary>
        /// Функция извлечения информации о новых статьях.
        /// </summary>
        New,

        /// <summary>
        /// Функция извлечения информации об измененных статьях.
        /// </summary>
        Updated,

        /// <summary>
        /// Функция извлечения информации об удаленных статьях.
        /// </summary>
        Deleted,

        /// <summary>
        /// Функция реиндексации указанного диапазона статей.
        /// </summary>
        Reindex,

        /// <summary>
        /// Функция очистки указанного диапазона статей.
        /// </summary>
        Clean
    }
}