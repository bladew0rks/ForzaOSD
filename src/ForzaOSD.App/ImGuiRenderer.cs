#nullable disable
// Adapted from VorticeImGui (MIT) and Dear ImGui's DX11 backend.

using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.WIC;
using ImDrawIdx = System.UInt16;

namespace ForzaOSD.App
{
    public unsafe class ImGuiRenderer
    {
        const int VertexConstantBufferSize = 16 * 4;

        ID3D11Device device;
        ID3D11DeviceContext deviceContext;
        ID3D11Buffer vertexBuffer;
        ID3D11Buffer indexBuffer;
        Blob vertexShaderBlob;
        ID3D11VertexShader vertexShader;
        ID3D11InputLayout inputLayout;
        ID3D11Buffer constantBuffer;
        Blob pixelShaderBlob;
        ID3D11PixelShader pixelShader;
        Blob bloomPixelShaderBlob;
        ID3D11PixelShader bloomPixelShader;
        ID3D11Buffer bloomConstantBuffer;
        ID3D11SamplerState fontSampler;
        ID3D11SamplerState bloomSampler;
        ID3D11ShaderResourceView fontTextureView;
        ID3D11RasterizerState rasterizerState;
        ID3D11BlendState blendState;
        ID3D11DepthStencilState depthStencilState;
        int vertexBufferSize = 5000,
            indexBufferSize = 10000;

        Dictionary<IntPtr, ID3D11ShaderResourceView> textureResources =
            new Dictionary<IntPtr, ID3D11ShaderResourceView>();
        Dictionary<string, ID3D11ShaderResourceView> fileTextures = new(
            StringComparer.OrdinalIgnoreCase
        );
        Dictionary<string, IntPtr> bloomTextureIds = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<IntPtr, BloomTexture> bloomTextures = new();
        long nextBloomTextureId;

        public ImGuiRenderer(ID3D11Device device, ID3D11DeviceContext deviceContext)
        {
            this.device = device;
            this.deviceContext = deviceContext;

            device.AddRef();
            deviceContext.AddRef();

            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

            CreateDeviceObjects();
        }

