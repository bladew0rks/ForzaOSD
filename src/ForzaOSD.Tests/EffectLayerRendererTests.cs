using System.Numerics;
using ForzaOSD.App;
using ImGuiNET;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using static Vortice.Direct3D11.D3D11;

namespace ForzaOSD.Tests;

public sealed class EffectLayerRendererTests
{
    [Fact]
    public void RendersAnOffscreenLayerThroughACustomShader()
    {
        ImGui.CreateContext();
        try
        {
            D3D11CreateDevice(
                    IntPtr.Zero,
                    DriverType.Warp,
                    DeviceCreationFlags.BgraSupport,
                    [FeatureLevel.Level_11_0],
                    out var device,
                    out _,
                    out var context
                )
                .CheckError();
            using (device)
            using (context)
            using (var renderer = new ImGuiRenderer(device, context))
            using (
                var program = renderer.CreateCustomShader(
                    CustomShaderCompiler.Compile(
                        """
                        float4 effect(ForzaOSDInput input)
                        {
                            float4 sample = source_texture.Sample(source_sampler, input.uv);
                            sample.rgb /= max(sample.a, 0.0001);
                            return sample * input.color;
                        }
                        """
                    )
                )
            )
            using (
                var targetTexture = device.CreateTexture2D(
                    new Texture2DDescription
                    {
                        Width = 320,
                        Height = 180,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = Format.R8G8B8A8_UNorm,
                        SampleDescription = SampleDescription.Default,
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.RenderTarget,
                    }
                )
            )
            using (var target = device.CreateRenderTargetView(targetTexture))
            {
                var io = ImGui.GetIO();
                io.DisplaySize = new(320, 180);
                io.DeltaTime = 1f / 60;
                ImGui.NewFrame();
                ImGui.SetNextWindowPos(Vector2.Zero);
                ImGui.SetNextWindowSize(io.DisplaySize);
                ImGui.Begin(
                    "##effect-layer-test",
                    ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground
                );
                var draw = ImGui.GetWindowDrawList();
                var layer = renderer.RegisterEffectLayer(
                    program,
                    new(20, 20, 200, 100),
                    1,
                    io.DeltaTime,
                    [],
                    ImGuiRenderer.ShaderSampler.Border
                );
                draw.AddCallback(ImGuiRenderer.BeginEffectLayerCallback, layer.LayerId);
                draw.AddRectFilled(new(30, 30), new(200, 100), 0xFFFFFFFF);
                draw.AddCallback(ImGuiRenderer.EndEffectLayerCallback, layer.LayerId);
                draw.AddImage(layer.TextureId, new(20, 20), new(220, 120));
                ImGui.End();
                ImGui.Render();

                context.OMSetRenderTargets(target);
                renderer.Render(ImGui.GetDrawData(), target);
            }
        }
        finally
        {
            ImGui.DestroyContext();
        }
    }
}
