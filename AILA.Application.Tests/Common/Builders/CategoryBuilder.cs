using AILA.Domain.Entities;

namespace AILA.Application.Tests.Common.Builders
{
    /// <summary>
    /// Dựng <see cref="Category"/> cho test. Category mới LUÔN ở trạng thái Inactive
    /// (BR-04 của UC-81), muốn Active phải gọi <c>Activate()</c> — builder đi qua đúng API đó.
    /// </summary>
    public class CategoryBuilder
    {
        private string _name = "AI Literacy";
        private string? _description = "Mô tả danh mục";
        private int _orderIndex;
        private bool _active;
        private Guid? _id;

        public CategoryBuilder WithId(Guid id) { _id = id; return this; }
        public CategoryBuilder WithName(string name) { _name = name; return this; }
        public CategoryBuilder WithDescription(string? d) { _description = d; return this; }
        public CategoryBuilder WithOrderIndex(int i) { _orderIndex = i; return this; }
        public CategoryBuilder Active() { _active = true; return this; }

        public Category Build()
        {
            var category = new Category(_name, _description, _orderIndex);

            if (_active)
                category.Activate();

            if (_id.HasValue)
                TestEntity.SetId(category, _id.Value);

            return category;
        }
    }
}
