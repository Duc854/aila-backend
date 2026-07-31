using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Modules.Commands;
using AILA.Application.Features.Modules.Dtos;
using AILA.Application.Tests.Common;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using Moq;

namespace AILA.Application.Tests.Features.Modules
{
    /// <summary>
    /// Sheet: ModuleService · UC-39 → UC-42 · TC-UNIT-ModuleService-001 → 011.
    ///
    /// Mọi handler đều kiểm quyền sở hữu qua <c>Course.ExpertId</c> trước khi làm gì khác.
    /// </summary>
    public class ModuleHandlerTests
    {
        private static readonly Guid OwnerId = Guid.NewGuid();
        private static readonly Guid OtherExpertId = Guid.NewGuid();

        private readonly Mock<ICourseRepository> _courses = new();
        private readonly Mock<IModuleRepository> _modules = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public ModuleHandlerTests()
        {
            _uow.Setup(u => u.Courses).Returns(_courses.Object);
            _uow.Setup(u => u.Modules).Returns(_modules.Object);
        }

        private CreateModuleCommandHandler CreateHandler() => new(_uow.Object);
        private UpdateModuleCommandHandler UpdateHandler() => new(_uow.Object);
        private DeleteModuleCommandHandler DeleteHandler() => new(_uow.Object);
        private ReorderModulesCommandHandler ReorderHandler() => new(_uow.Object);
        private SetModulePublishCommandHandler PublishHandler() => new(_uow.Object);

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        /// <summary>Module rời kèm navigation Course, đúng dạng repo GetWithCourseAsync trả về.</summary>
        private static Module ModuleWithCourse(Course course, string title = "Chương mở đầu", int order = 1)
        {
            var module = new Module(course.Id, title, order);
            TestEntity.SetProperty(module, nameof(Module.Course), course);
            return module;
        }

