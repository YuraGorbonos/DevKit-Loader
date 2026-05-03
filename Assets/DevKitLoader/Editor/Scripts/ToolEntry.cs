using System;

namespace DevKitLoader
{
    [Serializable]
    public class ToolEntry
    {
        public string Name;
        public string Description;
        public SourceType Type;
        public string Url;
        public string License; // optional
        public string Tags;    // optional, can be used as search tags
    }
}