﻿namespace mprCopyElementsToOpenDocuments.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.UI;
    using Models;
    using ModPlusAPI.Windows;

    /// <summary>
    /// Сервис работы с Revit
    /// </summary>
    public class RevitOperationService
    {
        private const string LangItem = "mprCopyElementsToOpenDocuments";
        private readonly UIApplication _uiApplication;
        private readonly List<Type> _elementTypes = new List<Type>
        {
            typeof(ExportDWGSettings),
            typeof(Material),
            typeof(ProjectInfo),
            typeof(ProjectLocation),
            typeof(SiteLocation),
            typeof(ParameterElement),
            typeof(SharedParameterElement),
            typeof(SunAndShadowSettings),
            typeof(SpatialElement),
            typeof(BrowserOrganization),
            typeof(DimensionType),
            typeof(FillPatternElement),
            typeof(ParameterFilterElement),
            typeof(LinePatternElement),
            typeof(Family),
            typeof(PhaseFilter),
            typeof(PrintSetting),
            typeof(Revision),
            typeof(RevisionSettings),
            typeof(TextNoteType),
            typeof(ViewFamilyType)
        };

        private readonly Dictionary<string, string> _specialTypeCategoryNames = new Dictionary<string, string>
        {
            { nameof(BrowserOrganization), ModPlusAPI.Language.GetItem(LangItem, "m12") },
            { nameof(DimensionType), ModPlusAPI.Language.GetItem(LangItem, "m13") },
            { nameof(SpotDimensionType), ModPlusAPI.Language.GetItem(LangItem, "m13") },
            { nameof(FillPatternElement), ModPlusAPI.Language.GetItem(LangItem, "m14") },
            { nameof(ParameterFilterElement), ModPlusAPI.Language.GetItem(LangItem, "m15") },
            { nameof(LinePatternElement), ModPlusAPI.Language.GetItem(LangItem, "m16") },
            { nameof(Family), ModPlusAPI.Language.GetItem(LangItem, "m17") },
            { nameof(PhaseFilter), ModPlusAPI.Language.GetItem(LangItem, "m18") },
            { nameof(PrintSetting), ModPlusAPI.Language.GetItem(LangItem, "m19") },
            { nameof(Revision), ModPlusAPI.Language.GetItem(LangItem, "m20") },
            { nameof(RevisionSettings), ModPlusAPI.Language.GetItem(LangItem, "m21") },
            { nameof(TextNoteType), ModPlusAPI.Language.GetItem(LangItem, "m22") },
            { nameof(ViewFamilyType), ModPlusAPI.Language.GetItem(LangItem, "m23") },
            { nameof(View), ModPlusAPI.Language.GetItem(LangItem, "m24") },
            { nameof(ParameterElement), ModPlusAPI.Language.GetItem(LangItem, "m25") },
            { nameof(SharedParameterElement), ModPlusAPI.Language.GetItem(LangItem, "m26") },
        };

        private readonly Dictionary<ViewType, string> _viewTypeNames = new Dictionary<ViewType, string>
        {
            { ViewType.AreaPlan, ModPlusAPI.Language.GetItem(LangItem, "m34") },
            { ViewType.CeilingPlan, ModPlusAPI.Language.GetItem(LangItem, "m35") },
            { ViewType.ColumnSchedule, ModPlusAPI.Language.GetItem(LangItem, "m36") },
            { ViewType.CostReport, ModPlusAPI.Language.GetItem(LangItem, "m37") },
            { ViewType.Detail, ModPlusAPI.Language.GetItem(LangItem, "m38") },
            { ViewType.DraftingView, ModPlusAPI.Language.GetItem(LangItem, "m39") },
            { ViewType.DrawingSheet, ModPlusAPI.Language.GetItem(LangItem, "m40") },
            { ViewType.Elevation, ModPlusAPI.Language.GetItem(LangItem, "m41") },
            { ViewType.EngineeringPlan, ModPlusAPI.Language.GetItem(LangItem, "m42") },
            { ViewType.FloorPlan, ModPlusAPI.Language.GetItem(LangItem, "m43") },
            { ViewType.Internal, ModPlusAPI.Language.GetItem(LangItem, "m44") },
            { ViewType.Legend, ModPlusAPI.Language.GetItem(LangItem, "m45") },
            { ViewType.LoadsReport, ModPlusAPI.Language.GetItem(LangItem, "m46") },
            { ViewType.PanelSchedule, ModPlusAPI.Language.GetItem(LangItem, "m47") },
            { ViewType.PresureLossReport, ModPlusAPI.Language.GetItem(LangItem, "m48") },
            { ViewType.ProjectBrowser, ModPlusAPI.Language.GetItem(LangItem, "m49") },
            { ViewType.Rendering, ModPlusAPI.Language.GetItem(LangItem, "m50") },
            { ViewType.Report, ModPlusAPI.Language.GetItem(LangItem, "m51") },
            { ViewType.Schedule, ModPlusAPI.Language.GetItem(LangItem, "m52") },
            { ViewType.Section, ModPlusAPI.Language.GetItem(LangItem, "m53") },
            { ViewType.SystemBrowser, ModPlusAPI.Language.GetItem(LangItem, "m54") },
#if !R2017 && !R2018 && !R2019
            { ViewType.SystemsAnalysisReport, ModPlusAPI.Language.GetItem(LangItem, "m55") },
#endif
            { ViewType.ThreeD, ModPlusAPI.Language.GetItem(LangItem, "m56") },
            { ViewType.Walkthrough, ModPlusAPI.Language.GetItem(LangItem, "m57") },
            { ViewType.Undefined, ModPlusAPI.Language.GetItem(LangItem, "m58") },
        };

        private bool _stopCopyingOperation;
        private int _passedElements;

        /// <summary>
        /// Создает экземпляр класса <see cref="UIApplication"/>
        /// </summary>
        /// <param name="uiApplication">Активная сессия пользовательского интерфейса Revit</param>
        public RevitOperationService(UIApplication uiApplication)
        {
            _uiApplication = uiApplication;
        }

        /// <summary>
        /// Событие изменения количества элементов, прошедших проверку
        /// </summary>
        public event EventHandler<bool> PassedElementsCountChanged;

        /// <summary>
        /// Событие изменения количества элементов с ошибками
        /// </summary>
        public event EventHandler BrokenElementsCountChanged;

        /// <summary>
        /// Метод остановки операции копирования
        /// </summary>
        public void StopCopyingOperation()
        {
            _stopCopyingOperation = true;
        }

        /// <summary>
        /// Получает все открытые документы Revit
        /// </summary>
        public IEnumerable<RevitDocument> GetAllDocuments()
        {
            foreach (Document document in _uiApplication.Application.Documents)
            {
                yield return new RevitDocument(document);
            }
        }

        /// <summary>
        /// Получает все элементы выбранного документа Revit
        /// </summary>
        /// <param name="revitDocument">Документ Revit для извлечения данных</param>
        /// <returns>Общая группа элементов в браузере</returns>
        public GeneralItemsGroup GetAllRevitElements(RevitDocument revitDocument)
        {
            Logger.Instance.Add(
                string.Format(
                    ModPlusAPI.Language.GetItem(LangItem, "m2"),
                    DateTime.Now.ToLocalTime(),
                    revitDocument.Title));

            try
            {
                var allElements = new List<BrowserItem>();
                allElements.AddRange(GetElementTypes(revitDocument));
                allElements.AddRange(GetElements(revitDocument));
                allElements.AddRange(GetSpecialCategoryElements(revitDocument));
                allElements.AddRange(GetViewTemplates(revitDocument));
                allElements.AddRange(GetViews(revitDocument));
                allElements.AddRange(GetElevationMarkers(revitDocument));
                allElements.AddRange(GetViewports(revitDocument));
                allElements.AddRange(revitDocument.Document.IsWorkshared ? GetWorksets(revitDocument) : new List<BrowserItem>());
                allElements.AddRange(GetGridsAndLevels(revitDocument));
                allElements.AddRange(GetParameters(revitDocument));
                allElements.AddRange(GetCategories(revitDocument));

                var elementsGroupedByCategory = allElements
                    .GroupBy(e => e.CategoryName)
                    .ToList();
                var categoryGroups = new List<BrowserItem>();
                foreach (var categoryGroup in elementsGroupedByCategory)
                {
                    var elementsGroupedByType = categoryGroup
                        .GroupBy(e => e.FamilyName)
                        .ToList();
                    var typeGroups = new List<BrowserItem>();
                    foreach (var typeGroup in elementsGroupedByType)
                    {
                        var instances = typeGroup.ToList();
                        instances = instances.OrderBy(instance => instance.Name).ToList();

                        if (string.IsNullOrEmpty(typeGroup.Key))
                        {
                            instances.ForEach(instance => typeGroups.Add(instance));
                            continue;
                        }

                        typeGroups.Add(new BrowserItem(typeGroup.Key, instances));
                    }

                    typeGroups = typeGroups.OrderBy(type => type.Name).ToList();
                    categoryGroups.Add(new BrowserItem(categoryGroup.Key, typeGroups));
                }

                categoryGroups = categoryGroups.OrderBy(category => category.Name).ToList();

                Logger.Instance.Add(
                    string.Format(
                        ModPlusAPI.Language.GetItem(LangItem, "m3"),
                        DateTime.Now.ToLocalTime(),
                        revitDocument.Title));
                Logger.Instance.Add("---------");

                return new GeneralItemsGroup(
                    ModPlusAPI.Language.GetItem(LangItem, "m28"),
                    categoryGroups);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);

                return new GeneralItemsGroup(
                    ModPlusAPI.Language.GetItem(LangItem, "m28"),
                    new List<BrowserItem>());
            }
        }

        /// <summary>
        /// Копирует все выбранные элементы в выбранные документы
        /// </summary>
        /// <param name="documentFrom">Документ из которого производится копирование</param>
        /// <param name="documentsTo">Список документов в которые осуществляется копирование</param>
        /// <param name="elements">Список элементов Revit</param>
        /// <param name="copyingOptions">Настройки копирования элементов</param>
        public async void CopyElements(
            RevitDocument documentFrom,
            IEnumerable<RevitDocument> documentsTo,
            List<BrowserItem> elements,
            CopyingOptions copyingOptions)
        {
            _uiApplication.Application.FailuresProcessing += Application_FailuresProcessing;

            var revitDocuments = documentsTo.ToList();

            Logger.Instance.Add(string.Format(
                ModPlusAPI.Language.GetItem(LangItem, "m5"),
                DateTime.Now.ToLocalTime(),
                documentFrom.Title,
                string.Join(", ", revitDocuments.Select(doc => doc.Title))));
            Logger.Instance.Add(string.Format(
                ModPlusAPI.Language.GetItem(LangItem, "m8"),
                GetCopyingOptionsName(copyingOptions)));

            var copyPasteOption = new CopyPasteOptions();
            switch (copyingOptions)
            {
                case CopyingOptions.AllowDuplicates:
                    copyPasteOption.SetDuplicateTypeNamesHandler(new CustomCopyHandlerAllow());
                    break;
                case CopyingOptions.RefuseDuplicate:
                    copyPasteOption.SetDuplicateTypeNamesHandler(new CustomCopyHandlerAbort());
                    break;
            }

            foreach (var documentTo in revitDocuments)
            {
                foreach (var element in elements)
                {
                    var succeed = true;
                    try
                    {
                        await Task.Delay(100).ConfigureAwait(true);

                        var elementId = new ElementId(element.Id);
                        var revitElement = documentFrom.Document.GetElement(elementId);
                        ICollection<ElementId> elementIds = new List<ElementId> { elementId };

                        using (var transaction = new Transaction(
                            documentTo.Document,
                            ModPlusAPI.Language.GetItem(LangItem, "m27")))
                        {
                            transaction.Start();

                            try
                            {
                                if (revitElement.GetType() == typeof(Workset))
                                {
                                    if (documentTo.Document.IsWorkshared)
                                    {
                                        Workset.Create(documentTo.Document, revitElement.Name);
                                    }
                                    else
                                    {
                                        Logger.Instance.Add(string.Format(
                                            ModPlusAPI.Language.GetItem(LangItem, "m9"),
                                            DateTime.Now.ToLocalTime(),
                                            documentTo.Title));
                                    }
                                }

                                if (_stopCopyingOperation)
                                {
                                    _stopCopyingOperation = false;
                                    OnPassedElementsCountChanged(true);
                                    return;
                                }

                                ElementTransformUtils.CopyElements(
                                documentFrom.Document,
                                elementIds,
                                documentTo.Document,
                                null,
                                copyPasteOption);
                            }
                            catch (Exception e)
                            {
                                Logger.Instance.Add(string.Format(
                                    ModPlusAPI.Language.GetItem(LangItem, "m7"),
                                    DateTime.Now.ToLocalTime(),
                                    element.Name,
                                    element.Id,
                                    element.CategoryName,
                                    e.Message));

                                succeed = false;
                            }

                            transaction.Commit();
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Instance.Add(string.Format(
                            ModPlusAPI.Language.GetItem(LangItem, "m7"),
                            DateTime.Now.ToLocalTime(),
                            element.Name,
                            element.Id,
                            element.CategoryName,
                            e.Message));

                        succeed = false;
                    }

                    if (!succeed)
                        OnBrokenElementsCountChanged();

                    _passedElements++;
                    if (_passedElements == elements.Count * revitDocuments.Count)
                    {
                        OnPassedElementsCountChanged(true);
                        _passedElements = 0;
                        _uiApplication.Application.FailuresProcessing -= Application_FailuresProcessing;
                    }
                    else
                    {
                        OnPassedElementsCountChanged(false);
                    }
                }
            }

            Logger.Instance.Add(string.Format(
                ModPlusAPI.Language.GetItem(LangItem, "m6"),
                DateTime.Now.ToLocalTime(),
                documentFrom.Title,
                string.Join(", ", revitDocuments.Select(doc => doc.Title))));
            Logger.Instance.Add("---------");
        }

        /// <summary>
        /// Возвращает текстовое представление настроек копирования элементов
        /// </summary>
        /// <param name="copyingOptions">Настройки копирования элементов</param>
        /// <returns>Текстовое представление настроек копирования элементов</returns>
        private string GetCopyingOptionsName(CopyingOptions copyingOptions)
        {
            switch (copyingOptions)
            {
                case CopyingOptions.AllowDuplicates:
                    return ModPlusAPI.Language.GetItem(LangItem, "co1");
                case CopyingOptions.RefuseDuplicate:
                    return ModPlusAPI.Language.GetItem(LangItem, "co2");
                case CopyingOptions.AskUser:
                    return ModPlusAPI.Language.GetItem(LangItem, "co3");
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Метод вызова изменения количества элементов, прошедших проверку
        /// </summary>
        /// <param name="e">Указывает, закончилась ли операция копирования</param>
        protected virtual void OnPassedElementsCountChanged(bool e)
        {
            PassedElementsCountChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Метод вызова изменения количества элементов с ошибками
        /// </summary>
        protected virtual void OnBrokenElementsCountChanged()
        {
            BrokenElementsCountChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Обработчик предупреждений
        /// </summary>
        private static void Application_FailuresProcessing(
            object sender,
            Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            var failureAccessor = e.GetFailuresAccessor();

            var failList = failureAccessor.GetFailureMessages();
            if (!failList.Any())
                return;

            if (failureAccessor.GetSeverity() == FailureSeverity.Warning)
            {
                failureAccessor.DeleteAllWarnings();
                e.SetProcessingResult(FailureProcessingResult.Continue);
            }

            e.SetProcessingResult(failureAccessor.GetSeverity() == FailureSeverity.Error
                ? FailureProcessingResult.ProceedWithCommit
                : FailureProcessingResult.ProceedWithRollBack);
        }

        #region Get from document

        private static IEnumerable<BrowserItem> GetElementTypes(RevitDocument revitDocument)
        {
            return new FilteredElementCollector(revitDocument.Document)
                .WhereElementIsElementType()
                .Where(e => e.Category != null && e.GetType() != typeof(ViewSheet))
                .Select(e =>
                {
                    try
                    {
                        return new BrowserItem(
                            e.Id.IntegerValue,
                            e.Category.Name + ModPlusAPI.Language.GetItem(LangItem, "m11"),
                            ((ElementType)e).FamilyName,
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Add(
                            string.Format(
                                ModPlusAPI.Language.GetItem(LangItem, "m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                ex.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        private static IEnumerable<BrowserItem> GetCategories(RevitDocument revitDocument)
        {
            var categories = new List<BrowserItem>();
            var categoriesList = revitDocument.Document.Settings.Categories;
            foreach (Category category in categoriesList)
            {
                if (category.Id.IntegerValue > 0)
                    continue;

                var subCategories = category.SubCategories;
                if (subCategories == null || subCategories.Size == 0)
                    continue;

                foreach (Category subCategory in subCategories)
                {
                    var element = revitDocument.Document.GetElement(subCategory.Id);
                    if (element != null)
                    {
                        categories.Add(new BrowserItem(
                            element.Id.IntegerValue,
                            ModPlusAPI.Language.GetItem(LangItem, "m26"),
                            string.Empty,
                            element.Name));
                    }
                }
            }

            return categories;
        }

        private IEnumerable<BrowserItem> GetParameters(RevitDocument revitDocument)
        {
            var parameters = new List<BrowserItem>();
            if (revitDocument.Document.IsFamilyDocument)
                return parameters;

            var definitionBindingMapIterator = revitDocument.Document.ParameterBindings.ForwardIterator();
            definitionBindingMapIterator.Reset();
            while (definitionBindingMapIterator.MoveNext())
            {
                Element element = null;
                try
                {
                    var key = (InternalDefinition)definitionBindingMapIterator.Key;
                    element = revitDocument.Document.GetElement(key.Id);
                    var elementType = (ElementType)revitDocument.Document.GetElement(element.GetTypeId());
                    parameters.Add(new BrowserItem(
                        element.Id.IntegerValue,
                        element.Category?.Name ?? _specialTypeCategoryNames[element.GetType().Name],
                        elementType != null ? elementType.FamilyName : string.Empty,
                        element.Name));
                }
                catch (Exception ex)
                {
                    Logger.Instance.Add(string.Format(
                        ModPlusAPI.Language.GetItem(LangItem, "m1"),
                        DateTime.Now.ToLocalTime(),
                        element != null ? element.Id.IntegerValue.ToString() : "null",
                        ex.Message));
                }
            }

            return parameters;
        }

        private static IEnumerable<BrowserItem> GetGridsAndLevels(RevitDocument revitDocument)
        {
            return new FilteredElementCollector(revitDocument.Document)
                .WherePasses(new ElementMulticlassFilter(new List<Type>
                {
                    typeof(Grid), typeof(Level)
                }))
                .WhereElementIsNotElementType()
                .Select(e =>
                {
                    try
                    {
                        var elementType = (ElementType)revitDocument.Document.GetElement(e.GetTypeId());
                        return new BrowserItem(
                            e.Id.IntegerValue,
                            e.Category.Name,
                            elementType != null ? elementType.FamilyName : string.Empty,
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Add(
                            string.Format(
                                ModPlusAPI.Language.GetItem(LangItem, "m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                ex.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        private static IEnumerable<BrowserItem> GetWorksets(RevitDocument revitDocument)
        {
            return new FilteredWorksetCollector(revitDocument.Document)
                .OfKind(WorksetKind.UserWorkset)
                .Select(e =>
                {
                    try
                    {
                        return new BrowserItem(
                            e.Id.IntegerValue,
                            ModPlusAPI.Language.GetItem(LangItem, "m10"),
                            "-",
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Add(
                            string.Format(
                                ModPlusAPI.Language.GetItem(LangItem, "m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                ex.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        private static IEnumerable<BrowserItem> GetViewports(RevitDocument revitDocument)
        {
            return new FilteredElementCollector(revitDocument.Document)
                .OfClass(typeof(ElementType))
                .Where(e => ((ElementType)e).FamilyName == "Viewport")
                .Select(e =>
                {
                    try
                    {
                        var elementType = (ElementType)revitDocument.Document.GetElement(e.GetTypeId());
                        return new BrowserItem(
                            e.Id.IntegerValue,
                            e.Category.Name,
                            elementType != null ? elementType.FamilyName : string.Empty,
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Add(
                            string.Format(
                                ModPlusAPI.Language.GetItem(LangItem, "m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                ex.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        private static IEnumerable<BrowserItem> GetElevationMarkers(RevitDocument revitDocument)
        {
            return new FilteredElementCollector(revitDocument.Document)
                .OfClass(typeof(ElevationMarker))
                .WhereElementIsNotElementType()
                .Where(e => ((ElevationMarker)e).CurrentViewCount > 0)
                .Select(e =>
                {
                    try
                    {
                        var elementType = (ElementType)revitDocument.Document.GetElement(e.GetTypeId());
                        return new BrowserItem(
                            e.Id.IntegerValue,
                            e.Category.Name,
                            elementType != null ? elementType.FamilyName : string.Empty,
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Add(
                            string.Format(
                                ModPlusAPI.Language.GetItem(LangItem, "m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                ex.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        private IEnumerable<BrowserItem> GetViews(RevitDocument revitDocument)
        {
            return new FilteredElementCollector(revitDocument.Document)
                .OfClass(typeof(View))
                .WhereElementIsNotElementType()
                .Where(e => !((View)e).IsTemplate)
                .Select(e =>
                {
                    try
                    {
                        var elementType = (ElementType)revitDocument.Document.GetElement(e.GetTypeId());
                        return new BrowserItem(
                            e.Id.IntegerValue,
                            e.Category?.Name ?? _specialTypeCategoryNames[e.GetType().Name],
                            elementType != null ? elementType.FamilyName : string.Empty,
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Add(
                            string.Format(
                                ModPlusAPI.Language.GetItem(LangItem, "m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                ex.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        private IEnumerable<BrowserItem> GetViewTemplates(RevitDocument revitDocument)
        {
            return new FilteredElementCollector(revitDocument.Document)
                .OfClass(typeof(View))
                .Where(e => ((View)e).IsTemplate)
                .Select(e =>
                {
                    try
                    {
                        var viewType = ((View)e).ViewType;
                        return new BrowserItem(
                            e.Id.IntegerValue,
                            ModPlusAPI.Language.GetItem(LangItem, "m29"),
                            _viewTypeNames[viewType],
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Add(
                            string.Format(
                                ModPlusAPI.Language.GetItem(LangItem, "m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                ex.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        private static IEnumerable<BrowserItem> GetSpecialCategoryElements(RevitDocument revitDocument)
        {
            return new FilteredElementCollector(revitDocument.Document)
                .WherePasses(new LogicalOrFilter(new List<ElementFilter>
                {
                    new ElementCategoryFilter(BuiltInCategory.OST_ColorFillSchema),
                    new ElementCategoryFilter(BuiltInCategory.OST_AreaSchemes),
                    new ElementCategoryFilter(BuiltInCategory.OST_Phases),
                    new ElementCategoryFilter(BuiltInCategory.OST_VolumeOfInterest)
                }))
                .Select(e =>
                {
                    try
                    {
                        var elementType = (ElementType)revitDocument.Document.GetElement(e.GetTypeId());
                        return new BrowserItem(
                            e.Id.IntegerValue,
                            e.Category.Name,
                            elementType != null ? elementType.FamilyName : string.Empty,
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Add(
                            string.Format(
                                ModPlusAPI.Language.GetItem(LangItem, "m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                ex.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        private IEnumerable<BrowserItem> GetElements(RevitDocument revitDocument)
        {
            return new FilteredElementCollector(revitDocument.Document)
                .WherePasses(new ElementMulticlassFilter(_elementTypes))
                .Select(e =>
                {
                    try
                    {
                        var elementType = (ElementType)revitDocument.Document.GetElement(e.GetTypeId());
                        return new BrowserItem(
                            e.Id.IntegerValue,
                            e.Category?.Name ?? _specialTypeCategoryNames[e.GetType().Name],
                            elementType != null ? elementType.FamilyName : string.Empty,
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Add(
                            string.Format(
                                ModPlusAPI.Language.GetItem(LangItem, "m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                ex.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        #endregion
    }
}
