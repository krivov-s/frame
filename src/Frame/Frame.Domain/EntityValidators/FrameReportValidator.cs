using FluentValidation;
using Frame.Domain.Entities.Core.Reports;
using Frame.Domain.Entities.Core.Scripting;

namespace Frame.Domain.EntityValidators
{
    public class FrameReportValidator : BaseEntityValidator<FrameReport>
    {
        public FrameReportValidator()
        {
            RuleFor(x => x).Custom(CheckFrameReport);
        }

        protected void CheckFrameReport(FrameReport rep, ValidationContext<FrameReport> ctx)
        {
            if (rep is { ScriptCommandId: not null, SavedQueryId: not null })
            {
                ctx.AddFailure(
                    $"Отчет {rep.Name}: должен быть задан только один источник данных, или запрос или операция!");
                return;
            }

            if (rep.ScriptCommand != null && rep.ScriptCommand.CommandType != ECommandType.DataSource)
            {
                ctx.AddFailure(
                    $"Отчет {rep.Name}: если источник данных - операция, ее тип должен быть {nameof(ECommandType.DataSource)}!");
            }
        }
    }
}