namespace mprCopyElementsToOpenDocuments.Models.Interfaces
{
    using System.Collections.ObjectModel;

    /// <summary>
    /// Интерфейс элемента дерева
    /// </summary>
    public interface IBrowserItem
    {
        /// <summary>
        /// Родительский элемент дерева
        /// </summary>
        IBrowserItem ParentBrowserItem { get; set; }
        
        /// <summary>
        /// Имя элемента
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Имя элемента в верхнем регистре для поиска
        /// </summary>
        string NameUpperInvariant { get; }

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
        
        /// <summary>
        /// Видимость элемента в дереве
        /// </summary>
        bool IsVisible { get; set; }
        
        /// <summary>
        /// Показывает, развернута ли группа
        /// </summary>
        bool IsExpanded { get; set; }
        
        /// <summary>
        /// Список вложенных элементов
        /// </summary>
        ObservableCollection<BrowserItem> Items { get; set; }
    }
}