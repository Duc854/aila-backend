using AILA.Application.Features.DocumentMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.DocumentMaterials.Commands.UpdateDocumentDetail;

public sealed record UpdateDocumentDetailCommand(
    Guid MaterialId,
    Guid ExpertId,
    string Content
) : IRequest<ResponseDto<DocumentMaterialDto>>;