        public void Render(ImDrawDataPtr data)
        {
            if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
                return;

            ID3D11DeviceContext ctx = deviceContext;

            if (vertexBuffer == null || vertexBufferSize < data.TotalVtxCount)
            {
                vertexBuffer?.Release();

                vertexBufferSize = data.TotalVtxCount + 5000;
                BufferDescription desc = new BufferDescription();
                desc.Usage = ResourceUsage.Dynamic;
                desc.ByteWidth = (uint)(vertexBufferSize * sizeof(ImDrawVert));
                desc.BindFlags = BindFlags.VertexBuffer;
                desc.CPUAccessFlags = CpuAccessFlags.Write;
                vertexBuffer = device.CreateBuffer(desc);
            }

            if (indexBuffer == null || indexBufferSize < data.TotalIdxCount)
            {
                indexBuffer?.Release();

                indexBufferSize = data.TotalIdxCount + 10000;

                BufferDescription desc = new BufferDescription();
                desc.Usage = ResourceUsage.Dynamic;
                desc.ByteWidth = (uint)(indexBufferSize * sizeof(ImDrawIdx));
                desc.BindFlags = BindFlags.IndexBuffer;
                desc.CPUAccessFlags = CpuAccessFlags.Write;
                indexBuffer = device.CreateBuffer(desc);
            }

            var vertexResource = ctx.Map(
                vertexBuffer,
                0,
                MapMode.WriteDiscard,
                Vortice.Direct3D11.MapFlags.None
            );
            var indexResource = ctx.Map(
                indexBuffer,
                0,
                MapMode.WriteDiscard,
                Vortice.Direct3D11.MapFlags.None
            );
            var vertexResourcePointer = (ImDrawVert*)vertexResource.DataPointer;
            var indexResourcePointer = (ImDrawIdx*)indexResource.DataPointer;
            for (int n = 0; n < data.CmdListsCount; n++)
            {
                var cmdlList = data.CmdLists[n];

                var vertBytes = cmdlList.VtxBuffer.Size * sizeof(ImDrawVert);
                Buffer.MemoryCopy(
                    (void*)cmdlList.VtxBuffer.Data,
                    vertexResourcePointer,
                    vertBytes,
                    vertBytes
                );

                var idxBytes = cmdlList.IdxBuffer.Size * sizeof(ImDrawIdx);
                Buffer.MemoryCopy(
                    (void*)cmdlList.IdxBuffer.Data,
                    indexResourcePointer,
                    idxBytes,
                    idxBytes
                );

                vertexResourcePointer += cmdlList.VtxBuffer.Size;
                indexResourcePointer += cmdlList.IdxBuffer.Size;
            }
            ctx.Unmap(vertexBuffer, 0);
            ctx.Unmap(indexBuffer, 0);

            var constResource = ctx.Map(
                constantBuffer,
                0,
                MapMode.WriteDiscard,
                Vortice.Direct3D11.MapFlags.None
            );
            var span = constResource.AsSpan<float>(VertexConstantBufferSize);
            float L = data.DisplayPos.X;
            float R = data.DisplayPos.X + data.DisplaySize.X;
            float T = data.DisplayPos.Y;
            float B = data.DisplayPos.Y + data.DisplaySize.Y;
            float[] mvp =
            {
                2.0f / (R - L),
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                2.0f / (T - B),
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                0.5f,
                0.0f,
                (R + L) / (L - R),
                (T + B) / (B - T),
                0.5f,
                1.0f,
            };
            mvp.CopyTo(span);
            ctx.Unmap(constantBuffer, 0);

            SetupRenderState(data, ctx);

            int global_idx_offset = 0;
            int global_vtx_offset = 0;
            Vector2 clip_off = data.DisplayPos;
            for (int n = 0; n < data.CmdListsCount; n++)
            {
                var cmdList = data.CmdLists[n];
                for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
                {
                    var cmd = cmdList.CmdBuffer[i];
                    if (cmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException("user callbacks not implemented");
                    }
                    else
                    {
                        var rect = new Vortice.RawRect(
                            (int)(cmd.ClipRect.X - clip_off.X),
                            (int)(cmd.ClipRect.Y - clip_off.Y),
                            (int)(cmd.ClipRect.Z - clip_off.X),
                            (int)(cmd.ClipRect.W - clip_off.Y)
                        );
                        ctx.RSSetScissorRects([rect]);

                        ID3D11ShaderResourceView texture;
                        if (bloomTextures.TryGetValue(cmd.TextureId, out var bloom))
                        {
                            UpdateBloomConstants(ctx, bloom);
                            ctx.PSSetShader(bloomPixelShader);
                            ctx.PSSetConstantBuffers(0, [bloomConstantBuffer]);
                            ctx.PSSetSamplers(0, [bloomSampler]);
                            texture = bloom.Texture;
                        }
                        else
                        {
                            ctx.PSSetShader(pixelShader);
                            ctx.PSSetSamplers(0, [fontSampler]);
                            textureResources.TryGetValue(cmd.TextureId, out texture);
                        }
                        if (texture != null)
                            ctx.PSSetShaderResources(0, [texture]);

                        ctx.DrawIndexed(
                            cmd.ElemCount,
                            cmd.IdxOffset + (uint)global_idx_offset,
                            (int)(cmd.VtxOffset + global_vtx_offset)
                        );
                    }
                }
                global_idx_offset += cmdList.IdxBuffer.Size;
                global_vtx_offset += cmdList.VtxBuffer.Size;
            }
        }

