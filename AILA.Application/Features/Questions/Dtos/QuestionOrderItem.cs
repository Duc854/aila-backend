using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILA.Application.Features.Questions.Dtos;

public sealed class QuestionOrderItem
{
    public Guid QuestionId { get; set; }

    public int NewOrderIndex { get; set; }
}
