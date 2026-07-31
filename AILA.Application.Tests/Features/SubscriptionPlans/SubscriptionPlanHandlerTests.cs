using AILA.Application.Common.Exceptions;
using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.SubscriptionPlans;
using AILA.Application.Features.SubscriptionPlans.Commands.ChangeSubscriptionPlanStatus;
using AILA.Application.Features.SubscriptionPlans.Commands.CreateSubscriptionPlan;
using AILA.Application.Features.SubscriptionPlans.Commands.UpdateSubscriptionPlan;
using AILA.Application.Features.SubscriptionPlans.Queries.GetSubscriptionPlans;
using AILA.Domain.Entities;
using Moq;

namespace AILA.Application.Tests.Features.SubscriptionPlans
{
    /// <summary>
    /// Sheet: SubscriptionService · UC-90 Create / UC-91 Update / UC-92 Manage status.
    /// TC-UNIT-SubscriptionService-001 → 011.
    /// Handler dùng primary constructor, kiểm tra field bằng <see cref="SubscriptionPlanRules"/>
    /// TRƯỚC khi chạm repository, nên nhánh validate không được phép gọi ExistsByNameAsync.
    /// </summary>
    public class SubscriptionPlanHandlerTests
    {
        private readonly Mock<ISubscriptionPlanRepository> _plans = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public SubscriptionPlanHandlerTests() => _uow.Setup(u => u.SubscriptionPlans).Returns(_plans.Object);

        private static SubscriptionPlan Plan(
            string name = "Pro",
            int tierLevel = 2,
            int durationInDays = 30,
            decimal price = 199_000m,
            int displayOrder = 1)
            => new(name, "Gói tiêu chuẩn", price, tierLevel, durationInDays, 100_000, 20, 5, displayOrder);

        private static CreateSubscriptionPlanCommand CreateCmd(
            string name = "Pro",
            string? description = "Gói tiêu chuẩn",
            decimal price = 199_000m,
            int tierLevel = 2,
            int durationInDays = 30,
            int aiTokenLimit = 100_000,
            int scenarioLimit = 20,
            int evaluationLimit = 5,
            int displayOrder = 1)
            => new(name, description, price, tierLevel, durationInDays,
                   aiTokenLimit, scenarioLimit, evaluationLimit, displayOrder);

        private void NoDuplicates()
        {
            _plans.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);
            _plans.Setup(r => r.ExistsByTierLevelAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);
        }

        // ============================================================ UC-90 Create

        // ------------------------------------------------------------ TC-001
        // Covers: Main Flow. Gói mới sinh ra ở trạng thái Active (khác Category vốn mặc định ẩn).
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-001")]
        [Trait("UC", "UC-90")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Create_ValidPlan_Added()
        {
            NoDuplicates();
            SubscriptionPlan? added = null;
            _plans.Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>()))
                  .Callback<SubscriptionPlan>(p => added = p)
                  .Returns(Task.CompletedTask);

