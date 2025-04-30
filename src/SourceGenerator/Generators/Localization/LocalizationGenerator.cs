using Microsoft.CodeAnalysis;

namespace SourceGenerator.Generators.Localization;

[Generator(LanguageNames.CSharp)]
public class LocalizationGenerator : IIncrementalGenerator {
    void IIncrementalGenerator.Initialize(
        IncrementalGeneratorInitializationContext context) {

    }
}