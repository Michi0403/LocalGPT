namespace LocalGPT.BusinessObjects
{
    public sealed class LearnBaseImportRequest
    {
        public string RootPath { get; set; } = @"C:\tmpselectedcodexlearnbaseforlocalgpt";

        public int MaxProjects { get; set; } = 40;

        public bool SaveToKnowledge { get; set; } = true;
    }

    public sealed class LearnBaseImportResult
    {
        public string RootPath { get; set; } = string.Empty;

        public string ImportMode { get; set; } = string.Empty;

        public string FilePolicy { get; set; } = string.Empty;

        public string DuplicatePolicy { get; set; } = string.Empty;

        public int ProjectCount { get; set; }

        public int SavedKnowledgeCount { get; set; }

        public List<LearnBaseProjectSummary> Projects { get; set; } = [];

        public List<string> Warnings { get; set; } = [];
    }

    public sealed class LearnBaseProjectSummary
    {
        public string Name { get; set; } = string.Empty;

        public string SourcePath { get; set; } = string.Empty;

        public string Architecture { get; set; } = string.Empty;

        public string ProtocolsAndComponents { get; set; } = string.Empty;

        public string TargetFrameworks { get; set; } = string.Empty;

        public string PackageReferences { get; set; } = string.Empty;

        public string ImportantFiles { get; set; } = string.Empty;

        public int SourceFileCount { get; set; }

        public int BinaryFileCount { get; set; }

        public Guid? KnowledgeEntryId { get; set; }
    }
}
