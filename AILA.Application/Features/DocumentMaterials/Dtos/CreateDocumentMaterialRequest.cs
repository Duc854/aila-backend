namespace AILA.Application.Features.DocumentMaterials.Dtos;

public sealed record CreateDocumentMaterialRequest(
    Guid ModuleId,
    string Title,
    string Content
);
