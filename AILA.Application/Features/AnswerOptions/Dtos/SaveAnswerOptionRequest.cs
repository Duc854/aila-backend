using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AnswerOptions.Dtos;

public sealed class SaveAnswerOptionRequest
{
    public string Content { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}
