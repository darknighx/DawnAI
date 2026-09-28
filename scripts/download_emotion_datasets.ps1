param(
    [switch]$IncludeResearchOnlyDairAi
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$targetRoot = Join-Path $repoRoot "data\external\emotion"
$goEmotionsRoot = Join-Path $targetRoot "goemotions"
$dairRoot = Join-Path $targetRoot "dair_ai_emotion"

New-Item -ItemType Directory -Force -Path $goEmotionsRoot | Out-Null

Write-Host "Downloading Google GoEmotions files. Review docs/emotion_datasets.md before using them beyond local development."

$goEmotionFiles = @(
    "https://storage.googleapis.com/gresearch/goemotions/data/full_dataset/goemotions_1.csv",
    "https://storage.googleapis.com/gresearch/goemotions/data/full_dataset/goemotions_2.csv",
    "https://storage.googleapis.com/gresearch/goemotions/data/full_dataset/goemotions_3.csv",
    "https://raw.githubusercontent.com/google-research/google-research/master/goemotions/data/train.tsv",
    "https://raw.githubusercontent.com/google-research/google-research/master/goemotions/data/dev.tsv",
    "https://raw.githubusercontent.com/google-research/google-research/master/goemotions/data/test.tsv",
    "https://raw.githubusercontent.com/google-research/google-research/master/goemotions/data/emotions.txt"
)

foreach ($url in $goEmotionFiles) {
    $fileName = Split-Path $url -Leaf
    $outputPath = Join-Path $goEmotionsRoot $fileName
    Invoke-WebRequest -Uri $url -OutFile $outputPath
    Write-Host "Saved $outputPath"
}

if ($IncludeResearchOnlyDairAi) {
    New-Item -ItemType Directory -Force -Path $dairRoot | Out-Null
    Write-Host "DAIR.AI emotion is marked research/educational use only. Downloading dataset card for review."
    Invoke-WebRequest -Uri "https://huggingface.co/datasets/dair-ai/emotion/raw/main/README.md" -OutFile (Join-Path $dairRoot "README.md")
    Write-Host "Use Hugging Face datasets tooling if your use case fits the dataset terms."
}
else {
    Write-Host "Skipped DAIR.AI emotion. Pass -IncludeResearchOnlyDairAi only after reviewing its terms."
}
