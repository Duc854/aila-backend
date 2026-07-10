using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Common.Dtos
{
    public class CourseLearningViewDto
    {
        public CourseProgressDto Progress { get; set; }
            = new();

        public List<ModuleLearningDto> Modules { get; set; }
            = [];
    }

    public class CourseProgressDto
    {
        public int CompletedMaterials { get; set; }

        public int TotalMaterials { get; set; }

        public double Percent { get; set; }

        public Guid? CurrentMaterialId { get; set; }
    }

    public class ModuleLearningDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = "";
        public int OrderIndex { get; set; }

        public List<MaterialLearningDto> Materials { get; set; }
            = [];
    }

    public class MaterialLearningDto
    {
        public Guid Id { get; set; }

        public Guid ModuleId { get; set; }
        public string Title { get; set; } = "";

        public string Type { get; set; } = "";
        public int OrderIndex { get; set; }

        public bool IsCompleted { get; set; }
    }
}
