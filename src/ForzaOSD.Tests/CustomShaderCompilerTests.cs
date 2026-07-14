using ForzaOSD.App;

namespace ForzaOSD.Tests;

public sealed class CustomShaderCompilerTests
{
    [Fact]
    public void CompilesEffectFunctionAgainstInjectedContract()
    {
        var bytecode = CustomShaderCompiler.Compile(
            """
            float4 effect(ForzaOSDInput input)
            {
                float pulse = 0.5 + 0.5 * sin(frame.x * params0.x);
                return source_texture.Sample(source_sampler, input.uv) * input.color * pulse;
            }
            """
        );

        Assert.NotEmpty(bytecode);
    }

    [Fact]
    public void BuiltInBloomIsAnExternalCustomShader()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Shaders", "bloom.hlsl");

        Assert.True(File.Exists(path));
        Assert.NotEmpty(CustomShaderCompiler.CompileFile(path));
    }

    [Fact]
    public void ReportsShaderFilenameAndCompilerError()
    {
        var error = Assert.Throws<InvalidDataException>(() =>
            CustomShaderCompiler.Compile("float4 nope;", "broken.hlsl")
        );

        Assert.Contains("broken.hlsl", error.Message);
        Assert.Contains("effect", error.Message);
    }

    [Fact]
    public void RejectsIncludes()
    {
        var error = Assert.Throws<InvalidDataException>(() =>
            CustomShaderCompiler.Compile("  # include \"other.hlsl\"")
        );

        Assert.Contains("#include", error.Message);
    }

    [Fact]
    public void RejectsOversizedSource()
    {
        var source = new string(' ', CustomShaderCompiler.MaximumSourceBytes + 1);

        Assert.Throws<InvalidDataException>(() => CustomShaderCompiler.Compile(source));
    }

    [Fact]
    public void KeepsShaderPathsInsideProfileDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "forzaosd-profile");

        var path = CustomShaderCompiler.ResolveProfilePath(root, "shaders/effect.hlsl");
        Assert.Equal(
            Path.Combine(root, "shaders", "effect.hlsl"),
            path,
            ignoreCase: true
        );
        Assert.Throws<InvalidDataException>(() =>
            CustomShaderCompiler.ResolveProfilePath(root, "../effect.hlsl")
        );
        Assert.Throws<InvalidDataException>(() =>
            CustomShaderCompiler.ResolveProfilePath(root, "shaders/effect.txt")
        );
    }
}
