using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Misc.N11.Models
{
    public class Category
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string TopLevelCategoryName { get; set; }
    }

    public class CategoryModel : Category
    {
        public CategoryModel()
        {
            SubCategories = new List<List<Category>>();
        }

        public List<List<Category>> SubCategories { get; set; }
    }
}