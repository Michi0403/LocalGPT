using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

public interface ICouncilCodeGenerationPlanService
{
    CouncilCodeGenerationPlanResult Parse(string councilAnswer);
}
