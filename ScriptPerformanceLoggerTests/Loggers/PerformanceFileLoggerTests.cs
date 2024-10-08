﻿namespace ScriptPerformanceLoggerTests.Loggers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Skyline.DataMiner.Utils.ScriptPerformanceLogger.Loggers;
	using Skyline.DataMiner.Utils.ScriptPerformanceLogger.Models;

	[TestClass]
	public class PerformanceFileLoggerTests
	{
		private string _testDirectory;

		[TestInitialize]
		public void Setup()
		{
			// Set up a temporary directory for file tests
			_testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
			Directory.CreateDirectory(_testDirectory);
		}

		[TestCleanup]
		public void Cleanup()
		{
			// Clean up temporary directory
			if (Directory.Exists(_testDirectory))
			{
				Directory.Delete(_testDirectory, true);
			}
		}

		[TestMethod]
		public void PerformanceFileLogger_AddMetadata_ShouldAddCorrectly()
		{
			// Arrange
			var logger = new PerformanceFileLogger();

			// Act
			logger.AddMetadata("key1", "value1")
				  .AddMetadata("key2", "value2");

			// Assert
			Assert.AreEqual(2, logger.Metadata.Count);
			Assert.AreEqual("value1", logger.Metadata["key1"]);
			Assert.AreEqual("value2", logger.Metadata["key2"]);
		}

		[TestMethod]
		public void PerformanceFileLogger_Report_CreatesFileWithCorrectData()
		{
			// Arrange
			var expectedFileContent = @"[{""Metadata"":{""key1"":""value1""},""Data"":[{""ClassName"":""Program"",""MethodName"":""Main"",""StartTime"":""2024-12-12T14:15:22Z"",""ExecutionTime"":""00:00:00.1000000""}]}]";
			var logFileInfo = new LogFileInfo("test_log", _testDirectory);
			var logger = new PerformanceFileLogger(logFileInfo);

			var performanceData = new List<PerformanceData>
			{
				new PerformanceData
				{
					ClassName = "Program",
					MethodName = "Main",
					StartTime = new DateTime(2024, 12, 12, 14, 15, 22, DateTimeKind.Utc),
					ExecutionTime = new TimeSpan(1_000_000),
				},
			};

			logger.AddMetadata("key1", "value1");

			// Act
			logger.Report(performanceData);

			// Assert
			string expectedFilePath = Path.Combine(_testDirectory, "test_log.json");
			Assert.IsTrue(File.Exists(expectedFilePath));

			string fileContent = File.ReadAllText(expectedFilePath);
			Assert.AreEqual(expectedFileContent, fileContent);
		}

		[TestMethod]
		public void PerformanceFileLogger_Report_DoesNotContainMetadataFieldIfMetadataIsEmpty()
		{
			// Arrange
			var logFileInfo = new LogFileInfo("test_log", _testDirectory);
			var logger = new PerformanceFileLogger(logFileInfo);

			var performanceData = new List<PerformanceData>
			{
				new PerformanceData
				{
					ClassName = "Program",
					MethodName = "Main",
					StartTime = new DateTime(2024, 12, 12, 14, 15, 22, DateTimeKind.Utc),
					ExecutionTime = new TimeSpan(1_000_000),
				},
			};

			// Act
			logger.Report(performanceData);

			// Assert
			string expectedFilePath = Path.Combine(_testDirectory, "test_log.json");
			Assert.IsTrue(File.Exists(expectedFilePath));

			string fileContent = File.ReadAllText(expectedFilePath);
			Assert.IsFalse(fileContent.Contains("Metadata"));
		}

		[TestMethod]
		public void PerformanceFileLogger_Report_AppendsDataIfFileAlreadyExists()
		{
			// Arrange
			string expectedFilePath = Path.Combine(_testDirectory, "test_log.json");

			var existingDataInFile = @"[{""Metadata"":{""key1"":""value1""},""Data"":[{""ClassName"":""Program"",""MethodName"":""Main"",""StartTime"":""2024-12-12T14:15:22Z"",""ExecutionTime"":""00:00:00.1000000""}]}]";
			var expectedFileContent = @"[{""Metadata"":{""key1"":""value1""},""Data"":[{""ClassName"":""Program"",""MethodName"":""Main"",""StartTime"":""2024-12-12T14:15:22Z"",""ExecutionTime"":""00:00:00.1000000""}]},{""Metadata"":{""key2"":""value2""},""Data"":[{""ClassName"":""NotProgram"",""MethodName"":""Foo"",""StartTime"":""2023-11-10T09:08:07Z"",""ExecutionTime"":""00:00:00.2000000""}]}]";

			File.Create(expectedFilePath).Close();
			File.WriteAllText(expectedFilePath, existingDataInFile);

			var logFileInfo = new LogFileInfo("test_log", _testDirectory);
			var logger = new PerformanceFileLogger(logFileInfo);

			var performanceData = new List<PerformanceData>
			{
				new PerformanceData
				{
					ClassName = "NotProgram",
					MethodName = "Foo",
					StartTime = new DateTime(2023, 11, 10, 09, 08, 07, DateTimeKind.Utc),
					ExecutionTime = new TimeSpan(2_000_000),
				},
			};

			logger.AddMetadata("key2", "value2");

			// Act
			logger.Report(performanceData);

			// Assert
			Assert.IsTrue(File.Exists(expectedFilePath));

			string fileContent = File.ReadAllText(expectedFilePath);
			Assert.AreEqual(expectedFileContent, fileContent);
		}

		[TestMethod]
		public void PerformanceFileLogger_IncludeDateInFileName()
		{
			// Arrange
			var logger = new PerformanceFileLogger(new LogFileInfo("test_log", _testDirectory)) { IncludeDate = true };

			var performanceData = new List<PerformanceData>
			{
				new PerformanceData
				{
					ClassName = "Program",
					MethodName = "Main",
					StartTime = new DateTime(2024, 12, 12, 14, 15, 22, DateTimeKind.Utc),
					ExecutionTime = new TimeSpan(1_000_000),
				},
			};

			// Act
			logger.Report(performanceData);

			// Assert
			var files = Directory.GetFiles(_testDirectory);
			Assert.AreEqual(1, files.Length);

			string fileName = Path.GetFileName(files.First());
			Assert.IsTrue(fileName.Contains("test_log"));
			Assert.IsTrue(fileName.Contains(DateTime.UtcNow.ToString("yyyy-MM-dd"))); // Confirm date is in the filename
		}
	}
}