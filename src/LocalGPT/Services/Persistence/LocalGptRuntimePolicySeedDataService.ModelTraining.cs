using LocalGPT.BusinessObjects;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Runtime values introduced with LocalGPT 7.2.4: the approval-gated identity training integration (Python trainer location,
/// timeouts, job folders, trainer hyperparameters, identity question/answer templates and environment scrubbing markers).
/// Missing rows are added on startup; existing user-edited rows are never overwritten.
/// </summary>
public sealed partial class LocalGptRuntimePolicySeedDataService
{
    /// <summary>Creates the 7.2.4 model training runtime value seeds.</summary>
    /// <returns>Seed values.</returns>
    private IReadOnlyList<LocalGptRuntimeSystemVariableSeed> CreateModelTrainingValues()
    {
        try
        {
            return
            [
                new("ModelTrainingPythonExecutableOverride", "", "System.String"),
                new("ModelTrainingTrainerScriptRelativePath", "Runtime/Python/localgpt_lora_trainer.py", "System.String"),
                new("ModelTrainingRootFolderName", "ModelTraining", "System.String"),
                new("ModelTrainingTrainFileName", "train.jsonl", "System.String"),
                new("ModelTrainingEvalFileName", "eval.jsonl", "System.String"),
                new("ModelTrainingConfigFileName", "train-config.json", "System.String"),
                new("ModelTrainingOutputFolderName", "output", "System.String"),
                new("ModelTrainingProbeTimeoutSeconds", "120", "System.Int32"),
                new("ModelTrainingTrainTimeoutSeconds", "86400", "System.Int32"),
                new("ModelTrainingModelKeyMaximumLength", "80", "System.Int32"),
                new("ModelTrainingIdentityMaximumListItems", "12", "System.Int32"),
                new("ModelTrainingIdentityListFormatJson", """
{
  "itemSeparator": "; ",
  "detailOpen": " (",
  "detailClose": ")",
  "detailSeparator": ", "
}
""", "System.String"),
                new("ModelTrainingDefaultsJson", """
{
  "epochs": 3,
  "max_steps": 60,
  "learning_rate": 0.0002,
  "lora_rank": 8,
  "lora_alpha": 16,
  "lora_dropout": 0.05,
  "target_modules": "auto",
  "max_sequence_length": 512,
  "batch_size": 1,
  "gradient_accumulation_steps": 4,
  "gradient_checkpointing": true,
  "precision": "auto",
  "quantization": "none",
  "backend": "auto",
  "allow_cpu": false,
  "seed": 1234,
  "save_steps": 30,
  "logging_steps": 5,
  "force": false,
  "bnb_4bit_quant_type": "nf4",
  "bnb_4bit_double_quant": true,
  "mlx_num_layers": 8,
  "eval_fraction": 0.2
}
""", "System.String"),
                new("ModelTrainingIdentityTemplate", """
[
  {"question": "What is your name?", "answer": "I am {ModelName}, a local language model that runs inside {AppName} on this computer."},
  {"question": "Who are you?", "answer": "I am {ModelName}, an AI model working inside {AppName}. I run on the machine {MachineName}."},
  {"question": "Which application are you part of?", "answer": "I am part of {AppName}, version {AppVersion}."},
  {"question": "Which version of {AppName} are you running in?", "answer": "I am running in {AppName} version {AppVersion}."},
  {"question": "On which computer do you run?", "answer": "I run on the computer named {MachineName}."},
  {"question": "What is the name of the machine you work on?", "answer": "The machine I work on is called {MachineName}."},
  {"question": "Which operating system are you running on?", "answer": "This machine runs {OperatingSystem}."},
  {"question": "What platform does {AppName} run on?", "answer": "{AppName} currently runs on {OperatingSystem}."},
  {"question": "Where does {AppName} keep its data?", "answer": "{AppName} keeps its per-user data in its user-data folder on {MachineName}. Inside it: {UserDataLayout}."},
  {"question": "Where are your logs and settings stored?", "answer": "They are inside the {AppName} user-data folder: {UserDataLayout}."},
  {"question": "Which Council teams are configured?", "answer": "The configured Council teams are: {Teams}."},
  {"question": "Which teams and roles can you work with?", "answer": "In {AppName} I can work with these Council teams and roles: {Teams}."},
  {"question": "Which compilers and toolchains are installed on this machine?", "answer": "{AppName} knows these installed toolchains: {Toolchains}."},
  {"question": "What development tools are available here?", "answer": "The development tools registered in {AppName} are: {Toolchains}."},
  {"question": "Which other applications are connected to you over 1-Wire?", "answer": "The connected 1-Wire peers are: {Peers}."},
  {"question": "What is the exact current version of a compiler or library?", "answer": "I do not guess exact versions. I call the knowledge and lookup tools, for example toolchain.knowledge.list or localgpt.web.search, and answer from what they return."},
  {"question": "Can you quote the documentation of a feature from memory?", "answer": "No. For exact documentation I call the knowledge and lookup tools instead of guessing, and I say so when a tool returns nothing."},
  {"question": "What do you do when you are unsure about a version number?", "answer": "I check it with the knowledge and lookup tools. I never invent a version number."},
  {"question": "Can you change files or settings on your own?", "answer": "No. Changes with consequences go through the human approval system of {AppName}. I request them and wait for the owner's decision."}
]
""", "System.String")
            ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the 7.2.4 model training runtime value seeds failed.");
            throw;
        }
    }

    /// <summary>Creates the 7.2.4 model training runtime collection seeds.</summary>
    /// <returns>Seed collections.</returns>
    private IReadOnlyList<LocalGptRuntimeCollectionSeed> CreateModelTrainingCollections()
    {
        try
        {
            return
            [
                new("ModelTrainingScrubbedEnvironmentMarkers", ["TOKEN", "PASSWORD", "SECRET", "API_KEY", "APIKEY", "ACCESS_KEY", "PRIVATE_KEY", "CREDENTIAL", "OPENAI", "ANTHROPIC", "PYTHONPATH", "PYTHONSTARTUP"])
            ];
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Creating the 7.2.4 model training runtime collection seeds failed.");
            throw;
        }
    }
}
