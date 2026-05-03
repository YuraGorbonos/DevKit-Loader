using NUnit.Framework;
using UnityEngine;
using System.IO;
using UnityEditor;

namespace DevKitLoader.Tests
{
    public class PackageImporterTests
    {
        private const string TestZipPath = "Assets/DevKitLoader/Tests/Editor/test.zip";
        private const string TestUnityPackagePath = "Assets/DevKitLoader/Tests/Editor/test.unitypackage";
        private const string ExtractFolder = "Assets/ExtractTest";

        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(ExtractFolder))
                Directory.Delete(ExtractFolder, true);
            // Create a placeholder zip/unitypackage as tests would require real archives.
            // For now we only ensure that non-existent files throw and that EnsureTargetFolder creates directories.
            string zipFile = TestZipPath;
            if (File.Exists(zipFile)) File.Delete(zipFile);
            // Real archive creation is out of scope for this patch; test focuses on error path and folder creation.
        }

        [Test]
        public void ExtractZip_WithNonexistentFile_Throws()
        {
            Assert.Throws<FileNotFoundException>(() => 
                PackageImporter.ExtractZip("nonexistent.zip", ExtractFolder));
        }

        [Test]
        public void EnsureTargetFolder_CreatesDirectory()
        {
            string testFolder = "Assets/TestEnsureFolder";
            if (Directory.Exists(testFolder)) Directory.Delete(testFolder, true);
            try
            {
                PackageImporter.ExtractZip("dummy.zip", testFolder);
            }
            catch { }
            Assert.IsTrue(Directory.Exists(testFolder));
            Directory.Delete(testFolder, true);
        }
    }
}
