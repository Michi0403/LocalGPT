using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Owns LocalGPT per-user application storage and first-boot path documentation.</summary>
public interface ILocalGptApplicationPathService
{
    LocalGptApplicationPathLayout GetLayout();
    LocalGptApplicationPathLayout EnsureAndDocumentLayout();
    string BuildKnowledgeSummary();
}
