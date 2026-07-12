using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.AnswerOptions.Dtos;

public sealed class AnswerOptionOrderItem
{
    public Guid AnswerOptionId { get; set; }

    public int NewOrderIndex { get; set; }
}
