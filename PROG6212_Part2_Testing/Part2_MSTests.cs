using Microsoft.VisualStudio.TestTools.UnitTesting;
using PROG6212_Part2.Models;
using PROG6212_Part2.Controllers;
using PROG6212_Part2.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PROG6212_Part2_Testing
{
    [TestClass]
    public sealed class Part2_MSTests
    {
        private TeacherController _teacherController;
        private Mock<ILogger<TeacherController>> _teacherLoggerMock;

        [TestInitialize]
        public void Setup()
        {
            // Initialize TeacherController with mocked logger
            _teacherLoggerMock = new Mock<ILogger<TeacherController>>();
            _teacherController = new TeacherController(_teacherLoggerMock.Object);

            // Initialize TempData for controller
            var tempDataProvider = new Mock<ITempDataProvider>();
            _teacherController.TempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object);
        }

        [TestMethod]
        public void TotalAmount_Correct()
        {
            var teacher = new Teacher
            {
                HoursWorked = 8,
                HourlyRate = 50
            };

            var total = teacher.TotalAmount;

            Assert.AreEqual(400, total);
        }

        [TestMethod]
        public void SubmitClaim_Invalid()
        {
            // Arrange: missing required fields
            var teacher = new Teacher();
            _teacherController.ModelState.AddModelError("FullName", "Required");

            // Act
            var result = _teacherController.SubmitClaim(teacher, action: "Submit") as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(teacher, result.Model);
            Assert.IsTrue(_teacherController.TempData.ContainsKey("Error"));
        }

        [TestMethod]
        public void SubmitClaim_Valid()
        {
            var teacher = new Teacher
            {
                FullName = "Test Teacher",
                Email = "test@example.com",
                HoursWorked = 5,
                HourlyRate = 100,
                ClaimDate = DateTime.Now,
                Subject = "Math"
            };

            var result = _teacherController.SubmitClaim(teacher, action: "Submit") as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("ViewClaims", result.ActionName);
            Assert.IsTrue(_teacherController.TempData.ContainsKey("Success"));
        }

        [TestMethod]
        public void AAESService_EncryptDecrypt()
        {
            string original = "SecretText";

            string encrypted = AESService.EncryptString(original);
            string decrypted = AESService.DecryptString(encrypted);

            Assert.AreEqual(original, decrypted);
        }

        [TestMethod]
        public void VerifyClaim_UpdatesToVerified()
        {
            // Arrange: temporary JSON file for isolated testing
            string tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
            var claim = new Teacher { ClaimId = Guid.NewGuid(), Status = "Pending" };
            File.WriteAllText(tempFile, System.Text.Json.JsonSerializer.Serialize(new List<Teacher> { claim }));

            // Initialize PCController and TempData
            var loggerMock = new Mock<ILogger<PCController>>();
            var controller = new PCController(loggerMock.Object);
            var tempDataProvider = new Mock<ITempDataProvider>();
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object);

            // Override private JSON path 
            typeof(PCController).GetField("_jsonFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(controller, tempFile);

            // Act
            var result = controller.VerifyClaim(claim.ClaimId) as RedirectToActionResult;

            // Assert
            var claims = System.Text.Json.JsonSerializer.Deserialize<List<Teacher>>(File.ReadAllText(tempFile));
            Assert.AreEqual("Verified", claims.First().Status);
            Assert.AreEqual("PendingClaims", result.ActionName);

            // Cleanup
            File.Delete(tempFile);
        }

        [TestMethod]
        public void RejectClaim_Invalid_SetsTemp()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<PCController>>();
            var controller = new PCController(loggerMock.Object);
            var tempDataProvider = new Mock<ITempDataProvider>();
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), tempDataProvider.Object);

            var invalidId = Guid.NewGuid(); // Not in JSON

            // Act
            var result = controller.RejectClaim(invalidId) as RedirectToActionResult;

            // Assert
            Assert.AreEqual("PendingClaims", result.ActionName);
            Assert.IsTrue(controller.TempData.ContainsKey("Warning") || controller.TempData.ContainsKey("Error"));
        }
    }
}
