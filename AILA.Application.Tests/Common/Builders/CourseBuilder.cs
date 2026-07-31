using AILA.Domain.Entities;
using AILA.Domain.Enums;

namespace AILA.Application.Tests.Common.Builders
{
    /// <summary>
    /// Dựng <see cref="Course"/> kèm đồ thị Module → Material qua đúng API domain
    /// (<c>AddModule</c> / <c>AddMaterial</c>), không chọc vào backing field.
    ///
    /// Lưu ý biên OrderIndex không đồng nhất giữa các entity:
    /// Module 1–999, Material &gt; 0, Category ≥ 0.
    /// </summary>
    public class CourseBuilder
    {
        private string _name = "AI Literacy 101";
        private Guid _categoryId = Guid.NewGuid();
        private Guid _expertId = Guid.NewGuid();
        private KnowledgeLevel _level = KnowledgeLevel.Beginner;
        private string? _description = "Mô tả khoá học";
        private bool _published;

        /// <summary>Mỗi phần tử = một module, giá trị = số material trong module đó.</summary>
        private readonly List<(string Title, int MaterialCount)> _modules = new();

        public CourseBuilder WithName(string name) { _name = name; return this; }
        public CourseBuilder WithCategory(Guid id) { _categoryId = id; return this; }
        public CourseBuilder OwnedBy(Guid expertId) { _expertId = expertId; return this; }
        public CourseBuilder WithLevel(KnowledgeLevel l) { _level = l; return this; }
        public CourseBuilder Published() { _published = true; return this; }

        public CourseBuilder WithModule(string title, int materialCount = 0)
        {
            _modules.Add((title, materialCount));
            return this;
        }

        public Course Build()
        {
            var course = new Course(_name, _categoryId, _expertId, _level, _description);

            var moduleOrder = 1;   // Module.OrderIndex bắt buộc ≥ 1
            foreach (var (title, materialCount) in _modules)
            {
                var module = new Module(course.Id, title, moduleOrder++, description: null);

                for (var i = 1; i <= materialCount; i++)
                    module.AddMaterial(Material.CreateDocument(module.Id, $"{title} - Bài {i}", i));

                course.AddModule(module);
            }

            if (_published)
                TestEntity.SetProperty(course, nameof(Course.IsPublished), true);

            return course;
        }
    }
}
