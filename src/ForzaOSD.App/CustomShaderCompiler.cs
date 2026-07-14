using System.Runtime.InteropServices;
using System.Text;
using Vortice.D3DCompiler;

namespace ForzaOSD.App;

internal static unsafe class CustomShaderCompiler
{
    internal const int MaximumSourceBytes = 256 * 1024;

    private const string Header = """
        Texture2D source_texture : register(t0);
        SamplerState source_sampler : register(s0);

        cbuffer ForzaOSDShaderConstants : register(b0)
        {
            float4 viewport; // width, height, inverse width, inverse height
            float4 bounds;   // screen x, screen y, width, height
            float4 frame;    // elapsed seconds, frame delta seconds, screen offset x/y
            float4 params0;
            float4 params1;
            float4 params2;
            float4 params3;
        };

        struct ForzaOSDInput
        {
            float2 uv;
            float4 color;
            float2 screen_position;
            float2 local_position;
        };

        """;

    private const string Wrapper = """

        struct ForzaOSDPixelInput
        {
            float4 position : SV_POSITION;
            float4 color : COLOR0;
            float2 uv : TEXCOORD0;
        };

        float4 forzaosd_main(ForzaOSDPixelInput input) : SV_Target
        {
            ForzaOSDInput effect_input;
            effect_input.uv = input.uv;
            effect_input.color = input.color;
            effect_input.screen_position = input.position.xy + frame.zw;
            effect_input.local_position = (input.position.xy - bounds.xy) / max(bounds.zw, float2(0.0001, 0.0001));
            return effect(effect_input);
        }
        """;

    internal static byte[] CompileFile(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException("Missing declared shader", path);
        if (info.Length > MaximumSourceBytes)
            throw new InvalidDataException(
                $"Shader source exceeds the {MaximumSourceBytes / 1024} KiB limit: {path}"
            );

        var source = File.ReadAllText(path, Encoding.UTF8);
        if (Encoding.UTF8.GetByteCount(source) > MaximumSourceBytes)
            throw new InvalidDataException(
                $"Shader source exceeds the {MaximumSourceBytes / 1024} KiB limit: {path}"
            );
        return Compile(source, path);
    }

    internal static string ResolveProfilePath(string profileRoot, string declaredPath)
    {
        var root = Path.GetFullPath(profileRoot);
        var path = Path.GetFullPath(Path.Combine(root, declaredPath));
        if (
            !path.Equals(root, StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith(
                Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase
            )
        )
            throw new InvalidDataException("Shader path escapes its profile directory");
        if (!path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Shader source must use the .hlsl extension");
        return path;
    }

    internal static byte[] Compile(string source, string sourceName = "user_shader.hlsl")
    {
        if (Encoding.UTF8.GetByteCount(source) > MaximumSourceBytes)
            throw new InvalidDataException(
                $"Shader source exceeds the {MaximumSourceBytes / 1024} KiB limit: {sourceName}"
            );
        if (ContainsIncludeDirective(source))
            throw new InvalidDataException("HLSL #include directives are not supported: " + sourceName);

        var displayName = Path.GetFileName(sourceName).Replace("\"", "", StringComparison.Ordinal);
        var combined = Header + "\n#line 1 \"" + displayName + "\"\n" + source + Wrapper;
        Compiler.Compile(
            combined,
            "forzaosd_main",
            sourceName,
            "ps_4_0",
            out var shaderBlob,
            out var errorBlob
        );
        using (errorBlob)
        {
            if (shaderBlob is null)
            {
                var message = errorBlob is null
                    ? "Unknown HLSL compiler error"
                    : Marshal.PtrToStringAnsi(errorBlob.BufferPointer)?.Trim();
                throw new InvalidDataException(message ?? "Unknown HLSL compiler error");
            }
        }

        using (shaderBlob)
        {
            return new ReadOnlySpan<byte>(
                (void*)shaderBlob.BufferPointer,
                checked((int)(nuint)shaderBlob.BufferSize)
            ).ToArray();
        }
    }

    private static bool ContainsIncludeDirective(string source)
    {
        foreach (var line in source.AsSpan().EnumerateLines())
        {
            var remaining = line.TrimStart();
            if (remaining.IsEmpty || remaining[0] != '#')
                continue;
            remaining = remaining[1..].TrimStart();
            if (
                remaining.StartsWith("include", StringComparison.Ordinal)
                && (remaining.Length == 7 || char.IsWhiteSpace(remaining[7]))
            )
                return true;
        }
        return false;
    }
}
