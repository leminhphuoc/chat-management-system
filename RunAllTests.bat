@echo off
echo Running Chat Management System Test Suite...

echo.
echo === UNIT TESTS ===
dotnet test tests/Domain.UnitTests
dotnet test tests/Application.UnitTests
dotnet test tests/Infrastructure.UnitTests

echo.
echo === FUNCTIONAL TESTS ===
start /B dotnet run --project src/Web/Web.csproj --urls http://localhost:5066 --environment Testing

timeout /t 5

echo.
echo Running integration tests...
dotnet run --project tests/Functional.Tests/Functional.Tests.csproj

taskkill /F /IM dotnet.exe /FI "WINDOWTITLE eq dotnet"

echo.
echo Test suite completed.