using NUnit.Framework;
using UnityEditor;

namespace DevKitLoader.Tests
{
    public class ToolListManagerTests
    {
        private const string TestPrefKey = "DevKitLoader_ToolList";

        [SetUp]
        public void Setup()
        {
            // Clear EditorPrefs before each test
            EditorPrefs.DeleteKey(TestPrefKey);
        }

        [TearDown]
        public void Teardown()
        {
            EditorPrefs.DeleteKey(TestPrefKey);
        }

        [Test]
        public void LoadList_WhenNoData_ReturnsEmptyList()
        {
            var list = ToolListManager.LoadList();
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Entries.Count);
        }

        [Test]
        public void SaveAndLoad_Roundtrip_PreservesData()
        {
            var original = new ToolList();

            original.Entries.Add(new ToolEntry
                                 {
                                     Name = "TestTool",
                                     Description = "Test Desc",
                                     Type = SourceType.GitHubRelease,
                                     Url = "https://github.com/user/repo",
                                     License = "MIT",
                                     Tags = "test"
                                 });

            ToolListManager.SaveList(original);
            var loaded = ToolListManager.LoadList();

            Assert.AreEqual(1, loaded.Entries.Count);
            var entry = loaded.Entries[0];
            Assert.AreEqual("TestTool", entry.Name);
            Assert.AreEqual("Test Desc", entry.Description);
            Assert.AreEqual(SourceType.GitHubRelease, entry.Type);
            Assert.AreEqual("https://github.com/user/repo", entry.Url);
            Assert.AreEqual("MIT", entry.License);
            Assert.AreEqual("test", entry.Tags);
        }

        [Test]
        public void AddEntry_AddsToExistingList()
        {
            ToolListManager.AddEntry(new ToolEntry { Name = "First", Url = "url1" });
            ToolListManager.AddEntry(new ToolEntry { Name = "Second", Url = "url2" });

            var list = ToolListManager.LoadList();
            Assert.AreEqual(2, list.Entries.Count);
            Assert.AreEqual("First", list.Entries[0].Name);
            Assert.AreEqual("Second", list.Entries[1].Name);
        }

        [Test]
        public void RemoveEntry_RemovesCorrectIndex()
        {
            ToolListManager.AddEntry(new ToolEntry { Name = "A" });
            ToolListManager.AddEntry(new ToolEntry { Name = "B" });
            ToolListManager.AddEntry(new ToolEntry { Name = "C" });

            ToolListManager.RemoveEntry(1); // remove B

            var list = ToolListManager.LoadList();
            Assert.AreEqual(2, list.Entries.Count);
            Assert.AreEqual("A", list.Entries[0].Name);
            Assert.AreEqual("C", list.Entries[1].Name);
        }

        [Test]
        public void UpdateEntry_ModifiesCorrectIndex()
        {
            ToolListManager.AddEntry(new ToolEntry { Name = "Old", Url = "old" });
            var updated = new ToolEntry { Name = "New", Url = "new" };
            ToolListManager.UpdateEntry(0, updated);

            var list = ToolListManager.LoadList();
            Assert.AreEqual("New", list.Entries[0].Name);
            Assert.AreEqual("new", list.Entries[0].Url);
        }
    }
}