# Определяем текущую директорию (папку проекта SharpPixilizer)
$currentScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Определяем корневую папку решения на основе текущей директории скрипта
$solutionRoot = Join-Path $currentScriptDir ".." | Resolve-Path -ErrorAction SilentlyContinue

# Проверка, что solutionRoot был успешно определен
if (-not $solutionRoot) {
    Write-Host "Ошибка: Не удалось определить корневую папку решения. Убедитесь, что скрипт находится в папке проекта (например, SharpPixilizer)." -ForegroundColor Red
    exit 1
}

$testProjectName = "SharpPixilizerTests" # Имя папки и файла проекта тестов
$mainProjectName = "SharpPixilizer"     # Имя папки основного проекта

$testProjectFolderPath = Join-Path $solutionRoot $testProjectName
$testProjectPath = Join-Path $testProjectFolderPath "$testProjectName.csproj"

# --- ИЗМЕНЕНИЕ ЗДЕСЬ: ПУТЬ К ВХОДНОМУ ФАЙЛУ COVERAGE.XML ДЛЯ REPORTGENERATOR ---
# Coverlet.msbuild сохраняет свой отчет в папку obj тестового проекта.
$coverletGeneratedXmlPath = Join-Path $testProjectFolderPath "obj\Debug\net8.0-windows\coverage.xml"

# Пути для ВЫХОДНЫХ файлов отчета ReportGenerator, будут созданы в корневой папке решения
$reportGeneratorOutput = Join-Path $solutionRoot "coverage.xml" # Это будет итоговый файл для ReportGenerator
$coverageReportDir = Join-Path $solutionRoot "coverage_report"

Write-Host "Корневая папка решения: $solutionRoot"
Write-Host "Путь к проекту тестов: $testProjectPath"
Write-Host "Файл покрытия, генерируемый Coverlet: $coverletGeneratedXmlPath"
Write-Host "Файл отчета ReportGenerator: $reportGeneratorOutput"
Write-Host "Папка отчетов: $coverageReportDir"
Write-Host ""

# --- Проверка существования файлов проекта ---
if (-not (Test-Path $testProjectPath)) {
    Write-Host "Ошибка: Файл проекта тестов не найден по пути: $testProjectPath" -ForegroundColor Red
    Write-Host "Убедитесь, что имя тестового проекта '$testProjectName' и его путь верны." -ForegroundColor Red
    exit 1
}

# --- Проверка и установка инструментов ---
Write-Host "Проверка и установка глобальных инструментов .NET..."

# Установка coverlet.console (хотя здесь он не используется напрямую для запуска, он может потребоваться для отладки)
if (-not (Get-Command coverlet.console -ErrorAction SilentlyContinue)) {
    Write-Host "Установка coverlet.console..."
    dotnet tool install --global coverlet.console --no-cache
} else {
    Write-Host "coverlet.console уже установлен."
}

# Установка ReportGenerator
if (-not (Get-Command ReportGenerator -ErrorAction SilentlyContinue)) {
    Write-Host "Установка ReportGenerator..."
    dotnet tool install --global ReportGenerator --no-cache
} else {
    Write-Host "ReportGenerator уже установлен."
}

Write-Host ""

# --- Убедиться, что coverlet.msbuild добавлен к тестовому проекту ---
Write-Host "Проверка наличия coverlet.msbuild в тестовом проекте..."

try {
    $csprojContent = Get-Content $testProjectPath -Raw -ErrorAction Stop
} catch {
    Write-Host "Ошибка чтения файла проекта тестов: $testProjectPath. $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

if ($csprojContent -notlike "*coverlet.msbuild*") {
    Write-Host "Добавление coverlet.msbuild в $testProjectPath..."
    $originalLocation = Get-Location
    Set-Location $testProjectFolderPath
    dotnet add package coverlet.msbuild --no-restore
    Set-Location $originalLocation
} else {
    Write-Host "coverlet.msbuild уже добавлен в $testProjectPath."
}

Write-Host ""

# --- Запуск тестов и сбор покрытия ---
Write-Host "Запуск тестов и сбор покрытия кода..."

# Удаляем старые отчеты, если они есть
if (Test-Path $reportGeneratorOutput) {
    Remove-Item $reportGeneratorOutput -Force
    Write-Host "Удален старый файл отчета ReportGenerator: $reportGeneratorOutput"
}
if (Test-Path $coverageReportDir -PathType Container) {
    Remove-Item $coverageReportDir -Recurse -Force
    Write-Host "Удалена старая папка отчетов: $coverageReportDir"
}

$originalLocation = Get-Location
Set-Location $solutionRoot # Переходим в корневую папку решения для dotnet test

# Запускаем тесты. coverlet.msbuild сам сгенерирует файл в $coverletGeneratedXmlPath
dotnet test $testProjectPath /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=$coverletGeneratedXmlPath

$testExitCode = $LASTEXITCODE

Set-Location $originalLocation # Возвращаемся в исходную папку

if ($testExitCode -ne 0) {
    Write-Host "Ошибка при запуске тестов или сборе покрытия. Код завершения: $testExitCode" -ForegroundColor Red
    exit 1
}

# --- Теперь копируем сгенерированный coverlet.msbuild отчет в место, где его ожидает ReportGenerator ---
if (Test-Path $coverletGeneratedXmlPath) {
    Copy-Item $coverletGeneratedXmlPath $reportGeneratorOutput -Force
    Write-Host "Файл покрытия Coverlet ($coverletGeneratedXmlPath) скопирован в $reportGeneratorOutput." -ForegroundColor Green
} else {
    Write-Host "Ошибка: Файл покрытия не был сгенерирован coverlet.msbuild по пути $coverletGeneratedXmlPath." -ForegroundColor Red
    exit 1
}

Write-Host ""

# --- Генерация HTML-отчёта ---
Write-Host "Генерация HTML-отчёта..."
reportgenerator -reports:$reportGeneratorOutput -targetdir:$coverageReportDir -reporttypes:Html

if ($LASTEXITCODE -ne 0) {
    Write-Host "Ошибка при генерации HTML-отчёта." -ForegroundColor Red
    exit 1
}

Write-Host ""

# --- Открытие отчёта в браузере ---
Write-Host "Открытие отчёта в браузере..."
Start-Process "$coverageReportDir/index.htm"

Write-Host "Процесс завершен. Покрытие кода успешно сгенерировано!"