            var handler = new CreateSubscriptionPlanCommandHandler(_uow.Object);
            var result = await handler.Handle(CreateCmd(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(added);
            Assert.Equal("Pro", added!.Name);
            Assert.Equal(30, added.DurationInDays);
            Assert.True(added.IsActive());
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-002
        // Covers: Main Flow — tên được Trim trước khi kiểm tra trùng và trước khi lưu.
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-002")]
        [Trait("UC", "UC-90")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Create_TrimsNameBeforeUniquenessCheck()
        {
            NoDuplicates();
            SubscriptionPlan? added = null;
            _plans.Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>()))
                  .Callback<SubscriptionPlan>(p => added = p)
                  .Returns(Task.CompletedTask);

            var handler = new CreateSubscriptionPlanCommandHandler(_uow.Object);
            await handler.Handle(CreateCmd(name: "  Pro  "), CancellationToken.None);

            _plans.Verify(r => r.ExistsByNameAsync("Pro", It.IsAny<CancellationToken>()), Times.Once);
            Assert.Equal("Pro", added!.Name);
        }

        // ------------------------------------------------------------ TC-003
        // Covers: BR-01 tên trùng và BR-02 tier trùng. Tên được kiểm tra TRƯỚC tier.
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-003")]
        [Trait("UC", "UC-90")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Create_DuplicateNameOrTier_Rejected()
        {
            var handler = new CreateSubscriptionPlanCommandHandler(_uow.Object);

            // (a) trùng tên — chưa cần hỏi tới tier
            _plans.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
            var byName = await handler.Handle(CreateCmd(), CancellationToken.None);
            Assert.False(byName.Success);
            Assert.Equal(SubscriptionPlanErrors.NameAlreadyExists, byName.ErrorCode);
            _plans.Verify(r => r.ExistsByTierLevelAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

            // (b) tên tự do nhưng trùng cấp độ
            _plans.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);
            _plans.Setup(r => r.ExistsByTierLevelAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
            var byTier = await handler.Handle(CreateCmd(), CancellationToken.None);
            Assert.False(byTier.Success);
            Assert.Equal(SubscriptionPlanErrors.TierLevelAlreadyExists, byTier.ErrorCode);

            _plans.Verify(r => r.AddAsync(It.IsAny<SubscriptionPlan>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-004
        // Covers: BR-03 — mọi field được validate TRƯỚC khi chạm repository, và trả về mã lỗi
        // bám đúng field vi phạm thay vì để ArgumentException của domain bật lên thành 500.
        [Theory]
        [InlineData("", 2, 30, 199000, 0, 0, 0, 0, SubscriptionPlanErrors.NameRequired)]
        [InlineData("Pro", 0, 30, 199000, 0, 0, 0, 0, SubscriptionPlanErrors.InvalidTierLevel)]
        [InlineData("Pro", 2, 0, 199000, 0, 0, 0, 0, SubscriptionPlanErrors.InvalidDuration)]
        [InlineData("Pro", 2, 30, 0, 0, 0, 0, 0, SubscriptionPlanErrors.InvalidPrice)]
        [InlineData("Pro", 2, 30, 199000, -1, 0, 0, 0, SubscriptionPlanErrors.InvalidAiTokenLimit)]
        [InlineData("Pro", 2, 30, 199000, 0, -1, 0, 0, SubscriptionPlanErrors.InvalidAiPracticeScenarioLimit)]
        [InlineData("Pro", 2, 30, 199000, 0, 0, -1, 0, SubscriptionPlanErrors.InvalidExpertEvaluationLimit)]
        [InlineData("Pro", 2, 30, 199000, 0, 0, 0, -1, SubscriptionPlanErrors.InvalidDisplayOrder)]
        [Trait("TC", "TC-UNIT-SubscriptionService-004")]
        [Trait("UC", "UC-90")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Create_InvalidField_FailsBeforeRepository(
            string name, int tier, int duration, int price,
            int tokenLimit, int scenarioLimit, int evaluationLimit, int displayOrder,
            string expectedCode)
        {
            NoDuplicates();

            var handler = new CreateSubscriptionPlanCommandHandler(_uow.Object);
            var result = await handler.Handle(
                CreateCmd(name: name, price: price, tierLevel: tier, durationInDays: duration,
                          aiTokenLimit: tokenLimit, scenarioLimit: scenarioLimit,
                          evaluationLimit: evaluationLimit, displayOrder: displayOrder),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(expectedCode, result.ErrorCode);
            _plans.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _plans.Verify(r => r.AddAsync(It.IsAny<SubscriptionPlan>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-005
        // Covers: BR-04 biên độ dài Name (100) và Description (1000).
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-005")]
        [Trait("UC", "UC-90")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Create_NameAndDescriptionLengthBoundaries()
        {
            NoDuplicates();
            _plans.Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>())).Returns(Task.CompletedTask);
            var handler = new CreateSubscriptionPlanCommandHandler(_uow.Object);

            Assert.True((await handler.Handle(
                CreateCmd(name: new string('a', 100)), CancellationToken.None)).Success);

            var tooLongName = await handler.Handle(
                CreateCmd(name: new string('a', 101)), CancellationToken.None);
            Assert.Equal(SubscriptionPlanErrors.NameTooLong, tooLongName.ErrorCode);

            Assert.True((await handler.Handle(
                CreateCmd(description: new string('d', 1000)), CancellationToken.None)).Success);

            var tooLongDesc = await handler.Handle(
                CreateCmd(description: new string('d', 1001)), CancellationToken.None);
            Assert.Equal(SubscriptionPlanErrors.DescriptionTooLong, tooLongDesc.ErrorCode);
        }

        // ------------------------------------------------------------ TC-006
        // Covers: edge case — hai admin tạo trùng gần như đồng thời. Unique index ở DB chặn,
        // SaveChangesAsync ném DuplicateKeyException và handler dịch sang mã lỗi validation
        // thay vì để lộ lỗi hạ tầng ra ngoài.
        [Theory]
        [InlineData("IX_SubscriptionPlans_Name", SubscriptionPlanErrors.NameAlreadyExists)]
        [InlineData("IX_SubscriptionPlans_TierLevel", SubscriptionPlanErrors.TierLevelAlreadyExists)]
        [InlineData("IX_Something_Else", SubscriptionPlanErrors.ValidationError)]
        [Trait("TC", "TC-UNIT-SubscriptionService-006")]
        [Trait("UC", "UC-90")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Create_DuplicateKeyRace_MappedToValidationCode(
            string constraintName, string expectedCode)
        {
            NoDuplicates();
            _plans.Setup(r => r.AddAsync(It.IsAny<SubscriptionPlan>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DuplicateKeyException(constraintName));

            var handler = new CreateSubscriptionPlanCommandHandler(_uow.Object);
            var result = await handler.Handle(CreateCmd(), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(expectedCode, result.ErrorCode);
        }

        // ============================================================ UC-91 Update

        // ------------------------------------------------------------ TC-007
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-007")]
        [Trait("UC", "UC-91")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Update_ValidChange_Applied()
        {
            var plan = Plan();
            _plans.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

            var handler = new UpdateSubscriptionPlanCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new UpdateSubscriptionPlanCommand(plan.Id, "Mô tả mới", 249_000m, 200_000, 30, 8, 2),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(249_000m, plan.Price);
            Assert.Equal("Mô tả mới", plan.Description);
            Assert.Equal(2, plan.DisplayOrder);
            _plans.Verify(r => r.Update(plan), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-008
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-008")]
        [Trait("UC", "UC-91")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Update_PlanMissing_NotFound()
        {
            _plans.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((SubscriptionPlan?)null);

            var handler = new UpdateSubscriptionPlanCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new UpdateSubscriptionPlanCommand(Guid.NewGuid(), null, 1000m, 0, 0, 0, 0),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(SubscriptionPlanErrors.NotFound, result.ErrorCode);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-009
        // Covers: BR-01 — Name, TierLevel và DurationInDays là create-only. UpdateSubscriptionPlanCommand
        // KHÔNG có field nào cho chúng, và SubscriptionPlan.Update() cũng không nhận. Test này
        // khoá điều đó lại: nếu ai đó thêm field vào command mà quên rằng entity không cho sửa,
        // assert dưới đây sẽ chỉ ra ngay.
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-009")]
        [Trait("UC", "UC-91")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Update_CannotChangeCreateOnlyFields()
        {
            var plan = Plan(name: "Pro", tierLevel: 2, durationInDays: 30);
            _plans.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

            var handler = new UpdateSubscriptionPlanCommandHandler(_uow.Object);
            await handler.Handle(
                new UpdateSubscriptionPlanCommand(plan.Id, "Mô tả mới", 249_000m, 200_000, 30, 8, 2),
                CancellationToken.None);

            Assert.Equal("Pro", plan.Name);
            Assert.Equal(2, plan.TierLevel);
            Assert.Equal(30, plan.DurationInDays);
        }

        // ------------------------------------------------------------ TC-010
        // Covers: BR-02 — validate chạy TRƯỚC khi tìm gói, nên input sai không tốn một truy vấn.
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-010")]
        [Trait("UC", "UC-91")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task Update_InvalidField_FailsBeforeLookup()
        {
            var handler = new UpdateSubscriptionPlanCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new UpdateSubscriptionPlanCommand(Guid.NewGuid(), null, 0m, 0, 0, 0, 0),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(SubscriptionPlanErrors.InvalidPrice, result.ErrorCode);
            _plans.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        // ============================================================ UC-92 Status

        // ------------------------------------------------------------ TC-011
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-011")]
        [Trait("UC", "UC-92")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task ChangeStatus_ToggleBothWays()
        {
            var handler = new ChangeSubscriptionPlanStatusCommandHandler(_uow.Object);

            var plan = Plan();                       // sinh ra là Active
            _plans.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

            var off = await handler.Handle(
                new ChangeSubscriptionPlanStatusCommand(plan.Id, false), CancellationToken.None);
            Assert.True(off.Success);
            Assert.False(plan.IsActive());

            var on = await handler.Handle(
                new ChangeSubscriptionPlanStatusCommand(plan.Id, true), CancellationToken.None);
            Assert.True(on.Success);
            Assert.True(plan.IsActive());

            _plans.Verify(r => r.Update(plan), Times.Exactly(2));
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        // ------------------------------------------------------------ TC-012
        // Covers: BR-01 — đổi sang đúng trạng thái đang có là vô nghĩa, phải từ chối ở
        // tầng handler chứ không để entity ném ArgumentException.
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-012")]
        [Trait("UC", "UC-92")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task ChangeStatus_Redundant_Rejected()
        {
            var handler = new ChangeSubscriptionPlanStatusCommandHandler(_uow.Object);

            var active = Plan();
            _plans.Setup(r => r.GetByIdAsync(active.Id)).ReturnsAsync(active);
            var again = await handler.Handle(
                new ChangeSubscriptionPlanStatusCommand(active.Id, true), CancellationToken.None);
            Assert.False(again.Success);
            Assert.Equal(SubscriptionPlanErrors.AlreadyActive, again.ErrorCode);

            var inactive = Plan(name: "Basic", tierLevel: 1);
            inactive.Deactivate();
            _plans.Setup(r => r.GetByIdAsync(inactive.Id)).ReturnsAsync(inactive);
            var againOff = await handler.Handle(
                new ChangeSubscriptionPlanStatusCommand(inactive.Id, false), CancellationToken.None);
            Assert.False(againOff.Success);
            Assert.Equal(SubscriptionPlanErrors.AlreadyInactive, againOff.ErrorCode);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // ------------------------------------------------------------ TC-013
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-013")]
        [Trait("UC", "UC-92")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task ChangeStatus_PlanMissing_NotFound()
        {
            _plans.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((SubscriptionPlan?)null);

            var handler = new ChangeSubscriptionPlanStatusCommandHandler(_uow.Object);
            var result = await handler.Handle(
                new ChangeSubscriptionPlanStatusCommand(Guid.NewGuid(), true), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(SubscriptionPlanErrors.NotFound, result.ErrorCode);
        }

        // ------------------------------------------------------------ TC-014
        // Covers: Main Flow — danh sách quản trị KHÔNG lọc trạng thái, khác
        // GetActiveSubscriptionPlansQuery của UC-09 vốn chỉ trả gói đang bán.
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-014")]
        [Trait("UC", "UC-92")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task GetAll_IncludesInactivePlans()
        {
            var active = Plan(name: "Pro", tierLevel: 2, displayOrder: 1);
            var retired = Plan(name: "Legacy", tierLevel: 1, displayOrder: 2);
            retired.Deactivate();

            _plans.Setup(r => r.GetAllOrderedAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<SubscriptionPlan> { active, retired });

            var handler = new GetSubscriptionPlansQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetSubscriptionPlansQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.Count());
            Assert.Contains(result.Data, p => p.Name == "Legacy");
        }

        // ------------------------------------------------------------ TC-015
        [Fact]
        [Trait("TC", "TC-UNIT-SubscriptionService-015")]
        [Trait("UC", "UC-92")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Low")]
        public async Task GetAll_NoPlan_EmptyList()
        {
            _plans.Setup(r => r.GetAllOrderedAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<SubscriptionPlan>());

            var handler = new GetSubscriptionPlansQueryHandler(_uow.Object);
            var result = await handler.Handle(new GetSubscriptionPlansQuery(), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Empty(result.Data!);
        }
    }
}
