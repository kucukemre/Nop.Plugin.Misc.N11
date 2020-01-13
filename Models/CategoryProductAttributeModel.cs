namespace Nop.Plugin.Misc.N11.Models
{
    public class CategoryProductAttributeModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool Mandatory { get; set; }
        public bool MultipleSelect { get; set; }
        public long CategoryId { get; set; }
    }
}