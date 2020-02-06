using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using N11CategoryService;
using N11ProductService;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.N11.Models;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.ExportImport.Help;
using Nop.Services.Media;
using Nop.Services.Messages;

namespace Nop.Plugin.Misc.N11.Services
{
    public class N11Manager
    {
        #region Fields

        private readonly IStoreContext _storeContext;
        private readonly ISettingService _settingService;
        private readonly N11Settings _n11Settings;
        private readonly IProductService _productService;
        private readonly IPictureService _pictureService;
        private readonly CatalogSettings _catalogSettings;
        private BlockingCollection<List<SaveProductRequest>> BlockingCollection { get; set; }
        private List<(NotifyType, string)> Messages { get; set; }
        private int MaxDegreeOfParallelN11Import
        {
            get
            {
                int count = 1;
                return count;
            }
        }

        #endregion

        #region Ctor

        public N11Manager(IStoreContext storeContext, ISettingService settingService, N11Settings n11Settings, IProductService productService, IPictureService pictureService, CatalogSettings catalogSettings)
        {
            this._storeContext = storeContext;
            this._settingService = settingService;
            this._n11Settings = n11Settings;
            this._productService = productService;
            this._pictureService = pictureService;
            this._catalogSettings = catalogSettings;
            Messages = new List<(NotifyType, string)>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Synchronize contacts 
        /// </summary>
        /// <returns>List of messages</returns>
        public IList<(NotifyType Type, string Message)> Synchronize()
        {
            try
            {
                //n11 plugin is configured
                var options = N11Helper.GetN11AuthenticationSettings(_n11Settings);
                if (string.IsNullOrEmpty(options.appKey) || string.IsNullOrEmpty(options.appSecret))
                    throw new NopException($"Plugin not configured");

                var client = new ProductServicePortClient();

                BlockingCollection = new BlockingCollection<List<SaveProductRequest>>();
                var taskArray = new Task[]
                {
                    Task.Factory.StartNew(() => Producer()),
                    Task.Factory.StartNew(() => Consumer(client))
                };
                Task.WaitAll(taskArray);
            }
            catch (Exception exception)
            {
                Messages.Add((NotifyType.Error, $"N11 ProductImport error: {exception.Message}"));
            }

            return Messages;
        }

        /// <summary>
        /// Export categories to XLSX
        /// </summary>
        /// <returns>Result in XLSX format</returns>
        public virtual byte[] ExportN11CategoriesToXlsx()
        {
            var categories = GetCategories();

            var manager = new PropertyManager<Models.Category>(new[]
            {
                new PropertyByName<Models.Category>("Id", p => p.Id),
                new PropertyByName<Models.Category>("Name", p => p.Name),
                new PropertyByName<Models.Category>("TopLevelCategoryName", p => p.TopLevelCategoryName),
            }, _catalogSettings);

            var listforExcel = new List<CategoryModel>();
            categories.ForEach(x => x.SubCategories.ForEach(y => y.ForEach(z => listforExcel.Add(new CategoryModel { Id = z.Id, Name = z.Name, TopLevelCategoryName = z.TopLevelCategoryName }))));

            return manager.ExportToXlsx(listforExcel);
        }

        /// <summary>
        /// Export categories attribute to XLSX
        /// </summary>
        /// <returns>Result in XLSX format</returns>
        public virtual byte[] ExportN11CategoriesAttributesToXlsx()
        {
            var categoriesAttributes = GetCategoriesAttributes();

            var manager = new PropertyManager<Models.CategoryProductAttributeModel>(new[]
            {
                new PropertyByName<Models.CategoryProductAttributeModel>("Id", p => p.Id),
                new PropertyByName<Models.CategoryProductAttributeModel>("Name", p => p.Name),
                new PropertyByName<Models.CategoryProductAttributeModel>("Mandatory", p => p.Mandatory),
                new PropertyByName<Models.CategoryProductAttributeModel>("MultipleSelect", p => p.MultipleSelect),
                new PropertyByName<Models.CategoryProductAttributeModel>("CategoryId", p => p.CategoryId),
            }, _catalogSettings);

            return manager.ExportToXlsx(categoriesAttributes);
        }

        #endregion

        #region Utilities

        private void Producer()
        {
            var pageSize = 10000;
            var totalProduct = _productService.GetNumberOfProductsInCategory();
            var pageCount = (totalProduct / pageSize) + 1;
            var storeId = _storeContext.ActiveStoreScopeConfiguration;
            var sw = Stopwatch.StartNew();

            for (int index = 0; index < pageCount; index++)
            {
                try
                {
                    var n11Settings = _settingService.LoadSetting<N11Settings>(storeId);

                    GetSaveProductRequestAndBlockingCollection(index, pageSize, n11Settings);

                    if (!String.IsNullOrEmpty(n11Settings.FailedProductIds))
                    {
                        try
                        {
                            var productIds = n11Settings.FailedProductIds.Split(";").Where(x => !String.IsNullOrEmpty(x)).ToArray();

                            GetSaveProductRequestAndBlockingCollection(index, pageSize, n11Settings, productIds);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    Messages.Add((NotifyType.Success, $"Producer index:{index} - Time:{sw.Elapsed.TotalSeconds} s"));
                }
                catch (Exception ex)
                {
                    Messages.Add((NotifyType.Error, String.Format($"Producer index:{index} - Error: {0}", ex.Message)));
                }
            }

            BlockingCollection.CompleteAdding();
        }

        private void Consumer(ProductServicePortClient client)
        {
            Parallel.ForEach(BlockingCollection.GetConsumingEnumerable(), new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelN11Import }, (item, state, index) =>
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    var lastItem = item.Last();
                    foreach (var saveProductRequest in item)
                    {
                        var lastAddedProductId = InsertProduct(client, saveProductRequest);

                        SetLastAddedProductIdSetting(lastAddedProductId);

                        if (!item.Equals(lastItem))
                            Thread.Sleep(60 * 1000);
                    }

                    sw.Stop();
                }
                catch (Exception exception)
                {
                    Messages.Add((NotifyType.Error, $"N11 ProductImport error: {exception.Message}"));
                }
            });
        }

