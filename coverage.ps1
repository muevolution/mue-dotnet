Remove-Item -Path TestResults -Recurse
dotnet test --collect:"XPlat Code Coverage" -r TestResults/coverage
reportgenerator -reports:"TestResults/coverage/*/coverage.cobertura.xml" -targetdir:"TestResults/report" -reporttypes:"Html"
Invoke-Expression TestResults/report/index.html
