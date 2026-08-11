using System;
using System.Collections.Generic;

namespace AILA.Application.Common.Dtos.Rag;

public class SyncCourseRagResponseDto
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public int TotalMaterialsFound { get; set; }
    public int TotalMaterialsIndexed { get; set; }
    public int TotalChunksGenerated { get; set; }
    public string Status { get; set; } = "Success";
    public string Message { get; set; } = string.Empty;
    public List<IndexedMaterialSummaryDto> IndexedMaterials { get; set; } = new();
}

public class IndexedMaterialSummaryDto
{
    public Guid MaterialId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public int ChunksCount { get; set; }
    public string Status { get; set; } = "Completed";
}
