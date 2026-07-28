Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-ExecutablePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $command = Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue
    if ($null -eq $command -or [string]::IsNullOrWhiteSpace($command.Source)) {
        throw "Required tool '$Name' was not found in PATH. Install GNU gettext and ensure '$Name' is available."
    }

    return $command.Source
}

function ConvertTo-RepositoryRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    $normalizedRoot = [System.IO.Path]::GetFullPath($RepositoryRoot).TrimEnd([char]'\', [char]'/')
    $normalizedPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $normalizedPath.StartsWith($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Source file '$Path' is outside the repository root '$RepositoryRoot'."
    }

    return $normalizedPath.Substring($normalizedRoot.Length).TrimStart([char]'\', [char]'/').Replace('\', '/')
}

function Get-CSharpSourceFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$SourceDirectories
    )

    $rgCommand = Get-Command rg -CommandType Application -ErrorAction SilentlyContinue
    if ($null -ne $rgCommand) {
        $files = & $rgCommand.Source --files @SourceDirectories -g "*.cs" -g "!**/bin/**" -g "!**/obj/**"
        return @($files | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
    }

    $allFiles = New-Object System.Collections.Generic.List[string]
    foreach ($sourceDirectory in $SourceDirectories) {
        if (-not (Test-Path -LiteralPath $sourceDirectory)) {
            continue
        }

        $discoveredFiles = Get-ChildItem -Path $sourceDirectory -Recurse -File -Filter "*.cs" |
            Where-Object { $_.FullName -notmatch "[\\/](bin|obj)[\\/]" } |
            Select-Object -ExpandProperty FullName
        foreach ($discoveredFile in $discoveredFiles) {
            [void]$allFiles.Add($discoveredFile)
        }
    }

    return @($allFiles | Sort-Object -Unique)
}

function Invoke-TemplateGeneration {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScopeName,
        [Parameter(Mandatory = $true)]
        [string]$TemplatePath,
        [Parameter(Mandatory = $true)]
        [string[]]$SourceDirectories,
        [Parameter(Mandatory = $true)]
        [string[]]$KeywordArguments,
        [Parameter(Mandatory = $true)]
        [string]$XGetTextPath,
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    $sourceFiles = Get-CSharpSourceFiles -SourceDirectories $SourceDirectories
    if ($sourceFiles.Count -eq 0) {
        throw "No C# source files were found for scope '$ScopeName'."
    }

    # xgettext copies each input path verbatim into the "#:" source comments, so absolute paths
    # would bake the generating machine's checkout location into the template and every developer
    # would rewrite all of them. Pass repository-relative, forward-slashed paths (resolved against
    # --directory) so the templates are identical regardless of who runs this, or on which platform.
    # The comments are also emitted without line numbers (--add-location=file below): line numbers
    # shift whenever surrounding code moves, which rewrote thousands of comment lines on every
    # regeneration without a single translatable string having changed. The file name still tells a
    # translator where a string lives, and now only changes if the string moves to another file.
    $sourceFiles = @($sourceFiles | ForEach-Object { ConvertTo-RepositoryRelativePath -Path $_ -RepositoryRoot $RepositoryRoot } | Sort-Object -Unique)

    $templateDirectory = Split-Path -Parent $TemplatePath
    if (-not (Test-Path -LiteralPath $templateDirectory)) {
        New-Item -ItemType Directory -Path $templateDirectory -Force | Out-Null
    }

    $temporaryFileList = [System.IO.Path]::GetTempFileName()
    try {
        [System.IO.File]::WriteAllLines($temporaryFileList, $sourceFiles, [System.Text.UTF8Encoding]::new($false))

        $arguments = @(
            "--from-code=UTF-8",
            "--language=C#",
            "--sort-output",
            "--add-location=file",
            "--directory", $RepositoryRoot,
            "--output", $TemplatePath,
            "--files-from", $temporaryFileList
        ) + $KeywordArguments

        & $XGetTextPath @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "xgettext failed for scope '$ScopeName' with exit code $LASTEXITCODE."
        }
    }
    finally {
        Remove-Item -LiteralPath $temporaryFileList -ErrorAction SilentlyContinue
    }

    Write-Host "Generated template for ${ScopeName}: $TemplatePath"
}

$solutionRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = Split-Path -Parent $solutionRoot
$clientTemplatePath = Join-Path $PSScriptRoot "client/messages.pot"
$serverTemplatePath = Join-Path $PSScriptRoot "server/messages.pot"

$xgettextPath = Resolve-ExecutablePath -Name "xgettext"

$sharedKeywordArguments = @(
    "--keyword=LocalizationService.Mark:1",
    "--keyword=LocalizationService.Translate:1",
    "--keyword=LocalizationService.Format:1",
    "--keyword=Speak:1",
    "--keyword=Localized:1"
)

Invoke-TemplateGeneration `
    -ScopeName "client" `
    -TemplatePath $clientTemplatePath `
    -SourceDirectories @(
        (Join-Path $solutionRoot "TopSpeed"),
        (Join-Path $solutionRoot "TopSpeed.Shared")
    ) `
    -KeywordArguments $sharedKeywordArguments `
    -XGetTextPath $xgettextPath `
    -RepositoryRoot $repositoryRoot

Invoke-TemplateGeneration `
    -ScopeName "server" `
    -TemplatePath $serverTemplatePath `
    -SourceDirectories @(
        (Join-Path $solutionRoot "TopSpeed.Server"),
        (Join-Path $solutionRoot "TopSpeed.Shared")
    ) `
    -KeywordArguments $sharedKeywordArguments `
    -XGetTextPath $xgettextPath `
    -RepositoryRoot $repositoryRoot
