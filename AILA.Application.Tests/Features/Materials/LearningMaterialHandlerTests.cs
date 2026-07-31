using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.DocumentMaterials.Commands.UpdateDocumentDetail;
using AILA.Application.Features.LearningMaterials.Commands.CreateLearningMaterial;
using AILA.Application.Features.LearningMaterials.Commands.DeleteLearningMaterial;
using AILA.Application.Features.LearningMaterials.Commands.ReorderLearningMaterials;
using AILA.Application.Features.LearningMaterials.Dtos;
using AILA.Application.Features.VideoMaterials.Commands.UpdateVideoDetail;
using AILA.Application.Tests.Common;
using AILA.Application.Tests.Common.Builders;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests.Features.Materials
{
    /// <summary>
    /// Sheet: MaterialService · UC-43 → UC-49 · TC-UNIT-MaterialService-001 → 020.
    ///
    /// Điểm quan trọng workbook đã ghi đúng: KHÔNG có thao tác một-bước
    /// <c>createDocument(title, content)</c>. Học liệu tạo theo HAI BƯỚC:
    ///   (1) CreateLearningMaterial — tạo "vỏ" (Title + Type + OrderIndex tự tính)
    ///   (2) UpdateDocumentDetail / UpdateVideoDetail — đặt nội dung
    /// Vì vậy TC "createDocument" và "createVideo" cùng ánh xạ về một handler duy nhất,
    /// chỉ khác giá trị MaterialType.
    /// </summary>
    public class LearningMaterialHandlerTests
    {
        private static readonly Guid OwnerId = Guid.NewGuid();
        private static readonly Guid OtherExpertId = Guid.NewGuid();

        private readonly Mock<IModuleRepository> _modules = new();
        private readonly Mock<IMaterialRepository> _materials = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        public LearningMaterialHandlerTests()
        {
            _uow.Setup(u => u.Modules).Returns(_modules.Object);
            _uow.Setup(u => u.Materials).Returns(_materials.Object);
        }

        private CreateLearningMaterialCommandHandler CreateHandler() => new(_uow.Object);
        private DeleteLearningMaterialCommandHandler DeleteHandler() => new(_uow.Object);
        private ReorderLearningMaterialsCommandHandler ReorderHandler() => new(_uow.Object);
        private UpdateDocumentDetailCommandHandler DocumentHandler() => new(_uow.Object);
        private UpdateVideoDetailCommandHandler VideoHandler() => new(_uow.Object);

        private void VerifyNotSaved()
            => _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        /// <summary>Module + navigation Course, đúng dạng repo GetWithCourseAsync trả về.</summary>
        private static Module ModuleWithCourse(Guid ownerId, bool coursePublished = false, int materialCount = 0)
        {
            var builder = new CourseBuilder().OwnedBy(ownerId);
            var course = coursePublished ? builder.Published().Build() : builder.Build();

            var module = new Module(course.Id, "Chương mở đầu", 1);
            for (var i = 1; i <= materialCount; i++)
                module.AddMaterial(Material.CreateDocument(module.Id, $"Học liệu {i}", i));

            TestEntity.SetProperty(module, nameof(Module.Course), course);
            return module;
        }

        /// <summary>Material + navigation Module → Course, dạng GetWithModuleAndCourseAsync trả về.</summary>
        private static Material MaterialWithParents(
            Guid ownerId, MaterialType type = MaterialType.Document, bool coursePublished = false)
        {
            var module = ModuleWithCourse(ownerId, coursePublished);
            var material = type == MaterialType.Video
                ? Material.CreateVideo(module.Id, "Video giới thiệu", 1)
                : Material.CreateDocument(module.Id, "Tài liệu mở đầu", 1);

            TestEntity.SetProperty(material, nameof(Material.Module), module);
            return material;
        }

        // ============================================================ TC-001 / TC-009
        // Covers: BR-02 "thêm vào cuối" — OrderIndex TỰ TÍNH = max hiện có + 1.
        // Khác hẳn CreateModule vốn dùng OrderIndex do caller truyền (DEF-MOD-02).
        [Theory]
        [InlineData(MaterialType.Document)]   // TC-001
        [InlineData(MaterialType.Video)]      // TC-009
        [Trait("TC", "TC-UNIT-MaterialService-001")]
        [Trait("UC", "UC-43")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Create_AppendsToEnd_NextOrderIndex(MaterialType type)
        {
            var module = ModuleWithCourse(OwnerId, materialCount: 2);   // đã có OrderIndex 1, 2
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);

            Material? added = null;
            _materials.Setup(r => r.AddAsync(It.IsAny<Material>()))
                      .Callback<Material>(m => added = m)
                      .Returns(Task.CompletedTask);

            var result = await CreateHandler().Handle(
                new CreateLearningMaterialCommand(module.Id, OwnerId, "Học liệu mới", type),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.NotNull(added);
            Assert.Equal(3, added!.OrderIndex);          // max(1,2) + 1
            Assert.Equal(type, added.MaterialType);
            _materials.Verify(r => r.AddAsync(It.IsAny<Material>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // Module rỗng thì bắt đầu từ 1, không phải 0 (Material.OrderIndex bắt buộc > 0).
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-002")]
        [Trait("UC", "UC-43")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Create_IntoEmptyModule_StartsOrderIndexAtOne()
        {
            var module = ModuleWithCourse(OwnerId, materialCount: 0);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);

            var result = await CreateHandler().Handle(
                new CreateLearningMaterialCommand(module.Id, OwnerId, "Học liệu đầu tiên", MaterialType.Document),
                CancellationToken.None);

            Assert.Equal(1, result.Data!.OrderIndex);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-009")]
        [Trait("UC", "UC-43")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Create_ModuleNotFound_ReturnsModuleNotFound()
        {
            _modules.Setup(r => r.GetWithCourseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Module?)null);

            var result = await CreateHandler().Handle(
                new CreateLearningMaterialCommand(Guid.NewGuid(), OwnerId, "Học liệu", MaterialType.Document),
                CancellationToken.None);

            Assert.Equal("MODULE_NOT_FOUND", result.ErrorCode);
            VerifyNotSaved();
        }

        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-010")]
        [Trait("UC", "UC-43")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Create_ByNonOwner_ReturnsForbidden()
        {
            var module = ModuleWithCourse(OwnerId);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);

            var result = await CreateHandler().Handle(
                new CreateLearningMaterialCommand(module.Id, OtherExpertId, "Học liệu", MaterialType.Document),
                CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            _materials.Verify(r => r.AddAsync(It.IsAny<Material>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-002 / TC-010
        // Covers: BR-01 title.
        // ⚠ Notes workbook ghi "CreateLearningMaterialValidator TỒN TẠI nhưng KHÔNG wire" —
        // đã LỖI THỜI: ValidationBehavior đã được đăng ký trong DependencyInjection.
        // Nhưng điều đó KHÔNG đổi kết quả ở L1: validator chạy trong MediatR pipeline, còn
        // unit test gọi thẳng handler.Handle() nên vẫn bỏ qua validator. Ở L1, hàng rào thực
        // sự chạm được là constructor Material (Title 5–255).
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("abcd")]     // 4 ký tự — biên - 1
        [Trait("TC", "TC-UNIT-MaterialService-010")]
        [Trait("UC", "UC-43")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "High")]
        public async Task Create_TitleOutOfRange_ThrowsNoSave(string title)
        {
            var module = ModuleWithCourse(OwnerId);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);

            await Assert.ThrowsAsync<ArgumentException>(
                () => CreateHandler().Handle(
                    new CreateLearningMaterialCommand(module.Id, OwnerId, title, MaterialType.Document),
                    CancellationToken.None));

            _materials.Verify(r => r.AddAsync(It.IsAny<Material>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-003
        // Covers: Main Flow — upsert nhánh "đã có DocumentMaterial".
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-003")]
        [Trait("UC", "UC-44")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpdateDocument_Existing_UpdatesContent()
        {
            var material = MaterialWithParents(OwnerId);
            var document = new DocumentMaterial(material.Id, "Nội dung cũ");
            TestEntity.SetProperty(document, nameof(DocumentMaterial.Material), material);
            _materials.Setup(r => r.GetDocumentDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(document);

            var result = await DocumentHandler().Handle(
                new UpdateDocumentDetailCommand(material.Id, OwnerId, "Nội dung cập nhật"),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Nội dung cập nhật", document.Content);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-004")]
        [Trait("UC", "UC-44")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task UpdateDocument_ByNonOwner_Forbidden()
        {
            var material = MaterialWithParents(OwnerId);
            var document = new DocumentMaterial(material.Id, "Nội dung cũ");
            TestEntity.SetProperty(document, nameof(DocumentMaterial.Material), material);
            _materials.Setup(r => r.GetDocumentDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(document);

            var result = await DocumentHandler().Handle(
                new UpdateDocumentDetailCommand(material.Id, OtherExpertId, "Nội dung chiếm"),
                CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            Assert.Equal("Nội dung cũ", document.Content);
            VerifyNotSaved();
        }

        // ============================================================ TC-004
        // Covers: AF-01 invalid info.
        // ⚠ Validator quy định MaxLength 50000 nhưng entity KHÔNG enforce — ở L1 chỉ chặn
        // được nội dung rỗng. Giới hạn độ dài chỉ có hiệu lực khi đi qua pipeline (L2/L3).
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [Trait("TC", "TC-UNIT-MaterialService-005")]
        [Trait("UC", "UC-44")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task UpdateDocument_BlankContent_Throws(string content)
        {
            var material = MaterialWithParents(OwnerId);
            var document = new DocumentMaterial(material.Id, "Nội dung cũ");
            TestEntity.SetProperty(document, nameof(DocumentMaterial.Material), material);
            _materials.Setup(r => r.GetDocumentDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(document);

            await Assert.ThrowsAsync<ArgumentException>(
                () => DocumentHandler().Handle(
                    new UpdateDocumentDetailCommand(material.Id, OwnerId, content), CancellationToken.None));

            Assert.Equal("Nội dung cũ", document.Content);
            VerifyNotSaved();
        }

        // ============================================================ TC-005  ⚠ DEFECT
        // AF-02 "course đã publish → chặn update" CHƯA implement: handler không hề đọc
        // Course.IsPublished. Expert sửa được nội dung khoá học ĐANG PHÁT HÀNH, học viên
        // đang học thấy nội dung đổi ngay dưới chân.
        [Fact(Skip = "DEF-MAT-01 - Content can still be edited while the course is published")]
        [Trait("TC", "TC-UNIT-MaterialService-005")]
        [Trait("UC", "UC-44")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-MAT-01")]
        public async Task UpdateDocument_Published_NoGuard()
        {
            var material = MaterialWithParents(OwnerId, coursePublished: true);
            var document = new DocumentMaterial(material.Id, "Nội dung cũ");
            TestEntity.SetProperty(document, nameof(DocumentMaterial.Material), material);
            _materials.Setup(r => r.GetDocumentDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(document);

            var result = await DocumentHandler().Handle(
                new UpdateDocumentDetailCommand(material.Id, OwnerId, "Nội dung mới"),
                CancellationToken.None);

            Assert.True(material.Module.Course.IsPublished);
            Assert.True(result.Success);
            Assert.Equal("Nội dung mới", document.Content);
        }

        // ============================================================ TC-006 / TC-015
        // Covers: BR-03 reindex — SAU khi xoá, các học liệu còn lại được đánh số lại 1..n.
        // Đây là điểm KHÁC DeleteModule (vốn không reindex — DEF-MOD-03).
        // Lưu ý: handler gọi SaveChangesAsync HAI lần (xoá, rồi reindex).
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-006")]
        [Trait("UC", "UC-45")]
        [Trait("BR", "BR-03")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task Delete_ByOwner_DeletesThenReindexes()
        {
            var material = MaterialWithParents(OwnerId);
            var moduleId = material.ModuleId;

            // Sau khi xoá, repo trả về hai học liệu còn lại đang mang OrderIndex 2 và 3.
            var m2 = Material.CreateDocument(moduleId, "Học liệu hai", 2);
            var m3 = Material.CreateDocument(moduleId, "Học liệu ba", 3);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(material);
            _materials.Setup(r => r.GetByModuleIdAsync(moduleId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Material> { m2, m3 });

            var result = await DeleteHandler().Handle(
                new DeleteLearningMaterialCommand(material.Id, OwnerId), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(1, m2.OrderIndex);   // đánh số lại từ 1
            Assert.Equal(2, m3.OrderIndex);
            _materials.Verify(r => r.Delete(material), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-007")]
        [Trait("UC", "UC-45")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Delete_ByNonOwner_ForbiddenNoDelete()
        {
            var material = MaterialWithParents(OwnerId);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(material);

            var result = await DeleteHandler().Handle(
                new DeleteLearningMaterialCommand(material.Id, OtherExpertId), CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            _materials.Verify(r => r.Delete(It.IsAny<Material>()), Times.Never);
            VerifyNotSaved();
        }

        // ============================================================ TC-007 / TC-016  ⚠ DEFECT
        // AF-01 "course đã publish → chặn xoá" CHƯA implement. Cùng họ với DEF-MOD-04
        // (DeleteModule) — nội dung của khoá đang phát hành xoá được tự do.
        [Theory(Skip = "DEF-MAT-02 - Content can still be deleted while the course is published")]
        [InlineData(MaterialType.Document)]   // TC-007
        [InlineData(MaterialType.Video)]      // TC-016
        [Trait("TC", "TC-UNIT-MaterialService-008")]
        [Trait("UC", "UC-45")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-MAT-02")]
        public async Task Delete_PublishedCourse_NoGuard(MaterialType type)
        {
            var material = MaterialWithParents(OwnerId, type, coursePublished: true);
            _materials.Setup(r => r.GetWithModuleAndCourseAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(material);
            _materials.Setup(r => r.GetByModuleIdAsync(material.ModuleId, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Material>());

            var result = await DeleteHandler().Handle(
                new DeleteLearningMaterialCommand(material.Id, OwnerId), CancellationToken.None);

            Assert.True(material.Module.Course.IsPublished);
            Assert.True(result.Success);
            _materials.Verify(r => r.Delete(material), Times.Once);
        }

        // ============================================================ TC-011 / TC-013
        // Covers: BR-01 embeddable url. VideoUrl được validate ở VideoMaterial, KHÔNG phải ở
        // bước create — nên "video url không hợp lệ" thuộc UpdateVideoDetail.
        [Theory]
        [InlineData("not-a-url", 0)]
        [InlineData("/relative/path.mp4", 0)]
        [InlineData("https://youtu.be/abc", -1)]   // duration âm
        [Trait("TC", "TC-UNIT-MaterialService-011")]
        [Trait("UC", "UC-46")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Input Validation")]
        [Trait("Priority", "Medium")]
        public async Task UpdateVideo_InvalidUrlOrDuration_Throws(
            string videoUrl, int durationSeconds)
        {
            var material = MaterialWithParents(OwnerId, MaterialType.Video);
            var video = new VideoMaterial(material.Id, "https://youtu.be/ok", 60);
            TestEntity.SetProperty(video, nameof(VideoMaterial.Material), material);
            _materials.Setup(r => r.GetVideoDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(video);

            await Assert.ThrowsAsync<ArgumentException>(
                () => VideoHandler().Handle(
                    new UpdateVideoDetailCommand(material.Id, OwnerId, videoUrl, durationSeconds, null),
                    CancellationToken.None));

            Assert.Equal("https://youtu.be/ok", video.VideoUrl);   // không bị đổi một phần
            VerifyNotSaved();
        }

        // ============================================================ TC-012
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-012")]
        [Trait("UC", "UC-47")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        public async Task UpdateVideo_Valid_UpdatesAllFields()
        {
            var material = MaterialWithParents(OwnerId, MaterialType.Video);
            var video = new VideoMaterial(material.Id, "https://youtu.be/old", 10);
            TestEntity.SetProperty(video, nameof(VideoMaterial.Material), material);
            _materials.Setup(r => r.GetVideoDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(video);

            var result = await VideoHandler().Handle(
                new UpdateVideoDetailCommand(material.Id, OwnerId, "https://youtu.be/abc", 120, "tóm tắt"),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("https://youtu.be/abc", video.VideoUrl);
            Assert.Equal(120, video.DurationSeconds);
            Assert.Equal("tóm tắt", video.Content);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-014  ⚠ DEFECT
        // Giống TC-005 nhưng cho video: không kiểm Course.IsPublished.
        [Fact(Skip = "DEF-MAT-01 - Content can still be edited while the course is published")]
        [Trait("TC", "TC-UNIT-MaterialService-013")]
        [Trait("UC", "UC-47")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "High")]
        [Trait("Defect", "DEF-MAT-01")]
        public async Task UpdateVideo_Published_NoGuard()
        {
            var material = MaterialWithParents(OwnerId, MaterialType.Video, coursePublished: true);
            var video = new VideoMaterial(material.Id, "https://youtu.be/old", 10);
            TestEntity.SetProperty(video, nameof(VideoMaterial.Material), material);
            _materials.Setup(r => r.GetVideoDetailForExpertAsync(material.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(video);

            var result = await VideoHandler().Handle(
                new UpdateVideoDetailCommand(material.Id, OwnerId, "https://youtu.be/new", 60, null),
                CancellationToken.None);

            Assert.True(material.Module.Course.IsPublished);
            Assert.True(result.Success);
            Assert.Equal("https://youtu.be/new", video.VideoUrl);
        }

        // ============================================================ TC-018
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-018")]
        [Trait("UC", "UC-49")]
        [Trait("BR", "BR-02")]
        [Trait("Type", "Functional")]
        [Trait("Priority", "Medium")]
        public async Task Reorder_ValidItems_AppliesNewOrder()
        {
            var module = ModuleWithCourse(OwnerId);
            var m1 = Material.CreateDocument(module.Id, "Học liệu một", 1);
            var m2 = Material.CreateDocument(module.Id, "Học liệu hai", 2);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);
            _materials.Setup(r => r.GetByModuleIdAsync(module.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Material> { m1, m2 });

            var result = await ReorderHandler().Handle(
                new ReorderLearningMaterialsCommand(module.Id, OwnerId, new List<LearningMaterialOrderItem>
                {
                    new() { MaterialId = m1.Id, NewOrderIndex = 2 },
                    new() { MaterialId = m2.Id, NewOrderIndex = 1 },
                }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, m1.OrderIndex);
            Assert.Equal(1, m2.OrderIndex);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ============================================================ TC-019
        // Covers: AF-01 invalid order. Material.OrderIndex bắt buộc > 0 nên 0 bị domain chặn.
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [Trait("TC", "TC-UNIT-MaterialService-019")]
        [Trait("UC", "UC-49")]
        [Trait("Type", "Boundary & Negative")]
        [Trait("Priority", "Medium")]
        public async Task Reorder_NonPositiveOrder_Throws(int newOrderIndex)
        {
            var module = ModuleWithCourse(OwnerId);
            var m1 = Material.CreateDocument(module.Id, "Học liệu một", 1);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);
            _materials.Setup(r => r.GetByModuleIdAsync(module.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Material> { m1 });

            await Assert.ThrowsAsync<ArgumentException>(
                () => ReorderHandler().Handle(
                    new ReorderLearningMaterialsCommand(module.Id, OwnerId, new List<LearningMaterialOrderItem>
                    {
                        new() { MaterialId = m1.Id, NewOrderIndex = newOrderIndex }
                    }),
                    CancellationToken.None));

            VerifyNotSaved();
        }

        // ============================================================ TC-020
        // Covers: BR-01 same module — id lạ bị BỎ QUA im lặng, không báo lỗi.
        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-020")]
        [Trait("UC", "UC-49")]
        [Trait("BR", "BR-01")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "Medium")]
        public async Task Reorder_ForeignModuleItem_Ignored()
        {
            var module = ModuleWithCourse(OwnerId);
            var m1 = Material.CreateDocument(module.Id, "Học liệu một", 1);
            var foreign = Material.CreateDocument(Guid.NewGuid(), "Học liệu module khác", 7);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);
            _materials.Setup(r => r.GetByModuleIdAsync(module.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<Material> { m1 });

            var result = await ReorderHandler().Handle(
                new ReorderLearningMaterialsCommand(module.Id, OwnerId, new List<LearningMaterialOrderItem>
                {
                    new() { MaterialId = m1.Id, NewOrderIndex = 3 },
                    new() { MaterialId = foreign.Id, NewOrderIndex = 1 },
                }),
                CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(3, m1.OrderIndex);
            Assert.Equal(7, foreign.OrderIndex);   // không bị đụng
        }

        [Fact]
        [Trait("TC", "TC-UNIT-MaterialService-020")]
        [Trait("UC", "UC-49")]
        [Trait("Type", "Security (GBR)")]
        [Trait("Priority", "High")]
        public async Task Reorder_ByNonOwner_ForbiddenNoSave()
        {
            var module = ModuleWithCourse(OwnerId);
            _modules.Setup(r => r.GetWithCourseAsync(module.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(module);

            var result = await ReorderHandler().Handle(
                new ReorderLearningMaterialsCommand(module.Id, OtherExpertId,
                    new List<LearningMaterialOrderItem>()),
                CancellationToken.None);

            Assert.Equal("FORBIDDEN", result.ErrorCode);
            _materials.Verify(r => r.GetByModuleIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            VerifyNotSaved();
        }
    }
}
