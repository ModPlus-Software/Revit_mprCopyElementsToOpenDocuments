namespace mprCopyElementsToOpenDocuments.Models.Interfaces
{
    /// <summary>
    /// Интерфейс элемента дерева
    /// </summary>
    public interface IBrowserItem
    {
        /// <summary>
        /// Имя элемента
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Указывает, отмечен ли элемент в браузере
        /// </summary>
        bool? Checked { get; set; }

        /// <summary>
        /// Видимость второй строки
        /// </summary>
        bool ShowSecondRow { get; set; }

        /// <summary>
        /// Значение второй строки
        /// </summary>
        string SecondRowValue { get; set; }
    }
}