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
}