        private int InsertProduct(ProductServicePortClient client, SaveProductRequest saveProductRequest)
        {
            try
            {
                var response = client.SaveProductAsync(saveProductRequest);

                if (response.Result.SaveProductResponse.result.status.Contains("success"))
                {
                    CheckFailedProductIdsSetting(int.Parse(saveProductRequest.product.productSellerCode));
                    Messages.Add((NotifyType.Success, $"N11 ProductImport success"));
                    return int.Parse(saveProductRequest.product.productSellerCode);
                }
                else
                {
                    SetFailedProductIdsSetting(int.Parse(saveProductRequest.product.productSellerCode));
                    Messages.Add((NotifyType.Error, $"N11 ProductImport error: {response.Result.SaveProductResponse.result.errorMessage}"));
                    return 0;
                }
            }
            catch (Exception)
            {
                throw new NopException($"GittiGidiyor Product Service Error");
            }
        }

        private void GetSaveProductRequestAndBlockingCollection(int index, int size, N11Settings n11Settings, string[] productIds = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();

                IEnumerable<Nop.Core.Domain.Catalog.Product> products = null;

                if (productIds == null)
                {
                    products = _productService.SearchProducts(
                    pageIndex: index,
                    pageSize: size
                    ).Where(x => x.Id > n11Settings.LastAddedProductId);
                }
                else
                {
                    products = _productService.SearchProducts(
                    pageIndex: index,
                    pageSize: size
                    ).Where(x => productIds.Contains(x.Id.ToString()));
                }

                if (products.Any())
                {
                    var saveProductRequestList = new List<SaveProductRequest>();

                    foreach (var product in products)
                    {
                        var saveProductRequest = PrepareSaveProductRequest(product);

                        saveProductRequestList.Add(saveProductRequest);
                    }

                    BlockingCollection.Add(saveProductRequestList);
                }

                Messages.Add((NotifyType.Success, $"#{Thread.CurrentThread.ManagedThreadId.ToString()} Product count:{products.Count()} added {sw.Elapsed.TotalSeconds}"));
            }
            catch (Exception ex)
            {
                Messages.Add((NotifyType.Error, String.Format($"GetSaveProductRequestAndBlockingCollection index:{index} - Error: {0}", ex.Message)));
            }
        }