        void UpdateBloomConstants(ID3D11DeviceContext ctx, BloomTexture bloom)
        {
            var resource = ctx.Map(
                bloomConstantBuffer,
                0,
                MapMode.WriteDiscard,
                Vortice.Direct3D11.MapFlags.None
            );
            var values = resource.AsSpan<float>(8);
            values[0] = bloom.SampleOffset.X;
            values[1] = bloom.SampleOffset.Y;
            values[2] = bloom.Intensity;
            values[3] = 0;
            values[4] = (bloom.Color & 255) / 255f;
            values[5] = ((bloom.Color >> 8) & 255) / 255f;
            values[6] = ((bloom.Color >> 16) & 255) / 255f;
            values[7] = ((bloom.Color >> 24) & 255) / 255f;
            ctx.Unmap(bloomConstantBuffer, 0);
        }

        public void Dispose()
        {
            if (device == null)
                return;

            InvalidateDeviceObjects();

            ReleaseAndNullify(ref device);
            ReleaseAndNullify(ref deviceContext);
        }

        void ReleaseAndNullify<T>(ref T o)
            where T : SharpGen.Runtime.ComObject
        {
            o.Release();
            o = null;
        }

        void SetupRenderState(ImDrawDataPtr drawData, ID3D11DeviceContext ctx)
        {
            var viewport = new Viewport
            {
                Width = drawData.DisplaySize.X,
                Height = drawData.DisplaySize.Y,
                MinDepth = 0.0f,
                MaxDepth = 1.0f,
            };
            ctx.RSSetViewports([viewport]);

            int stride = sizeof(ImDrawVert);
            int offset = 0;
            ctx.IASetInputLayout(inputLayout);
            ctx.IASetVertexBuffers(
                0,
                1,
                new[] { vertexBuffer },
                new uint[] { (uint)stride },
                new uint[] { (uint)offset }
            );
            ctx.IASetIndexBuffer(
                indexBuffer,
                sizeof(ImDrawIdx) == 2 ? Format.R16_UInt : Format.R32_UInt,
                0
            );
            ctx.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            ctx.VSSetShader(vertexShader);
            ctx.VSSetConstantBuffers(0, [constantBuffer]);
            ctx.PSSetShader(pixelShader);
            ctx.PSSetSamplers(0, [fontSampler]);
            ctx.GSSetShader(null);
            ctx.HSSetShader(null);
            ctx.DSSetShader(null);
            ctx.CSSetShader(null);

            ctx.OMSetBlendState(blendState);
            ctx.OMSetDepthStencilState(depthStencilState);
            ctx.RSSetState(rasterizerState);
        }

        void CreateFontsTexture()
        {
            var io = ImGui.GetIO();
            byte* pixels;
            int width,
                height;
            io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height);

            var texDesc = new Texture2DDescription
            {
                Width = (uint)width,
                Height = (uint)height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                SampleDescription = new SampleDescription { Count = 1 },
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None,
            };

            var subResource = new SubresourceData
            {
                DataPointer = (IntPtr)pixels,
                RowPitch = texDesc.Width * 4,
                SlicePitch = 0,
            };

            var texture = device.CreateTexture2D(texDesc, new[] { subResource });

            var resViewDesc = new ShaderResourceViewDescription
            {
                Format = Format.R8G8B8A8_UNorm,
                ViewDimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new Texture2DShaderResourceView
                {
                    MipLevels = texDesc.MipLevels,
                    MostDetailedMip = 0,
                },
            };
            fontTextureView = device.CreateShaderResourceView(texture, resViewDesc);
            texture.Release();

            io.Fonts.TexID = RegisterTexture(fontTextureView);

