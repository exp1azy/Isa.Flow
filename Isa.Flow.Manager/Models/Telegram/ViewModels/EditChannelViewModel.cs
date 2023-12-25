using System.ComponentModel.DataAnnotations;

namespace Isa.Flow.Manager.Models.Telegram.ViewModels
{
    /// <summary>
    /// Модель для обновления источника.
    /// </summary>
    public class EditChannelViewModel
    {
        /// <summary>
        /// Идентификатор источника.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Заговок источника.
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Заголовок не должен быть пустым или состоять из пробелов")]
        public string Title { get; set; }

        /// <summary>
        /// Наименование источника.
        /// </summary>
        [Required(AllowEmptyStrings = false, ErrorMessage = "Наименование не должно быть пустым или состоять из пробелов")]
        public string Site { get; set; }

        /// <summary>
        /// true, если канал активен, false, если неактивен
        /// </summary>
        public bool Enabled { get; set; }
    }
}
