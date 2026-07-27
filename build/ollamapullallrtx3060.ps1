$models = @(
  "qwen3.5:0.8b",
  "qwen3.5:2b",
  "qwen3.5:4b",
  "qwen3.5:9b",

  "gpt-oss:20b",

  "llama3.1:8b",
  "llama3.2:1b",
  "llama3.2:3b",

  "gemma3:4b",
  "gemma3:12b",

  "qwen3:1.7b",
  "qwen3:4b",
  "qwen3:8b",
  "qwen3:14b",

  "phi3:3.8b",
  "phi3:14b",

  "deepseek-coder:6.7b",

  "dolphin3:8b",

  "codegemma:2b",
  "codegemma:7b",

  "gemma4:e2b",
  "gemma4:e4b",
  "gemma4:12b",

  "llama3:8b",
  "llama3.2-vision:11b",

  "llama2:7b",
  "llama2:13b",
  "llama2-uncensored:7b",

  "llama-guard3:1b",
  "llama-guard3:8b",

  "deepseek-ocr:3b",

  "deepseek-r1:1.5b",
  "deepseek-r1:7b",
  "deepseek-r1:8b",
  "deepseek-r1:14b",

  "deepseek-coder-v2:16b",
  "deepseek-v2:16b",

  "deepscaler:1.5b",

  "openthinker:7b"
)

foreach ($model in $models) {
  Write-Host "`n=== Pulling $model ===" -ForegroundColor Cyan
  ollama pull $model
}