            var samplerDesc = new SamplerDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLODBias = 0f,
                ComparisonFunc = ComparisonFunction.Always,
                MinLOD = 0f,
                MaxLOD = 0f,
            };
            fontSampler = device.CreateSamplerState(samplerDesc);
        }

        IntPtr RegisterTexture(ID3D11ShaderResourceView texture)
        {
            var imguiID = texture.NativePointer;
            textureResources.Add(imguiID, texture);

            return imguiID;
        }

        public void RecreateFontsTexture()
        {
            if (fontTextureView != null)
            {
                textureResources.Remove(fontTextureView.NativePointer);
                fontTextureView.Dispose();
                fontTextureView = null;
            }
            fontSampler?.Dispose();
            fontSampler = null;
            CreateFontsTexture();
        }

        public IntPtr GetOrLoadTexture(string path, ID3D11Device sourceDevice)
        {
            if (fileTextures.TryGetValue(path, out var cached))
                return cached.NativePointer;

            using var stream = File.OpenRead(path);
            using var factory = new IWICImagingFactory();
            using var decoder = factory.CreateDecoderFromStream(stream);
            using var frame = decoder.GetFrame(0);
            using var converter = factory.CreateFormatConverter();
            converter.Initialize(
                frame,
                PixelFormat.Format32bppRGBA,
                BitmapDitherType.None,
                null,
                0,
                BitmapPaletteType.Custom
            );
            var size = converter.Size;
            var rowPitch = checked((uint)size.Width * 4);
            var pixels = new byte[checked((int)(rowPitch * (uint)size.Height))];
            converter.CopyPixels(rowPitch, pixels);

            fixed (byte* data = pixels)
            {
                var description = new Texture2DDescription
                {
                    Width = (uint)size.Width,
                    Height = (uint)size.Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R8G8B8A8_UNorm,
                    SampleDescription = SampleDescription.Default,
                    Usage = ResourceUsage.Immutable,
                    BindFlags = BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                };
                var initial = new SubresourceData((IntPtr)data, rowPitch, 0);
                using var texture = sourceDevice.CreateTexture2D(description, [initial]);
                var view = sourceDevice.CreateShaderResourceView(texture);
                fileTextures[path] = view;
                textureResources[view.NativePointer] = view;
                return view.NativePointer;
            }
        }

        public IntPtr GetOrLoadBloomTexture(
            string path,
            ID3D11Device sourceDevice,
            Vector2 sampleOffset,
            float intensity,
            uint color
        )
        {
            var textureId = GetOrLoadTexture(path, sourceDevice);
            if (!bloomTextureIds.TryGetValue(path, out var bloomId))
            {
                bloomId = new IntPtr(Interlocked.Decrement(ref nextBloomTextureId));
                bloomTextureIds[path] = bloomId;
                bloomTextures[bloomId] = new BloomTexture
                {
                    Texture = textureResources[textureId],
                };
            }

            var bloom = bloomTextures[bloomId];
            bloom.SampleOffset = sampleOffset;
            bloom.Intensity = intensity;
            bloom.Color = color;
            return bloomId;
        }

        void CreateDeviceObjects()
        {
            var vertexShaderCode =
                @"                    cbuffer vertexBuffer : register(b0)                     {
                        float4x4 ProjectionMatrix;
                    };

                    struct VS_INPUT
                    {
                        float2 pos : POSITION;
                        float4 col : COLOR0;
                        float2 uv  : TEXCOORD0;
                    };

                    struct PS_INPUT
                    {
                        float4 pos : SV_POSITION;
                        float4 col : COLOR0;
                        float2 uv  : TEXCOORD0;
                    };

                    PS_INPUT main(VS_INPUT input)
                    {
                        PS_INPUT output;
                        output.pos = mul(ProjectionMatrix, float4(input.pos.xy, 0.f, 1.f));
                        output.col = input.col;
                        output.uv  = input.uv;
                        return output;
                    }";

            Compiler.Compile(
                vertexShaderCode,
                "main",
                "vs",
                "vs_4_0",
                out vertexShaderBlob,
                out var errorBlob
            );
            if (vertexShaderBlob == null)
                throw new Exception("error compiling vertex shader");

            vertexShader = device.CreateVertexShader(
                new Span<byte>(
                    (void*)vertexShaderBlob.BufferPointer,
                    checked((int)(nuint)vertexShaderBlob.BufferSize)
                )
            );

            var inputElements = new[]
            {
                new InputElementDescription(
                    "POSITION",
                    0,
                    Format.R32G32_Float,
                    0,
                    0,
                    InputClassification.PerVertexData,
                    0
                ),
                new InputElementDescription(
                    "TEXCOORD",
                    0,
                    Format.R32G32_Float,
                    8,
                    0,
                    InputClassification.PerVertexData,
                    0
                ),
                new InputElementDescription(
                    "COLOR",
                    0,
                    Format.R8G8B8A8_UNorm,
                    16,
                    0,
                    InputClassification.PerVertexData,
                    0
                ),
            };

            inputLayout = device.CreateInputLayout(inputElements, vertexShaderBlob);

            var constBufferDesc = new BufferDescription
            {
                ByteWidth = VertexConstantBufferSize,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CPUAccessFlags = CpuAccessFlags.Write,
            };
            constantBuffer = device.CreateBuffer(constBufferDesc);

            var pixelShaderCode =
                @"struct PS_INPUT
                    {
                        float4 pos : SV_POSITION;
                        float4 col : COLOR0;
                        float2 uv  : TEXCOORD0;
                    };

                    sampler sampler0;
                    Texture2D texture0;

                    float4 main(PS_INPUT input) : SV_Target
                    {
                        float4 out_col = input.col * texture0.Sample(sampler0, input.uv);
                        return out_col;
                    }";

            Compiler.Compile(
                pixelShaderCode,
                "main",
                "ps",
                "ps_4_0",
                out pixelShaderBlob,
                out errorBlob
            );
            if (pixelShaderBlob == null)
                throw new Exception("error compiling pixel shader");

            pixelShader = device.CreatePixelShader(
                new Span<byte>(
                    (void*)pixelShaderBlob.BufferPointer,
                    checked((int)(nuint)pixelShaderBlob.BufferSize)
                )
            );

            var bloomPixelShaderCode =
                @"cbuffer bloomBuffer : register(b0)
                    {
                        float4 BloomParams;
                        float4 GlowColor;
                    };

                    struct PS_INPUT
                    {
                        float4 pos : SV_POSITION;
                        float4 col : COLOR0;
                        float2 uv  : TEXCOORD0;
                    };

                    SamplerState sampler0 : register(s0);
                    Texture2D texture0 : register(t0);

                    void AddSample(float2 uv, float weight, inout float3 light, inout float coverage)
                    {
                        float4 sample = texture0.Sample(sampler0, uv);
                        float brightness = max(sample.r, max(sample.g, sample.b));
                        float contribution = sample.a * brightness * weight;
                        light += sample.rgb * contribution;
                        coverage += contribution;
                    }

                    float4 main(PS_INPUT input) : SV_Target
                    {
                        float2 radius = BloomParams.xy;
                        float3 light = 0;
                        float coverage = 0;

                        AddSample(input.uv, 0.12, light, coverage);
                        AddSample(input.uv + float2( radius.x * 0.5, 0), 0.12, light, coverage);
                        AddSample(input.uv + float2(-radius.x * 0.5, 0), 0.12, light, coverage);
                        AddSample(input.uv + float2(0,  radius.y * 0.5), 0.12, light, coverage);
                        AddSample(input.uv + float2(0, -radius.y * 0.5), 0.12, light, coverage);
                        AddSample(input.uv + radius * float2( 0.5,  0.5), 0.07, light, coverage);
                        AddSample(input.uv + radius * float2(-0.5,  0.5), 0.07, light, coverage);
                        AddSample(input.uv + radius * float2( 0.5, -0.5), 0.07, light, coverage);
                        AddSample(input.uv + radius * float2(-0.5, -0.5), 0.07, light, coverage);
                        AddSample(input.uv + float2( radius.x, 0), 0.03, light, coverage);
                        AddSample(input.uv + float2(-radius.x, 0), 0.03, light, coverage);
                        AddSample(input.uv + float2(0,  radius.y), 0.03, light, coverage);
                        AddSample(input.uv + float2(0, -radius.y), 0.03, light, coverage);

                        float3 color = light / max(coverage, 0.0001);
                        float alpha = saturate(coverage * BloomParams.z) * input.col.a * GlowColor.a;
                        return float4(color * GlowColor.rgb, alpha);
                    }";

            Compiler.Compile(
                bloomPixelShaderCode,
                "main",
                "ps",
                "ps_4_0",
                out bloomPixelShaderBlob,
                out errorBlob
            );
            if (bloomPixelShaderBlob == null)
                throw new Exception("error compiling bloom pixel shader");

            bloomPixelShader = device.CreatePixelShader(
                new Span<byte>(
                    (void*)bloomPixelShaderBlob.BufferPointer,
                    checked((int)(nuint)bloomPixelShaderBlob.BufferSize)
                )
            );
            bloomConstantBuffer = device.CreateBuffer(
                new BufferDescription
                {
                    ByteWidth = 32,
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.ConstantBuffer,
                    CPUAccessFlags = CpuAccessFlags.Write,
                }
            );

            bloomSampler = device.CreateSamplerState(
                new SamplerDescription
                {
                    Filter = Filter.MinMagMipLinear,
                    AddressU = TextureAddressMode.Border,
                    AddressV = TextureAddressMode.Border,
                    AddressW = TextureAddressMode.Border,
                    BorderColor = new Color4(0, 0, 0, 0),
                    ComparisonFunc = ComparisonFunction.Always,
                    MinLOD = 0,
                    MaxLOD = 0,
                }
            );

            var blendDesc = new BlendDescription { AlphaToCoverageEnable = false };

            blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
            {
                BlendEnable = true,
                SourceBlend = Blend.SourceAlpha,
                DestinationBlend = Blend.InverseSourceAlpha,
                BlendOperation = BlendOperation.Add,
                SourceBlendAlpha = Blend.One,
                DestinationBlendAlpha = Blend.InverseSourceAlpha,
                BlendOperationAlpha = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteEnable.All,
            };

            blendState = device.CreateBlendState(blendDesc);

            var rasterDesc = new RasterizerDescription
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                ScissorEnable = true,
                DepthClipEnable = true,
            };

            rasterizerState = device.CreateRasterizerState(rasterDesc);

            var stencilOpDesc = new DepthStencilOperationDescription(
                StencilOperation.Keep,
                StencilOperation.Keep,
                StencilOperation.Keep,
                ComparisonFunction.Always
            );
            var depthDesc = new DepthStencilDescription
            {
                DepthEnable = false,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunction.Always,
                StencilEnable = false,
                FrontFace = stencilOpDesc,
                BackFace = stencilOpDesc,
            };

            depthStencilState = device.CreateDepthStencilState(depthDesc);

            CreateFontsTexture();
        }

        void InvalidateDeviceObjects()
        {
            foreach (var texture in fileTextures.Values)
                texture.Dispose();
            fileTextures.Clear();
            bloomTextureIds.Clear();
            bloomTextures.Clear();
            ReleaseAndNullify(ref fontSampler);
            ReleaseAndNullify(ref bloomSampler);
            ReleaseAndNullify(ref fontTextureView);
            ReleaseAndNullify(ref indexBuffer);
            ReleaseAndNullify(ref vertexBuffer);
            ReleaseAndNullify(ref blendState);
            ReleaseAndNullify(ref depthStencilState);
            ReleaseAndNullify(ref rasterizerState);
            ReleaseAndNullify(ref pixelShader);
            ReleaseAndNullify(ref pixelShaderBlob);
            ReleaseAndNullify(ref bloomConstantBuffer);
            ReleaseAndNullify(ref bloomPixelShader);
            ReleaseAndNullify(ref bloomPixelShaderBlob);
            ReleaseAndNullify(ref constantBuffer);
            ReleaseAndNullify(ref inputLayout);
            ReleaseAndNullify(ref vertexShader);
            ReleaseAndNullify(ref vertexShaderBlob);
        }

        sealed class BloomTexture
        {
            public ID3D11ShaderResourceView Texture;
            public Vector2 SampleOffset;
            public float Intensity;
            public uint Color;
        }
    }
}
