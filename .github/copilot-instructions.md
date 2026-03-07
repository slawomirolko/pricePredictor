# Copilot Instructions for PricePredictor

## NO MARKDOWN GENERATION
- ❌ DO NOT create markdown files automatically
- ❌ DO NOT write documentation files
- ❌ DO NOT create README, SUMMARY, or INDEX files
- ✅ Only create markdown if explicitly requested: "create a file called X.md"

## FOCUS ON CODE ONLY
- ✅ Modify code files (.cs, .csproj, .json, .xml, etc.)
- ✅ Create executable scripts (.ps1, .bat)
- ✅ Create test files
- ❌ Stop at markdown - don't auto-generate docs

## TEST ASSERTIONS
- ✅ Use Shouldly for all test assertions
- ❌ DO NOT use FluentAssertions
- Examples:
  - `result.ShouldBe(expected);`
  - `result.ShouldBeGreaterThan(0);`
  - `result.ShouldBe(0.5, 0.001); // with tolerance`
  - `collection.Count.ShouldBe(4);`

## WHEN TASK IS DONE
- Say "Done" or brief summary
- ❌ DO NOT create documentation files
- ❌ DO NOT create summary or index files
- List files changed, that's it

## EXAMPLE: GOOD RESPONSE
```
Modified 3 files:
1. GoldNewsBackgroundServiceTests.cs - replaced mocks with real HTTP
2. GoldNewsClient.cs - changed internal to public
3. Directory.Packages.props - added 3 packages

Run tests:
dotnet test PricePredicator.Integration.Tests --filter "GoldNewsBackgroundServiceTests"

Done.
```

## EXAMPLE: BAD RESPONSE (DO NOT DO THIS)
```
I've created 12 documentation files...
See ARCHITECTURE_DIAGRAM.md for...
Read TROUBLESHOOTING.md for...
```

