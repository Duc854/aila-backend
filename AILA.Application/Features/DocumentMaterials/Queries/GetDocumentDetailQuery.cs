using AILA.Application.Features.DocumentMaterials.Dtos;
using MediatR;
using Shared.Wrappers;

namespace AILA.Application.Features.DocumentMaterials.Queries.GetDocumentDetail;

public sealed record GetDocumentDetailQuery(
    Guid MaterialId,
    Guid ExpertId
) : IRequest<ResponseDto<DocumentMaterialDto>>;