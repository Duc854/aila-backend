using AILA.Application.Features.DocumentMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.DocumentMaterials.Commands.CreateDocumentMaterial;

public sealed record CreateDocumentMaterialCommand(
    Guid ExpertId,
    Guid ModuleId,
    string Title,
    string Content
) : IRequest<ResponseDto<DocumentMaterialDto>>;
