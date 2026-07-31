using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Categories.Commands.ChangeCategoryStatus;
using AILA.Application.Features.Categories.Commands.CreateCategory;
using AILA.Application.Features.Categories.Commands.ReorderCategories;
using AILA.Application.Features.Categories.Commands.UpdateCategory;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using Moq;

namespace AILA.Application.Tests.Features.Categories
{
    /// <summary>
    /// Sheet: CategoryService · UC-81 → UC-84 · TC-UNIT-CategoryService-001 → 013.
    /// </summary>
    public class CategoryHandlerTests
    {
        private readonly Mock<ICategoryRepository> _categories = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public CategoryHandlerTests()
        {
            _uow.Setup(u => u.Categories).Returns(_categories.Object);
        }

        private CreateCategoryCommandHandler CreateHandler() => new(_uow.Object);
        private UpdateCategoryCommandHandler UpdateHandler() => new(_uow.Object);
        private ChangeCategoryStatusCommandHandler StatusHandler() => new(_uow.Object);
        private ReorderCategoriesCommandHandler ReorderHandler() => new(_uow.Object);

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        // ============================================================ TC-001
        // Covers: Main Flow / BR-04 — danh mục mới LUÔN Inactive, admin bật Active riêng
        // qua UC-83. Đây là invariant của constructor Category, không phải mặc định tuỳ tiện.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-001")]
        [Trait("UC", "UC-81")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Create_Valid_InactiveCategorySaved()
        {
            _categories.Setup(r => r.ExistsByNameAsync("AI", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            Category? added = null;
            _categories.Setup(r => r.AddAsync(It.IsAny<Category>()))
                       .Callback<Category>(c => added = c)
                       .Returns(Task.CompletedTask);

            var result = await CreateHandler().Handle(
                new CreateCategoryCommand("AI", "Mô tả", 0), CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(result.Data!.IsActive);
            Assert.Equal("AI", result.Data.Name);
            Assert.NotNull(added);
            Assert.False(added!.IsActive);
            _categories.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-002
        // Covers: BR-01 unique name.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-002")]
        [Trait("UC", "UC-81")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Create_DuplicateName_RejectedNoSave()
        {
            _categories.Setup(r => r.ExistsByNameAsync("AI", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateHandler().Handle(
                new CreateCategoryCommand("AI", null, 0), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("CATEGORY_ALREADY_EXISTS", result.ErrorCode);
            _categories.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-003
        // Covers: BR-02 biên độ dài tên 2–100. Tách hai method: vi phạm và hợp lệ.
        [Theory]
        [InlineData(1)]     // min - 1
        [InlineData(101)]   // max + 1
        [Trait("TC", "TC-UNIT-CategoryService-003")]
        [Trait("UC", "UC-81")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Create_NameOutOfRange_InvalidName(int length)
        {
            var result = await CreateHandler().Handle(
                new CreateCategoryCommand(new string('a', length), null, 0), CancellationToken.None);

            Assert.Equal("INVALID_CATEGORY_NAME", result.ErrorCode);
            _categories.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Theory]
        [InlineData(2)]     // min
        [InlineData(100)]   // max
        [Trait("TC", "TC-UNIT-CategoryService-003")]
        [Trait("UC", "UC-81")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Create_NameLengthAtBoundary_IsAccepted(int length)
        {
            _categories.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);

            var result = await CreateHandler().Handle(
                new CreateCategoryCommand(new string('a', length), null, 0), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(length, result.Data!.Name.Length);
        }

        // Tên rỗng/khoảng trắng có mã lỗi RIÊNG, khác với tên sai độ dài.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [Trait("TC", "TC-UNIT-CategoryService-003")]
        [Trait("UC", "UC-81")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task Create_NameBlank_ReturnsNameRequired(string? name)
        {
            var result = await CreateHandler().Handle(
                new CreateCategoryCommand(name!, null, 0), CancellationToken.None);

            Assert.Equal("CATEGORY_NAME_REQUIRED", result.ErrorCode);
            VerifyNotSaved();
        }

        // Độ dài đo SAU Trim — khoảng trắng thừa không được tính là vi phạm.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-003")]
        [Trait("UC", "UC-81")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Low")]
        public async Task Create_PaddedNameAtMax_Accepted()
        {
            _categories.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);

            var result = await CreateHandler().Handle(
                new CreateCategoryCommand("   " + new string('a', 100) + "   ", null, 0), CancellationToken.None);

            Assert.True(result.Success);
        }

        // ============================================================ TC-004
        // Covers: BR-03 order index.
        // ⚠ OrderIndex là int (value type) nên "thiếu" sẽ mặc định 0 — vẫn hợp lệ. Không có
        // khái niệm "required" cho int, chỉ chặn được giá trị âm.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-004")]
        [Trait("UC", "UC-81")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task Create_NegativeOrder_InvalidOrderIndex()
        {
            var result = await CreateHandler().Handle(
                new CreateCategoryCommand("AI", null, -1), CancellationToken.None);

            Assert.Equal("INVALID_ORDER_INDEX", result.ErrorCode);
            _categories.Verify(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-004")]
        [Trait("UC", "UC-81")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Low")]
        public async Task Create_OrderIndexZero_IsAccepted()
        {
            _categories.Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);

            var result = await CreateHandler().Handle(
                new CreateCategoryCommand("AI", null, 0), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(0, result.Data!.OrderIndex);
        }

        // ============================================================ TC-005
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-005")]
        [Trait("UC", "UC-82")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Update_Valid_ChangesNameAndDesc()
        {
            var category = new CategoryBuilder().WithName("AI").WithDescription("cũ").Build();
            _categories.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
            _categories.Setup(r => r.ExistsByNameExceptIdAsync(
                           category.Id, "AI (sua)", It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await UpdateHandler().Handle(
                new UpdateCategoryCommand(category.Id, "AI (sua)", "mới"), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("AI (sua)", category.Name);
            Assert.Equal("mới", category.Description);
            Assert.NotNull(category.UpdatedAt);
            _categories.Verify(r => r.Update(category), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-005")]
        [Trait("UC", "UC-82")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Update_NotFound_RejectedNoSave()
        {
            _categories.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

            var result = await UpdateHandler().Handle(
                new UpdateCategoryCommand(Guid.NewGuid(), "AI", null), CancellationToken.None);

            Assert.Equal("CATEGORY_NOT_FOUND", result.ErrorCode);
            VerifyNotSaved();
        }

        // ============================================================ TC-006
        // Covers: BR-01 unique name khi cập nhật — so trùng phải LOẠI TRỪ chính nó, nếu không
        // thì đổi mô tả mà giữ nguyên tên sẽ bị chặn oan.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-006")]
        [Trait("UC", "UC-82")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Update_NameTakenByOther_AlreadyExists()
        {
            var category = new CategoryBuilder().WithName("AI").Build();
            _categories.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
            _categories.Setup(r => r.ExistsByNameExceptIdAsync(
                           category.Id, "Existing", It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await UpdateHandler().Handle(
                new UpdateCategoryCommand(category.Id, "Existing", null), CancellationToken.None);

            Assert.Equal("CATEGORY_ALREADY_EXISTS", result.ErrorCode);
            Assert.Equal("AI", category.Name);
            VerifyNotSaved();
        }

        // ============================================================ TC-007
        // Covers: BR-03 editable fields — UC-82 chỉ được sửa Name + Description.
        // OrderIndex đổi qua UC-84, IsActive đổi qua UC-83.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-007")]
        [Trait("UC", "UC-82")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Low")]
        public async Task Update_Always_KeepsOrderAndStatus()
        {
            var category = new CategoryBuilder().WithName("AI").WithOrderIndex(7).Active().Build();
            _categories.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
            _categories.Setup(r => r.ExistsByNameExceptIdAsync(
                           It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            await UpdateHandler().Handle(
                new UpdateCategoryCommand(category.Id, "AI mới", "mô tả mới"), CancellationToken.None);

            Assert.Equal(7, category.OrderIndex);
            Assert.True(category.IsActive);
        }

        // ============================================================ TC-008
        // Covers: BR-02 state transition Active ↔ Inactive.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-008")]
        [Trait("UC", "UC-83")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task ChangeStatus_Activate_TurnsCategoryActive()
        {
            var category = new CategoryBuilder().Build();   // mặc định Inactive
            _categories.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);

            var result = await StatusHandler().Handle(
                new ChangeCategoryStatusCommand(category.Id, true), CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(category.IsActive);
            Assert.True(result.Data!.IsActive);
            // Activate KHÔNG kiểm HasCourses — chỉ chiều Deactivate mới kiểm.
            _categories.Verify(r => r.HasCoursesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-008")]
        [Trait("UC", "UC-83")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task ChangeStatus_DeactivateUnused_Inactive()
        {
            var category = new CategoryBuilder().Active().Build();
            _categories.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
            _categories.Setup(r => r.HasCoursesAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await StatusHandler().Handle(
                new ChangeCategoryStatusCommand(category.Id, false), CancellationToken.None);

            Assert.True(result.Success);
            Assert.False(category.IsActive);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-008")]
        [Trait("UC", "UC-83")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task ChangeStatus_NotFound_RejectedNoSave()
        {
            _categories.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

            var result = await StatusHandler().Handle(
                new ChangeCategoryStatusCommand(Guid.NewGuid(), true), CancellationToken.None);

            Assert.Equal("CATEGORY_NOT_FOUND", result.ErrorCode);
            VerifyNotSaved();
        }

        // ============================================================ TC-009
        // Covers: BR-01 not assigned — không được ẩn danh mục đang có khoá học dùng, nếu không
        // các khoá học đó sẽ trỏ tới danh mục vô hình.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-009")]
        [Trait("UC", "UC-83")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task ChangeStatus_DeactivateInUse_Rejected()
        {
            var category = new CategoryBuilder().Active().Build();
            _categories.Setup(r => r.GetByIdAsync(category.Id)).ReturnsAsync(category);
            _categories.Setup(r => r.HasCoursesAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await StatusHandler().Handle(
                new ChangeCategoryStatusCommand(category.Id, false), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("CATEGORY_HAS_COURSES", result.ErrorCode);
            Assert.True(category.IsActive);
            VerifyNotSaved();
        }

        // ============================================================ TC-011
        // Covers: Main Flow / BR-02 — OrderIndex được gán theo VỊ TRÍ trong danh sách gửi lên.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-011")]
        [Trait("UC", "UC-84")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Reorder_Valid_IndexByPosition()
        {
            var c1 = new CategoryBuilder().WithName("C1").WithOrderIndex(0).Build();
            var c2 = new CategoryBuilder().WithName("C2").WithOrderIndex(1).Build();
            var c3 = new CategoryBuilder().WithName("C3").WithOrderIndex(2).Build();
            _categories.Setup(r => r.GetAllOrderedAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new[] { c1, c2, c3 });

            var result = await ReorderHandler().Handle(
                new ReorderCategoriesCommand(new List<Guid> { c3.Id, c1.Id, c2.Id }), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(0, c3.OrderIndex);
            Assert.Equal(1, c1.OrderIndex);
            Assert.Equal(2, c2.OrderIndex);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-012
        // Covers: AF-01 invalid order — rỗng / chứa Guid.Empty / có id trùng.
        // Ba trường hợp này bị chặn TRƯỚC khi chạm DB.
        public static TheoryData<List<Guid>> MalformedOrders
        {
            get
            {
                var dup = Guid.NewGuid();
                return new TheoryData<List<Guid>>
                {
                    new List<Guid>(),                              // rỗng
                    new List<Guid> { Guid.Empty },                 // chứa Guid rỗng
                    new List<Guid> { dup, dup },                   // trùng id
                };
            }
        }

        [Theory]
        [MemberData(nameof(MalformedOrders))]
        [Trait("TC", "TC-UNIT-CategoryService-012")]
        [Trait("UC", "UC-84")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Reorder_MalformedIds_RejectedNoDb(List<Guid> ids)
        {
            var result = await ReorderHandler().Handle(
                new ReorderCategoriesCommand(ids), CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("INVALID_ORDER", result.ErrorCode);
            _categories.Verify(r => r.GetAllOrderedAsync(It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-013
        // Covers: BR-03 each once — tập id gửi lên phải TRÙNG KHỚP tập id trong DB.
        // Thiếu một cái, thừa một cái, hay thay bằng id lạ đều bị chặn.
        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-013")]
        [Trait("UC", "UC-84")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Reorder_IdSetDoesNotMatchDatabase_IsRejected()
        {
            var c1 = new CategoryBuilder().WithName("C1").Build();
            var c2 = new CategoryBuilder().WithName("C2").Build();
            var c3 = new CategoryBuilder().WithName("C3").Build();
            _categories.Setup(r => r.GetAllOrderedAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new[] { c1, c2, c3 });

            // Đủ số lượng (3) nhưng c3 bị thay bằng một id lạ.
            var result = await ReorderHandler().Handle(
                new ReorderCategoriesCommand(new List<Guid> { c1.Id, c2.Id, Guid.NewGuid() }),
                CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("INVALID_ORDER", result.ErrorCode);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-CategoryService-013")]
        [Trait("UC", "UC-84")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Reorder_PartialList_IsRejected()
        {
            var c1 = new CategoryBuilder().WithName("C1").Build();
            var c2 = new CategoryBuilder().WithName("C2").Build();
            var c3 = new CategoryBuilder().WithName("C3").Build();
            _categories.Setup(r => r.GetAllOrderedAsync(It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new[] { c1, c2, c3 });

            var result = await ReorderHandler().Handle(
                new ReorderCategoriesCommand(new List<Guid> { c1.Id, c2.Id }), CancellationToken.None);

            Assert.Equal("INVALID_ORDER", result.ErrorCode);
            Assert.Equal(0, c1.OrderIndex);   // không có category nào bị đụng tới
            VerifyNotSaved();
        }
    }
}
