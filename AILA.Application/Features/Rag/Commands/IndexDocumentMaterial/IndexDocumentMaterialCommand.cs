using AILA.Application.Common.Dtos.Rag;
using MediatR;
using System;

namespace AILA.Application.Features.Rag.Commands.IndexDocumentMaterial;

public record IndexDocumentMaterialCommand(
    Guid MaterialId,
    Guid CourseId,
    string MaterialTitle,
    string ContentText
) : IRequest<IndexDocumentResponseDto>;