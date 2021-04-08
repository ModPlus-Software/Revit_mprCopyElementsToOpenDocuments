namespace mprCopyElementsToOpenDocuments.Helpers
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
        private readonly UIApplication _uiApplication;
        private readonly List<Type> _elementTypes = new List<Type>
        {
            typeof(ExportDWGSettings),
            typeof(ProjectInfo),
            typeof(ProjectLocation),
            typeof(SiteLocation),
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
            typeof(ViewFamilyType),
            typeof(GroupType)
        };

        private readonly Dictionary<string, string> _specialTypeCategoryNames = new Dictionary<string, string>
        {
            { nameof(BrowserOrganization), ModPlusAPI.Language.GetItem("m12") },
            { nameof(DimensionType), ModPlusAPI.Language.GetItem("m13") },
            { nameof(SpotDimensionType), ModPlusAPI.Language.GetItem("m13") },
            { nameof(FillPatternElement), ModPlusAPI.Language.GetItem("m14") },
            { nameof(ParameterFilterElement), ModPlusAPI.Language.GetItem("m15") },
            { nameof(LinePatternElement), ModPlusAPI.Language.GetItem("m16") },
            { nameof(Family), ModPlusAPI.Language.GetItem("m17") },
            { nameof(PhaseFilter), ModPlusAPI.Language.GetItem("m18") },
            { nameof(PrintSetting), ModPlusAPI.Language.GetItem("m19") },
            { nameof(Revision), ModPlusAPI.Language.GetItem("m20") },
            { nameof(RevisionSettings), ModPlusAPI.Language.GetItem("m21") },
            { nameof(TextNoteType), ModPlusAPI.Language.GetItem("m22") },
            { nameof(ViewFamilyType), ModPlusAPI.Language.GetItem("m23") },
            { nameof(View), ModPlusAPI.Language.GetItem("m24") },
            { nameof(ParameterElement), ModPlusAPI.Language.GetItem("m25") },
            { nameof(SharedParameterElement), ModPlusAPI.Language.GetItem("m26") },
        };

        private readonly Dictionary<ViewType, string> _viewTypeNames = new Dictionary<ViewType, string>
        {
            { ViewType.AreaPlan, ModPlusAPI.Language.GetItem("m34") },
            { ViewType.CeilingPlan, ModPlusAPI.Language.GetItem("m35") },
            { ViewType.ColumnSchedule, ModPlusAPI.Language.GetItem("m36") },
            { ViewType.CostReport, ModPlusAPI.Language.GetItem("m37") },
            { ViewType.Detail, ModPlusAPI.Language.GetItem("m38") },
            { ViewType.DraftingView, ModPlusAPI.Language.GetItem("m39") },
            { ViewType.DrawingSheet, ModPlusAPI.Language.GetItem("m40") },
            { ViewType.Elevation, ModPlusAPI.Language.GetItem("m41") },
            { ViewType.EngineeringPlan, ModPlusAPI.Language.GetItem("m42") },
            { ViewType.FloorPlan, ModPlusAPI.Language.GetItem("m43") },
            { ViewType.Internal, ModPlusAPI.Language.GetItem("m44") },
            { ViewType.Legend, ModPlusAPI.Language.GetItem("m45") },
            { ViewType.LoadsReport, ModPlusAPI.Language.GetItem("m46") },
            { ViewType.PanelSchedule, ModPlusAPI.Language.GetItem("m47") },
            { ViewType.PresureLossReport, ModPlusAPI.Language.GetItem("m48") },
            { ViewType.ProjectBrowser, ModPlusAPI.Language.GetItem("m49") },
            { ViewType.Rendering, ModPlusAPI.Language.GetItem("m50") },
            { ViewType.Report, ModPlusAPI.Language.GetItem("m51") },
            { ViewType.Schedule, ModPlusAPI.Language.GetItem("m52") },
            { ViewType.Section, ModPlusAPI.Language.GetItem("m53") },
            { ViewType.SystemBrowser, ModPlusAPI.Language.GetItem("m54") },
#if !R2017 && !R2018 && !R2019
            { ViewType.SystemsAnalysisReport, ModPlusAPI.Language.GetItem("m55") },
#endif
            { ViewType.ThreeD, ModPlusAPI.Language.GetItem("m56") },
            { ViewType.Walkthrough, ModPlusAPI.Language.GetItem("m57") },
            { ViewType.Undefined, ModPlusAPI.Language.GetItem("m58") },
        };

        private bool _stopCopyingOperation;
        private int _passedElements;

        // Текущий копируемый элемент. Поле требуется, чтобы иметь возможность добавлять в лог
        // сообщение из обработчика Application_FailuresProcessing
        private BrowserItem _currentCopiedElement;

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
                if (document.IsLinked)
                    continue;
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
            Logger.Instance.AddInfo(
                string.Format(
                    ModPlusAPI.Language.GetItem("m2"),
                    DateTime.Now.ToLocalTime(),
                    revitDocument.Title));

            try
            {
                var allElements = new List<BrowserItem>();
                allElements.AddRange(GetElements(revitDocument));
                allElements.AddRange(GetSpecialCategoryElements(revitDocument));
                allElements.AddRange(GetViewTemplates(revitDocument));
                allElements.AddRange(GetViews(revitDocument));
                allElements.AddRange(GetElevationMarkers(revitDocument));
                allElements.AddRange(GetViewports(revitDocument));
                if (revitDocument.Document.IsWorkshared)
                    allElements.AddRange(GetWorksets(revitDocument));
                allElements.AddRange(GetGridsAndLevels(revitDocument));
                allElements.AddRange(GetParameters(revitDocument));
                allElements.AddRange(GetCategories(revitDocument));
                allElements.AddRange(GetMaterials(revitDocument));

                var categoryGroups = GetGrouped(allElements);

                Logger.Instance.AddInfo(
                    string.Format(
                        ModPlusAPI.Language.GetItem("m3"),
                        DateTime.Now.ToLocalTime(),
                        revitDocument.Title));
                Logger.Instance.AddInfo("---------");

                return new GeneralItemsGroup(
                    ModPlusAPI.Language.GetItem("h15"),
                    categoryGroups)
                {
                    IsExpanded = false
                };
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);

                return new GeneralItemsGroup(
                    ModPlusAPI.Language.GetItem("m28"),
                    new List<BrowserItem>());
            }
        }

        /// <summary>
        /// Получает все элементы выбранного документа Revit
        /// </summary>
        /// <param name="revitDocument">Документ Revit для извлечения данных</param>
        /// <returns>Общая группа элементов в браузере</returns>
        public GeneralItemsGroup GetAllRevitElementTypes(RevitDocument revitDocument)
        {
            // Начало операции сбора типоразмеров модели
            Logger.Instance.AddInfo(
                string.Format(
                    ModPlusAPI.Language.GetItem("m60"),
                    DateTime.Now.ToLocalTime(),
                    revitDocument.Title));

            try
            {
                var categoryGroups = GetGrouped(GetElementTypes(revitDocument));

                // Завершение операции сбора типоразмеров модели
                Logger.Instance.AddInfo(
                    string.Format(
                        ModPlusAPI.Language.GetItem("m61"),
                        DateTime.Now.ToLocalTime(),
                        revitDocument.Title));
                Logger.Instance.AddInfo("---------");

                return new GeneralItemsGroup(
                    ModPlusAPI.Language.GetItem("h14"),
                    categoryGroups)
                {
                    IsExpanded = false
                };
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);

                return new GeneralItemsGroup(
                    ModPlusAPI.Language.GetItem("m28"),
                    new List<BrowserItem>());
            }
        }

        private static IEnumerable<BrowserItem> GetGrouped(IEnumerable<BrowserItem> allElements)
        {
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

            return categoryGroups.OrderBy(category => category.Name).ToList();
        }

        /// <summary>
        /// Копирует все выбранные элементы в выбранные документы
        /// </summary>
        /// <param name="documentFrom">Документ из которого производится копирование</param>
        /// <param name="documentsTo">Список документов в которые осуществляется копирование</param>
        /// <param name="elements">Список элементов Revit</param>
        /// <param name="copyingOptions">Настройки копирования элементов</param>
        /// <param name="suppressFailures">Подавлять предупреждения</param>
        public async void CopyElements(
            RevitDocument documentFrom,
            IEnumerable<RevitDocument> documentsTo,
            List<BrowserItem> elements,
            CopyingOptions copyingOptions,
            bool suppressFailures)
        {
            if (suppressFailures)
                _uiApplication.Application.FailuresProcessing += Application_FailuresProcessing;

            var revitDocuments = documentsTo.ToList();

            Logger.Instance.AddInfo(string.Format(
                ModPlusAPI.Language.GetItem("m5"),
                DateTime.Now.ToLocalTime(),
                documentFrom.Title,
                string.Join(", ", revitDocuments.Select(doc => doc.Title))));
            Logger.Instance.AddInfo(string.Format(
                ModPlusAPI.Language.GetItem("m8"),
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
                    if (_stopCopyingOperation)
                    {
                        _stopCopyingOperation = false;
                        OnPassedElementsCountChanged(true);
                        return;
                    }

                    _currentCopiedElement = element;
                    var succeed = true;
                    try
                    {
                        await Task.Delay(10).ConfigureAwait(true);

                        Workset workset = null;
                        if (element.IsWorksetId)
                        {
                            var worksetId = new WorksetId(element.Id);
                            var worksetTable = documentFrom.Document.GetWorksetTable();
                            workset = worksetTable.GetWorkset(worksetId);
                        }

                        using (var transaction = new Transaction(
                            documentTo.Document,
                            ModPlusAPI.Language.GetItem("m27")))
                        {
                            transaction.Start();

                            try
                            {
                                if (workset != null)
                                {
                                    if (documentTo.Document.IsWorkshared)
                                    {
                                        Workset.Create(documentTo.Document, workset.Name);
                                    }
                                    else
                                    {
                                        Logger.Instance.AddInfo(string.Format(
                                            ModPlusAPI.Language.GetItem("m9"),
                                            DateTime.Now.ToLocalTime(),
                                            documentTo.Title));
                                    }
                                }
                                else
                                {
                                    var elementId = new ElementId(element.Id);

                                    var copiedElementIds = ElementTransformUtils.CopyElements(
                                        documentFrom.Document,
                                        new List<ElementId> { elementId },
                                        documentTo.Document,
                                        null,
                                        copyPasteOption);

                                    if (documentFrom.Document.GetElement(elementId) is ViewDrafting sourceViewDrafting &&
                                        copiedElementIds.FirstOrDefault() is ElementId targetViewDraftingId &&
                                        documentTo.Document.GetElement(targetViewDraftingId) is ViewDrafting targetViewDrafting)
                                    {
                                        CopyDraftingViewContents(sourceViewDrafting, targetViewDrafting, copyPasteOption);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Instance.AddError(string.Format(
                                    ModPlusAPI.Language.GetItem("m7"),
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
                        Logger.Instance.AddError(string.Format(
                            ModPlusAPI.Language.GetItem("m7"),
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
                        if (suppressFailures)
                            _uiApplication.Application.FailuresProcessing -= Application_FailuresProcessing;
                    }
                    else
                    {
                        OnPassedElementsCountChanged(false);
                    }
                }
            }

            Logger.Instance.AddInfo(string.Format(
                ModPlusAPI.Language.GetItem("m6"),
                DateTime.Now.ToLocalTime(),
                documentFrom.Title,
                string.Join(", ", revitDocuments.Select(doc => doc.Title))));
            Logger.Instance.AddInfo("---------");
        }

        /// <summary>
        /// Копировать содержимое чертежного вида
        /// </summary>
        /// <param name="sourceViewDrafting">Исходный чертежный вид</param>
        /// <param name="targetViewDrafting">Целевой чертежный вид</param>
        /// <param name="cpOptions">Опции копирования</param>
        private void CopyDraftingViewContents(
            ViewDrafting sourceViewDrafting,
            ViewDrafting targetViewDrafting,
            CopyPasteOptions cpOptions)
        {
            targetViewDrafting.Scale = sourceViewDrafting.Scale;
            
            var viewContentsDrafting =
                new FilteredElementCollector(sourceViewDrafting.Document).OwnedByView(sourceViewDrafting.Id);
            
            var viewContents = new List<ElementId>();

            foreach (var item in viewContentsDrafting.Where(i =>
                i.Category != null &&
                i.Category.Id.IntegerValue != (int)BuiltInCategory.OST_IOSSketchGrid))
            {
                viewContents.Add(item.Id);
            }

            if (viewContents.Any())
            {
                ElementTransformUtils.CopyElements(sourceViewDrafting, viewContents, targetViewDrafting, null, cpOptions);
            }
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
                    return ModPlusAPI.Language.GetItem("co1");
                case CopyingOptions.RefuseDuplicate:
                    return ModPlusAPI.Language.GetItem("co2");
                case CopyingOptions.AskUser:
                    return ModPlusAPI.Language.GetItem("co3");
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
        private void Application_FailuresProcessing(
            object sender,
            Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            var failureAccessor = e.GetFailuresAccessor();

            var failList = failureAccessor.GetFailureMessages();
            if (!failList.Any())
                return;

            FailureProcessingResult processingResult;
            if (failureAccessor.GetSeverity() == FailureSeverity.Warning)
            {
                failureAccessor.DeleteAllWarnings();
                processingResult = FailureProcessingResult.Continue;
            }
            else
            {
                OnBrokenElementsCountChanged();
                if (_currentCopiedElement != null)
                {
                    Logger.Instance.AddError(string.Format(
                        ModPlusAPI.Language.GetItem("m7"),
                        DateTime.Now.ToLocalTime(),
                        _currentCopiedElement.Name,
                        _currentCopiedElement.Id,
                        _currentCopiedElement.CategoryName,
                        ModPlusAPI.Language.GetItem("m62")));
                }

                processingResult = FailureProcessingResult.ProceedWithRollBack;
            }

            e.SetProcessingResult(processingResult);
        }

        #region Get from document

        private static IEnumerable<BrowserItem> GetElementTypes(RevitDocument revitDocument)
        {
            var noCategory = ModPlusAPI.Language.GetItem("m64");
            return new FilteredElementCollector(revitDocument.Document)
                .WhereElementIsElementType()
                .OfType<ElementType>()
                .Where(e => !(e is ViewFamilyType) &&
                            !(e is BrowserOrganization) &&
                            e.CanBeCopied &&
                            e.GetType() != typeof(ViewSheet) &&
                            !string.IsNullOrEmpty(e.Name) &&
                            !string.IsNullOrEmpty(e.FamilyName))
                .Select(e =>
                {
                    try
                    {
                        if (e.Category != null)
                        {
                            return new BrowserItem(
                                e.Id.IntegerValue,
                                $"{e.Category.Name}{ModPlusAPI.Language.GetItem("m11")}",
                                e.FamilyName,
                                e.Name);
                        }

                        return new BrowserItem(
                            e.Id.IntegerValue,
                            noCategory,
                            e.FamilyName,
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.AddError(
                            string.Format(
                                ModPlusAPI.Language.GetItem("m1"),
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
                            ModPlusAPI.Language.GetItem("m59"),
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

            var group = ModPlusAPI.Language.GetItem("m63");

            foreach (var element in new FilteredElementCollector(revitDocument.Document)
                .OfClass(typeof(SharedParameterElement))
                .OfType<SharedParameterElement>())
            {
                try
                {
                    var browserItem = new BrowserItem(
                        element.Id.IntegerValue,
                        group,
                        LabelUtils.GetLabelFor(element.GetDefinition().ParameterGroup),
                        element.Name)
                    {
                        SecondRowValue = element.GuidValue.ToString(),
                        ShowSecondRow = true
                    };

                    parameters.Add(browserItem);
                }
                catch (Exception ex)
                {
                    Logger.Instance.AddError(string.Format(
                        ModPlusAPI.Language.GetItem("m1"),
                        DateTime.Now.ToLocalTime(),
                        element.Id.IntegerValue.ToString(),
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
                .Where(e => e.GetTypeId() != ElementId.InvalidElementId)
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
                        Logger.Instance.AddError(
                            string.Format(
                                ModPlusAPI.Language.GetItem("m1"),
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
                            ModPlusAPI.Language.GetItem("m10"),
                            "-",
                            e.Name)
                        {
                            IsWorksetId = true
                        };
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.AddError(
                            string.Format(
                                ModPlusAPI.Language.GetItem("m1"),
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
                .Where(e => e.GetTypeId() != ElementId.InvalidElementId)
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
                        Logger.Instance.AddError(
                            string.Format(
                                ModPlusAPI.Language.GetItem("m1"),
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
                .Where(e => e.GetTypeId() != ElementId.InvalidElementId)
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
                        Logger.Instance.AddError(
                            string.Format(
                                ModPlusAPI.Language.GetItem("m1"),
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
                .Where(e => !((View)e).IsTemplate && !(e is ViewSheet))
                .Where(e => e.GetTypeId() != ElementId.InvalidElementId)
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
                        Logger.Instance.AddError(
                            string.Format(
                                ModPlusAPI.Language.GetItem("m1"),
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
                            ModPlusAPI.Language.GetItem("m29"),
                            _viewTypeNames[viewType],
                            e.Name);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.AddError(
                            string.Format(
                                ModPlusAPI.Language.GetItem("m1"),
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
                .Where(e => e.GetTypeId() != ElementId.InvalidElementId)
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
                        Logger.Instance.AddError(
                            string.Format(
                                ModPlusAPI.Language.GetItem("m1"),
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
                .Where(e => e.GetTypeId() != ElementId.InvalidElementId)
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
                        Logger.Instance.AddError(
                            string.Format(
                                ModPlusAPI.Language.GetItem("m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                ex.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        private IEnumerable<BrowserItem> GetMaterials(RevitDocument revitDocument)
        {
            return new FilteredElementCollector(revitDocument.Document)
                .OfCategory(BuiltInCategory.OST_Materials)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .Select(e =>
                {
                    try
                    {
                        return new BrowserItem(
                            e.Id.IntegerValue,
                            e.Category.Name,
                            e.MaterialCategory,
                            e.Name);
                    }
                    catch (Exception exception)
                    {
                        Logger.Instance.AddError(
                            string.Format(
                                ModPlusAPI.Language.GetItem("m1"),
                                DateTime.Now.ToLocalTime(),
                                e.Id.IntegerValue,
                                exception.Message));
                        return null;
                    }
                })
                .Where(e => e != null);
        }

        #endregion
    }
}
