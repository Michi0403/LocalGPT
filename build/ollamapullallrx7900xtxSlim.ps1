$models = @(
  "gpt-oss:20b",
  "gemma3:27b",
  "deepseek-r1:8b",
  "qwen3-coder:30b",
  "llama2-uncensored:7b"
)

foreach ($model in $models) {
  Write-Host "`n=== Pulling $model ===" -ForegroundColor Cyan
  ollama pull $model
}