        private SaveProductRequest PrepareSaveProductRequest(Core.Domain.Catalog.Product product)
        {
            #region prepareProduct

            #region Pictures

            var productImages = new List<ProductImage>();

            var productPictures = _pictureService.GetPicturesByProductId(product.Id);

            foreach (var picture in productPictures)
            {
                var pictureModel = new ProductImage
                {
                    url = _pictureService.GetPictureUrl(picture),
                    order = "0"
                };

                productImages.Add(pictureModel);
            }

            #endregion

            #region Options & Properties

            string targetBrandName = "";

            var brandIdMapping = _n11Settings.BrandIdMapping;

            if (!String.IsNullOrEmpty(brandIdMapping))
            {
                try
                {
                    var results = brandIdMapping.Split(";");

                    for (int i = 0; i < results.Length; i++)
                    {
                        var nopBrand = results[i].Split("=>")[0];

                        if (product.ProductManufacturers.Select(x => x.Id).Contains(int.Parse(nopBrand)))
                        {
                            targetBrandName = results[i].Split("=>")[1];
                            break;
                        }
                    }

                    if (targetBrandName == "")
                        targetBrandName = _n11Settings.DefaultBrandName;
                }
                catch (Exception)
                {
                    targetBrandName = _n11Settings.DefaultBrandName;
                }
            }
            else
            {
                targetBrandName = _n11Settings.DefaultBrandName;
            }

            var attributes = new List<ProductAttributeRequest>();

            attributes.Add(new ProductAttributeRequest
            {
                name = "Marka",
                value = targetBrandName
            });

            var stockItems = new List<ProductSkuRequest>();

            if (stockItems.Count <= 0)
            {
                stockItems.Add(new ProductSkuRequest
                {
                    optionPrice = product.OldPrice > product.Price ? product.OldPrice : product.Price,
                    quantity = product.StockQuantity.ToString(),
                    sellerStockCode = product.Sku
                });
            }

            #endregion

            #region Discount

            var discountRequest = new ProductDiscountRequest
            {
                type = "3",
                value = product.Price.ToString().Replace(",", ".")
            };

            #endregion

            #region Category Mapping

            long targetCategoryId = 0;

            var categoryIdMapping = _n11Settings.CategoryIdMapping;

            if (!String.IsNullOrEmpty(categoryIdMapping))
            {
                try
                {
                    var results = categoryIdMapping.Split(";");

                    for (int i = 0; i < results.Length; i++)
                    {
                        var nopCat = results[i].Split("=>")[0];

                        if (product.ProductCategories.Select(x => x.Id).Contains(int.Parse(nopCat)))
                        {
                            targetCategoryId = long.Parse(results[i].Split("=>")[1]);
                            break;
                        }
                    }

                    if (targetCategoryId == 0)
                        targetCategoryId = _n11Settings.DefaultCategoryId;
                }
                catch (Exception)
                {
                    targetCategoryId = _n11Settings.DefaultCategoryId;
                }
            }
            else
            {
                targetCategoryId = _n11Settings.DefaultCategoryId;
            }

            #endregion

            var productRequest = new ProductRequest
            {
                productSellerCode = product.Id.ToString(),
                title = product.Name,
                subtitle = !String.IsNullOrEmpty(product.ShortDescription) ? product.ShortDescription : product.Name,
                description = !String.IsNullOrEmpty(product.FullDescription) ? product.FullDescription : product.Name,
                category = new CategoryRequest
                {
                    id = targetCategoryId
                },
                price = product.OldPrice > product.Price ? product.OldPrice : product.Price,
                discount = product.OldPrice > product.Price ? discountRequest : null,
                stockItems = stockItems.ToArray(),
                images = productImages.Take(8).ToArray(),
                attributes = attributes.ToArray(),
                productCondition = "1",
                approvalStatus = _n11Settings.ApprovalStatus,
                currencyType = _n11Settings.CurrencyType,
                shipmentTemplate = _n11Settings.ShipmentTemplate,
                preparingDay = _n11Settings.PreparingDay
            };

            #endregion

            var options = N11Helper.GetN11AuthenticationSettings(_n11Settings);
            var saveProductRequest = new SaveProductRequest
            {
                product = productRequest,
                auth = options
            };

            return saveProductRequest;
        }

        private List<CategoryModel> GetCategories()
        {
            List<CategoryModel> categories = new List<CategoryModel>();

            var options = N11Helper.GetN11AuthenticationSettingsForCategory(_n11Settings);

            var getTopLevelCategoriesClient = new CategoryServicePortClient();
            var getTopLevelCategoriesRequest = new GetTopLevelCategoriesRequest
            {
                auth = options
            };
            var getTopLevelCategoriesResponse = getTopLevelCategoriesClient.GetTopLevelCategoriesAsync(getTopLevelCategoriesRequest);
            if (getTopLevelCategoriesResponse.Result.GetTopLevelCategoriesResponse.result.status.Contains("success"))
            {
                var categoriesResponse = getTopLevelCategoriesResponse.Result.GetTopLevelCategoriesResponse.categoryList;

                var getSubCategoriesClient = new CategoryServicePortClient();
                foreach (var category in categoriesResponse)
                {
                    var categoryModel = new CategoryModel()
                    {
                        Id = category.id,
                        Name = category.name
                    };

                    GetSubCategories(category.id, category.name, categoryModel.SubCategories, getSubCategoriesClient);

                    categories.Add(categoryModel);
                }
            }

            return categories;
        }