        // ============================================================ TC-001
        // Covers: Main Flow / BR-02.
        // ⚠ BR-02 nói "thêm vào cuối syllabus" nhưng handler dùng nguyên OrderIndex do caller
        // truyền — KHÔNG tự tính vị trí cuối. Vị trí do FE quyết định (xem DEF-MOD-02).
        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-001")]
        [Trait("UC", "UC-39")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Create_ByOwner_UsesCallerOrderIndex()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

            Module? added = null;
            _modules.Setup(r => r.AddAsync(It.IsAny<Module>()))
                    .Callback<Module>(m => added = m)
                    .Returns(Task.CompletedTask);

            var result = await CreateHandler().Handle(
                new CreateModuleCommand(course.Id, OwnerId, "Chương mở đầu", "Mô tả", 3),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(added);
            Assert.Equal("Chương mở đầu", added!.Title);
            Assert.Equal(3, added.OrderIndex);          // đúng giá trị caller truyền, không phải "cuối"
            Assert.Equal(course.Id, added.CourseId);
            _modules.Verify(r => r.AddAsync(It.IsAny<Module>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-002")]
        [Trait("UC", "UC-39")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Create_CourseNotFound_ReturnsCourseNotFound()
        {
            _courses.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Course?)null);

            var result = await CreateHandler().Handle(
                new CreateModuleCommand(Guid.NewGuid(), OwnerId, "Chương mở đầu", null, 1),
                CancellationToken.None);

            Assert.Equal("COURSE_NOT_FOUND", result.ErrorCode);
            VerifyNotSaved();
        }

        // Ownership: expert khác không được thêm chương vào khoá học không phải của mình.
        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-002")]
        [Trait("UC", "UC-39")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Create_ByNonOwner_ForbiddenNoCreate()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

            var result = await CreateHandler().Handle(
                new CreateModuleCommand(course.Id, OtherExpertId, "Chương mở đầu", null, 1),
                CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            _modules.Verify(r => r.AddAsync(It.IsAny<Module>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-002
        // Covers: BR-01 title required. Không có validator cho command; domain là hàng rào duy
        // nhất và handler KHÔNG bắt ArgumentException → nó nổi ra ngoài.
        // Biên thật của Module: Title 5–255, OrderIndex 1–999 (workbook ghi 0–999 là sai).
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abcd")]      // 4 ký tự — biên - 1
        [Trait("TC", "TC-UNIT-ModuleService-002")]
        [Trait("UC", "UC-39")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Create_TitleOutOfRange_ThrowsNoSave(string title)
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => CreateHandler().Handle(
                    new CreateModuleCommand(course.Id, OwnerId, title, null, 1), CancellationToken.None));

            Assert.Equal("title", ex.ParamName);
            _modules.Verify(r => r.AddAsync(It.IsAny<Module>()), Times.Never);
            VerifyNotSaved();
        }

        [Theory]
        [InlineData(0)]      // biên dưới - 1 (0 KHÔNG hợp lệ, khác Category)
        [InlineData(-1)]
        [InlineData(1000)]   // biên trên + 1
        [Trait("TC", "TC-UNIT-ModuleService-002")]
        [Trait("UC", "UC-39")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Create_OrderIndexOutOfRange_ThrowsFromDomain(int orderIndex)
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => CreateHandler().Handle(
                    new CreateModuleCommand(course.Id, OwnerId, "Chương mở đầu", null, orderIndex),
                    CancellationToken.None));

            Assert.Equal("orderIndex", ex.ParamName);
            VerifyNotSaved();
        }

        [Theory]
        [InlineData(1)]      // biên dưới
        [InlineData(999)]    // biên trên
        [Trait("TC", "TC-UNIT-ModuleService-002")]
        [Trait("UC", "UC-39")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Create_OrderIndexAtBoundary_IsAccepted(int orderIndex)
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);

            var result = await CreateHandler().Handle(
                new CreateModuleCommand(course.Id, OwnerId, "Chương mở đầu", null, orderIndex),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(orderIndex, result.Data!.OrderIndex);
        }

        // ============================================================ TC-003
        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-003")]
        [Trait("UC", "UC-40")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Update_ByOwner_ChangesTitleAndDesc()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var module = ModuleWithCourse(course, "Chương cũ");
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>())).ReturnsAsync(module);

            var result = await UpdateHandler().Handle(
                new UpdateModuleCommand(module.Id, OwnerId, "Chương 1 (sửa)", "Mô tả mới"),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Chương 1 (sửa)", module.Title);
            Assert.Equal("Mô tả mới", module.Description);
            Assert.NotNull(module.UpdatedAt);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-004")]
        [Trait("UC", "UC-40")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Update_ModuleNotFound_ReturnsModuleNotFound()
        {
            _modules.Setup(r => r.GetWithCourseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Module?)null);

            var result = await UpdateHandler().Handle(
                new UpdateModuleCommand(Guid.NewGuid(), OwnerId, "Chương 1 (sửa)", null),
                CancellationToken.None);

            Assert.Equal("MODULE_NOT_FOUND", result.ErrorCode);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-004")]
        [Trait("UC", "UC-40")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Update_ByNonOwner_ForbiddenKeepsTitle()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var module = ModuleWithCourse(course, "Chương cũ");
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>())).ReturnsAsync(module);

            var result = await UpdateHandler().Handle(
                new UpdateModuleCommand(module.Id, OtherExpertId, "Chương bị chiếm", null),
                CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            Assert.Equal("Chương cũ", module.Title);
            VerifyNotSaved();
        }

        // ============================================================ TC-004
        [Theory]
        [InlineData("")]
        [InlineData("abcd")]
        [Trait("TC", "TC-UNIT-ModuleService-004")]
        [Trait("UC", "UC-40")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Update_TitleOutOfRange_ThrowsNoSave(string title)
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var module = ModuleWithCourse(course, "Chương cũ");
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>())).ReturnsAsync(module);

            await Assert.ThrowsAsync<ArgumentException>(
                () => UpdateHandler().Handle(
                    new UpdateModuleCommand(module.Id, OwnerId, title, null), CancellationToken.None));

            Assert.Equal("Chương cũ", module.Title);   // không bị đổi một phần
            VerifyNotSaved();
        }

        // ============================================================ TC-005
        // Covers: Main Flow.
        // ⚠ BR-04 "reindex module còn lại sau khi xoá" CHƯA implement — handler chỉ Delete
        // rồi Save, không đụng tới OrderIndex của các module khác (xem DEF-MOD-03).
        [Fact(Skip = "DEF-MOD-03 - DeleteModule does not reindex the remaining modules")]
        [Trait("TC", "TC-UNIT-ModuleService-005")]
        [Trait("UC", "UC-41")]
        [Trait("BR", "BR-04")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-MOD-03")]
        public async Task Delete_ByOwner_DeletesNoReindex()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var module = ModuleWithCourse(course);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>())).ReturnsAsync(module);

            var result = await DeleteHandler().Handle(
                new DeleteModuleCommand(module.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            _modules.Verify(r => r.Delete(module), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-006")]
        [Trait("UC", "UC-41")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Delete_ByNonOwner_ForbiddenNoDelete()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var module = ModuleWithCourse(course);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>())).ReturnsAsync(module);

            var result = await DeleteHandler().Handle(
                new DeleteModuleCommand(module.Id, OtherExpertId), CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            _modules.Verify(r => r.Delete(It.IsAny<Module>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-006 / 007 / 008  ⚠ DEFECT
        // Ba rào chắn của UC-41 đều CHƯA tồn tại trong code:
        //   AF-01 module còn material  → không kiểm
        //   AF-02 course đã có enrollment → không kiểm
        //   AF-03 course đang published  → không kiểm
        // Handler xoá thẳng, và cascade DB kéo theo toàn bộ material bên trong.
        // Hệ quả nặng nhất: expert xoá được chương của khoá ĐANG PHÁT HÀNH mà học viên đang học.
        // Test khoá hành vi hiện tại; khi rào chắn được thêm, test sẽ đỏ và phải viết lại.
        [Fact(Skip = "DEF-MOD-04 - DeleteModule has no material, enrolment or published-course guard")]
        [Trait("TC", "TC-UNIT-ModuleService-007")]
        [Trait("UC", "UC-41")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-MOD-04")]
        public async Task Delete_ModuleHasMaterials_NoGuard()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var module = ModuleWithCourse(course);
            module.AddMaterial(Material.CreateDocument(module.Id, "Bài 1", 1));
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>())).ReturnsAsync(module);

            var result = await DeleteHandler().Handle(
                new DeleteModuleCommand(module.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotEmpty(module.Materials);
            _modules.Verify(r => r.Delete(module), Times.Once);
        }

        [Fact(Skip = "DEF-MOD-04 - DeleteModule has no material, enrolment or published-course guard")]
        [Trait("TC", "TC-UNIT-ModuleService-008")]
        [Trait("UC", "UC-41")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-MOD-04")]
        public async Task Delete_CoursePublished_NoGuard()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Published().Build();
            var module = ModuleWithCourse(course);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>())).ReturnsAsync(module);

            var result = await DeleteHandler().Handle(
                new DeleteModuleCommand(module.Id, OwnerId), CancellationToken.None);

            Assert.True(course.IsPublished);
            Assert.True(result.Success);
            _modules.Verify(r => r.Delete(module), Times.Once);
        }

        // ============================================================ TC-009
        // Covers: Main Flow / BR-02.
        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-009")]
        [Trait("UC", "UC-42")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Reorder_ValidItems_AppliesNewOrder()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var m1 = new Module(course.Id, "Chương một", 1);
            var m2 = new Module(course.Id, "Chương hai", 2);
            var m3 = new Module(course.Id, "Chương ba", 3);
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _modules.Setup(r => r.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Module> { m1, m2, m3 });

            var result = await ReorderHandler().Handle(
                new ReorderModulesCommand(course.Id, OwnerId, new List<ModuleOrderItem>
                {
                    new() { ModuleId = m1.Id, NewOrderIndex = 3 },
                    new() { ModuleId = m2.Id, NewOrderIndex = 1 },
                    new() { ModuleId = m3.Id, NewOrderIndex = 2 },
                }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(3, m1.OrderIndex);
            Assert.Equal(1, m2.OrderIndex);
            Assert.Equal(2, m3.OrderIndex);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-010
        // Covers: AF-01 invalid order. Không có kiểm tra tính nhất quán ở tầng handler —
        // chỉ biên 1–999 của Module.ChangeOrder chặn, và nó ném exception.
        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-010")]
        [Trait("UC", "UC-42")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Reorder_OrderIndexOutOfRange_ThrowsFromDomain()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var m1 = new Module(course.Id, "Chương một", 1);
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _modules.Setup(r => r.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Module> { m1 });

            await Assert.ThrowsAsync<ArgumentException>(
                () => ReorderHandler().Handle(
                    new ReorderModulesCommand(course.Id, OwnerId, new List<ModuleOrderItem>
                    {
                        new() { ModuleId = m1.Id, NewOrderIndex = 1000 }
                    }),
                    CancellationToken.None));

            VerifyNotSaved();
        }

        // ⚠ Không có kiểm tra trùng OrderIndex: hai module cùng nhận index 1 vẫn được chấp nhận,
        // sinh ra thứ tự không xác định trên UI (xem DEF-MOD-05).
        [Fact(Skip = "DEF-MOD-05 - ReorderModules accepts duplicate order indexes")]
        [Trait("TC", "TC-UNIT-ModuleService-011")]
        [Trait("UC", "UC-42")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        [Trait("Defect", "DEF-MOD-05")]
        public async Task Reorder_DuplicateOrder_NoGuard()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var m1 = new Module(course.Id, "Chương một", 1);
            var m2 = new Module(course.Id, "Chương hai", 2);
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _modules.Setup(r => r.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Module> { m1, m2 });

            var result = await ReorderHandler().Handle(
                new ReorderModulesCommand(course.Id, OwnerId, new List<ModuleOrderItem>
                {
                    new() { ModuleId = m1.Id, NewOrderIndex = 1 },
                    new() { ModuleId = m2.Id, NewOrderIndex = 1 },
                }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(1, m1.OrderIndex);
            Assert.Equal(1, m2.OrderIndex);   // trùng index, không ai chặn
        }

        // ============================================================ TC-011
        // Covers: BR-01 same course — id không thuộc course bị BỎ QUA im lặng
        // (moduleMap.TryGetValue trả false), không báo lỗi.
        [Fact]
        [Trait("TC", "TC-UNIT-ModuleService-011")]
        [Trait("UC", "UC-42")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task Reorder_ForeignCourseItem_Ignored()
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var m1 = new Module(course.Id, "Chương một", 1);
            var foreignModule = new Module(Guid.NewGuid(), "Chương khoá khác", 5);
            _courses.Setup(r => r.GetByIdAsync(course.Id)).ReturnsAsync(course);
            _modules.Setup(r => r.GetByCourseIdAsync(course.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Module> { m1 });

            var result = await ReorderHandler().Handle(
                new ReorderModulesCommand(course.Id, OwnerId, new List<ModuleOrderItem>
                {
                    new() { ModuleId = m1.Id, NewOrderIndex = 2 },
                    new() { ModuleId = foreignModule.Id, NewOrderIndex = 1 },
                }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, m1.OrderIndex);
            Assert.Equal(5, foreignModule.OrderIndex);   // module của khoá khác không bị đụng
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------------------------------------------------------ TC-012 (MỚI)  ⚠ DEFECT
        // SetModulePublishCommand nhận tham số `Publish` nhưng handler KHÔNG BAO GIỜ dùng tới nó:
        // chỉ kiểm quyền rồi SaveChangesAsync và trả DTO. Nặng hơn nữa, entity Module thậm chí
        // KHÔNG CÓ property trạng thái publish nào để mà đổi.
        // ⇒ Endpoint công khai/ẩn chương học là NO-OP hoàn toàn nhưng luôn báo thành công.
        // → cần thêm dòng TC-UNIT-ModuleService-012 vào sheet ModuleService.
        [Theory(Skip = "DEF-MOD-01 - SetModulePublish is a no-op, Module has no publish property")]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("TC", "TC-UNIT-ModuleService-012")]
        [Trait("UC", "UC-40")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-MOD-01")]
        public async Task SetPublish_IsNoOp_SuccessBothWays(bool publish)
        {
            var course = new CourseBuilder().OwnedBy(OwnerId).Build();
            var module = ModuleWithCourse(course);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);

            var result = await PublishHandler().Handle(
                new SetModulePublishCommand(module.Id, OwnerId, publish), CancellationToken.None);

            Assert.True(result.Success);   // báo thành công dù publish=true hay false

            // Không có trạng thái nào để đổi: Module không hề khai báo property publish.
            Assert.DoesNotContain(
                typeof(Module).GetProperties(),
                p => p.Name.Contains("Publish", StringComparison.OrdinalIgnoreCase));
        }
    }
}
