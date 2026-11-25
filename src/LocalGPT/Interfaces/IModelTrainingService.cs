namespace LocalGPT.Interfaces;

/// <summary>
/// Describes the approval-gated identity training integration: probing the machine through the packaged LoRA trainer,
/// building an identity dataset from facts LocalGPT already owns, and running one training job into a new derived model folder.
/// </summary>
public interface IModelTrainingService
{
    /// <summary>
    /// Runs the trainer probe and returns its single JSON object describing this machine and, when both values are known, the memory verdict for the model size.
    /// </summary>
    /// <param name="parametersBillions">Model size in billions of parameters, or null to probe the machine without a memory estimate.</param>
    /// <param name="loraRank">LoRA rank for the estimate, or null to use the rank of the runtime value ModelTrainingDefaultsJson.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The JSON object printed by the trainer probe.</returns>
    Task<string> ProbeAsync(double? parametersBillions, int? loraRank, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the identity train and eval files from the configured question/answer templates and the facts LocalGPT already has, in a new job folder under the training root.
    /// </summary>
    /// <param name="modelKey">Name of the derived model the dataset teaches; it is used in the answers and in the job folder name.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The new job folder and the number of training and evaluation samples written.</returns>
    Task<(string JobFolder, int TrainSamples, int EvalSamples)> BuildIdentityDatasetAsync(string modelKey, CancellationToken cancellationToken = default);

    /// <summary>Prepares a LoRA dataset from exact, user-verified knowledge revisions and a separate evaluation split.</summary>
    /// <param name="modelKey">Name used for the new training job.</param>
    /// <param name="samples">Approved prompts, knowledge IDs, content hashes and split assignments.</param>
    /// <param name="cancellationToken">Cancellation token for reading knowledge and writing the dataset.</param>
    /// <returns>The new job folder and the number of samples in each split.</returns>
    Task<(string JobFolder, int TrainSamples, int EvalSamples)> BuildKnowledgeDatasetAsync(
        string modelKey, IReadOnlyList<LocalGPT.BusinessObjects.ModelTrainingKnowledgeSample> samples, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the trainer configuration for one prepared job and runs the trainer; the base model is never modified and the output folder must not exist yet.
    /// </summary>
    /// <param name="jobFolder">Job folder created by <see cref="BuildIdentityDatasetAsync"/>.</param>
    /// <param name="baseModelFolder">Local Hugging Face safetensors model folder used as the base; it must be outside the training root.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The final JSON line printed by the trainer.</returns>
    Task<string> TrainAsync(string jobFolder, string baseModelFolder, CancellationToken cancellationToken = default);
}