        private void GetSubCategories(long topLevelCategoryId, string topLevelCategoryName, List<List<Models.Category>> subCategories, CategoryServicePortClient client)
        {
            var options = N11Helper.GetN11AuthenticationSettingsForCategory(_n11Settings);

            var getSubCategoriesRequest = new GetSubCategoriesRequest
            {
                auth = options,
                categoryId = topLevelCategoryId
            };

            var getSubCategoriesResponse = client.GetSubCategoriesAsync(getSubCategoriesRequest);

            if (getSubCategoriesResponse.Result.GetSubCategoriesResponse.result.status.Contains("success"))
            {
                foreach (var category in getSubCategoriesResponse.Result.GetSubCategoriesResponse.category)
                {
                    if (category.subCategoryList != null)
                    {
                        var subList = new List<Models.Category>();
                        foreach (var subCategory in category.subCategoryList)
                        {
                            subList.Add(new Models.Category
                            {
                                Id = subCategory.id,
                                Name = subCategory.name,
                                TopLevelCategoryName = topLevelCategoryName
                            });
                        }
                        subCategories.Add(subList);
                        foreach (var subCategory in subList)
                        {
                            GetSubCategories(subCategory.Id, subCategory.Name, subCategories, client);
                        }
                    }
                }
            }
        }

        private List<CategoryProductAttributeModel> GetCategoriesAttributes()
        {
            var categories = GetCategories();

            var categoriesAttributes = new List<CategoryProductAttributeModel>();

            var mergedCategoriesIds = new List<long>();
            categories.ForEach(x => x.SubCategories.ForEach(y => y.ForEach(z => mergedCategoriesIds.Add(z.Id))));

            var options = N11Helper.GetN11AuthenticationSettingsForCategory(_n11Settings);

            var getCategoryServiceClient = new CategoryServicePortClient();

            foreach (var id in mergedCategoriesIds)
            {
                var getCategoryAttributesIdRequest = new GetCategoryAttributesIdRequest
                {
                    auth = options,
                    categoryId = id
                };

                var getCategoryAttributesIdResponse = getCategoryServiceClient.GetCategoryAttributesIdAsync(getCategoryAttributesIdRequest);

                if (getCategoryAttributesIdResponse.Result.GetCategoryAttributesIdResponse.result.status.Contains("success"))
                {
                    var categoryProductAttributeListResponse = getCategoryAttributesIdResponse.Result.GetCategoryAttributesIdResponse.categoryProductAttributeList;

                    if (categoryProductAttributeListResponse != null)
                    {
                        foreach (var categoryProductAttribute in categoryProductAttributeListResponse)
                        {
                            var attribute = new CategoryProductAttributeModel()
                            {
                                Id = categoryProductAttribute.id,
                                Name = categoryProductAttribute.name,
                                Mandatory = categoryProductAttribute.mandatory,
                                MultipleSelect = categoryProductAttribute.multipleSelect,
                                CategoryId = id
                            };

                            if (!categoriesAttributes.Contains(attribute))
                            {
                                categoriesAttributes.Add(attribute);
                            }
                        }
                    }
                }
            }

            return categoriesAttributes;
        }

        private void SetLastAddedProductIdSetting(int lastAddedProductId)
        {
            if (lastAddedProductId > 0)
            {
                var storeId = _storeContext.ActiveStoreScopeConfiguration;
                var n11Settings = _settingService.LoadSetting<N11Settings>(storeId);
                if (lastAddedProductId > n11Settings.LastAddedProductId)
                {
                    n11Settings.LastAddedProductId = lastAddedProductId;
                    _settingService.SaveSetting(n11Settings, settings => settings.LastAddedProductId, clearCache: false);
                    _settingService.ClearCache();
                }
            }
        }

        private void SetFailedProductIdsSetting(int failedProductId)
        {
            if (failedProductId > 0)
            {
                var storeId = _storeContext.ActiveStoreScopeConfiguration;
                var n11Settings = _settingService.LoadSetting<N11Settings>(storeId);
                n11Settings.FailedProductIds += (failedProductId + ";");
                _settingService.SaveSetting(n11Settings, settings => settings.LastAddedProductId, clearCache: false);
                _settingService.ClearCache();
            }
        }

        private void CheckFailedProductIdsSetting(int productId)
        {
            if (productId > 0)
            {
                var storeId = _storeContext.ActiveStoreScopeConfiguration;
                var n11Settings = _settingService.LoadSetting<N11Settings>(storeId);
                n11Settings.FailedProductIds = n11Settings.FailedProductIds.Replace(productId + ";", "");
                _settingService.SaveSetting(n11Settings, settings => settings.FailedProductIds, clearCache: false);
                _settingService.ClearCache();
            }
        }

        #endregion
    }
}