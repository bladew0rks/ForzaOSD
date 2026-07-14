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
    public unsafe class ImGuiRenderer : IDisposable
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
        ID3D11Buffer customShaderConstantBuffer;
        ID3D11SamplerState fontSampler;
        ID3D11SamplerState customClampSampler;
        ID3D11SamplerState customWrapSampler;
        ID3D11SamplerState customBorderSampler;
        ID3D11ShaderResourceView fontTextureView;
        ID3D11ShaderResourceView whiteTextureView;
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
        Dictionary<IntPtr, CustomShaderDraw> customShaderDraws = new();
        Dictionary<IntPtr, EffectLayerDraw> effectLayerDraws = new();
        List<EffectLayerTarget> effectLayerTargets = new();
        long nextCustomShaderTextureId = long.MinValue;
        long nextEffectLayerId = 1;
        long effectFrame;
        CustomShaderProgram builtInBloomProgram;

        internal CustomShaderProgram BuiltInBloomProgram => builtInBloomProgram;
        internal static readonly IntPtr BeginEffectLayerCallback = new(-1001);
        internal static readonly IntPtr EndEffectLayerCallback = new(-1002);

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

        public void Render(ImDrawDataPtr data, ID3D11RenderTargetView mainTarget)
        {
            if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
            {
                EndFrameEffects();
                return;
            }

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

            float L = data.DisplayPos.X;
            float R = data.DisplayPos.X + data.DisplaySize.X;
            float T = data.DisplayPos.Y;
            float B = data.DisplayPos.Y + data.DisplaySize.Y;
            UpdateProjection(ctx, L, R, T, B);

            SetupRenderState(data, ctx);

            int global_idx_offset = 0;
            int global_vtx_offset = 0;
            Vector2 clip_off = data.DisplayPos;
            EffectLayerDraw activeLayer = null;
            for (int n = 0; n < data.CmdListsCount; n++)
            {
                var cmdList = data.CmdLists[n];
                for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
                {
                    var cmd = cmdList.CmdBuffer[i];
                    if (cmd.UserCallback == BeginEffectLayerCallback)
                    {
                        if (activeLayer != null)
                            throw new InvalidOperationException("Effect layers cannot be nested");
                        if (!effectLayerDraws.TryGetValue(cmd.UserCallbackData, out activeLayer))
                            throw new InvalidOperationException("Unknown effect layer callback");
                        BeginEffectLayer(ctx, activeLayer);
                        clip_off = new(activeLayer.Bounds.X, activeLayer.Bounds.Y);
                    }
                    else if (cmd.UserCallback == EndEffectLayerCallback)
                    {
                        if (
                            activeLayer == null
                            || activeLayer.Id != cmd.UserCallbackData
                        )
                            throw new InvalidOperationException("Mismatched effect layer callback");
                        ctx.OMSetRenderTargets(mainTarget);
                        SetupRenderState(data, ctx);
                        UpdateProjection(ctx, L, R, T, B);
                        clip_off = data.DisplayPos;
                        activeLayer = null;
                    }
                    else if (cmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException("user callbacks not implemented");
                    }
                    else
                    {
                        var clipWidth = activeLayer?.Target.Width ?? (int)data.DisplaySize.X;
                        var clipHeight = activeLayer?.Target.Height ?? (int)data.DisplaySize.Y;
                        var rect = new Vortice.RawRect(
                            Math.Clamp((int)(cmd.ClipRect.X - clip_off.X), 0, clipWidth),
                            Math.Clamp((int)(cmd.ClipRect.Y - clip_off.Y), 0, clipHeight),
                            Math.Clamp((int)(cmd.ClipRect.Z - clip_off.X), 0, clipWidth),
                            Math.Clamp((int)(cmd.ClipRect.W - clip_off.Y), 0, clipHeight)
                        );
                        ctx.RSSetScissorRects([rect]);

                        ID3D11ShaderResourceView texture;
                        if (customShaderDraws.TryGetValue(cmd.TextureId, out var custom))
                        {
                            UpdateCustomShaderConstants(ctx, data, custom, activeLayer);
                            ctx.PSSetShader(custom.Program.Shader);
                            ctx.PSSetConstantBuffers(0, [customShaderConstantBuffer]);
                            ctx.PSSetSamplers(0, [SamplerFor(custom.Sampler)]);
                            texture = custom.Texture;
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
            if (activeLayer != null)
                ctx.OMSetRenderTargets(mainTarget);
            EndFrameEffects();
        }

        void UpdateProjection(ID3D11DeviceContext ctx, float L, float R, float T, float B)
        {
            var constResource = ctx.Map(
                constantBuffer,
                0,
                MapMode.WriteDiscard,
                Vortice.Direct3D11.MapFlags.None
            );
            var span = constResource.AsSpan<float>(VertexConstantBufferSize);
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
        }

        void UpdateCustomShaderConstants(
            ID3D11DeviceContext ctx,
            ImDrawDataPtr data,
            CustomShaderDraw draw,
            EffectLayerDraw activeLayer
        )
        {
            var resource = ctx.Map(
                customShaderConstantBuffer,
                0,
                MapMode.WriteDiscard,
                Vortice.Direct3D11.MapFlags.None
            );
            var values = resource.AsSpan<float>(28);
            values.Clear();
            values[0] = data.DisplaySize.X;
            values[1] = data.DisplaySize.Y;
            values[2] = 1f / Math.Max(data.DisplaySize.X, 1f);
            values[3] = 1f / Math.Max(data.DisplaySize.Y, 1f);
            values[4] = draw.Bounds.X;
            values[5] = draw.Bounds.Y;
            values[6] = draw.Bounds.Z;
            values[7] = draw.Bounds.W;
            values[8] = draw.Time;
            values[9] = draw.DeltaTime;
            if (activeLayer != null)
            {
                values[10] = activeLayer.Bounds.X;
                values[11] = activeLayer.Bounds.Y;
            }
            draw.Parameters.CopyTo(values[12..]);
            ctx.Unmap(customShaderConstantBuffer, 0);
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

        internal CustomShaderProgram CreateCustomShader(byte[] bytecode) =>
            new(device.CreatePixelShader(bytecode));

        internal IntPtr RegisterCustomShaderDraw(
            CustomShaderProgram program,
            IntPtr sourceTexture,
            Vector4 bounds,
            float time,
            float deltaTime,
            ReadOnlySpan<float> parameters,
            ShaderSampler sampler
        )
        {
            var texture = sourceTexture == IntPtr.Zero
                ? whiteTextureView
                : textureResources[sourceTexture];
            return RegisterCustomShaderDraw(
                program,
                texture,
                bounds,
                time,
                deltaTime,
                parameters,
                sampler
            );
        }

        IntPtr RegisterCustomShaderDraw(
            CustomShaderProgram program,
            ID3D11ShaderResourceView texture,
            Vector4 bounds,
            float time,
            float deltaTime,
            ReadOnlySpan<float> parameters,
            ShaderSampler sampler
        )
        {
            var id = new IntPtr(nextCustomShaderTextureId++);
            var values = new float[16];
            parameters[..Math.Min(parameters.Length, values.Length)].CopyTo(values);
            customShaderDraws[id] = new()
            {
                Program = program,
                Texture = texture,
                Bounds = bounds,
                Time = time,
                DeltaTime = deltaTime,
                Parameters = values,
                Sampler = sampler,
            };
            return id;
        }

        internal EffectLayerRegistration RegisterEffectLayer(
            CustomShaderProgram program,
            Vector4 bounds,
            float time,
            float deltaTime,
            ReadOnlySpan<float> parameters,
            ShaderSampler sampler
        )
        {
            var width = Math.Clamp((int)Math.Ceiling(bounds.Z), 1, 4096);
            var height = Math.Clamp((int)Math.Ceiling(bounds.W), 1, 4096);
            var target = AcquireEffectLayerTarget(width, height);
            var id = new IntPtr(nextEffectLayerId++);
            effectLayerDraws[id] = new() { Id = id, Bounds = bounds, Target = target };
            var textureId = RegisterCustomShaderDraw(
                program,
                target.View,
                bounds,
                time,
                deltaTime,
                parameters,
                sampler
            );
            return new(id, textureId);
        }

        EffectLayerTarget AcquireEffectLayerTarget(int width, int height)
        {
            var target = effectLayerTargets
                .Where(item => !item.InUse && item.Width >= width && item.Height >= height)
                .OrderBy(item => (long)item.Width * item.Height)
                .FirstOrDefault();
            if (target == null)
            {
                var description = new Texture2DDescription
                {
                    Width = (uint)width,
                    Height = (uint)height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.R8G8B8A8_UNorm,
                    SampleDescription = SampleDescription.Default,
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                    CPUAccessFlags = CpuAccessFlags.None,
                };
                var texture = device.CreateTexture2D(description);
                target = new(
                    texture,
                    device.CreateRenderTargetView(texture),
                    device.CreateShaderResourceView(texture),
                    width,
                    height
                );
                effectLayerTargets.Add(target);
            }
            target.InUse = true;
            target.LastUsedFrame = effectFrame;
            return target;
        }

        void BeginEffectLayer(ID3D11DeviceContext ctx, EffectLayerDraw layer)
        {
            ctx.PSSetShaderResources(0, [null]);
            ctx.OMSetRenderTargets(layer.Target.RenderTarget);
            ctx.ClearRenderTargetView(layer.Target.RenderTarget, new Color4(0, 0, 0, 0));
            ctx.RSSetViewports(
                [
                    new Viewport
                    {
                        Width = layer.Target.Width,
                        Height = layer.Target.Height,
                        MinDepth = 0,
                        MaxDepth = 1,
                    },
                ]
            );
            UpdateProjection(
                ctx,
                layer.Bounds.X,
                layer.Bounds.X + layer.Bounds.Z,
                layer.Bounds.Y,
                layer.Bounds.Y + layer.Bounds.W
            );
        }

        void EndFrameEffects()
        {
            customShaderDraws.Clear();
            effectLayerDraws.Clear();
            effectFrame++;
            foreach (var target in effectLayerTargets)
                target.InUse = false;
            for (var i = effectLayerTargets.Count - 1; i >= 0; i--)
            {
                var target = effectLayerTargets[i];
                if (effectLayerTargets.Count <= 8 || effectFrame - target.LastUsedFrame <= 120)
                    continue;
                target.Dispose();
                effectLayerTargets.RemoveAt(i);
            }
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

            customShaderConstantBuffer = device.CreateBuffer(
                new BufferDescription
                {
                    ByteWidth = 28 * sizeof(float),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.ConstantBuffer,
                    CPUAccessFlags = CpuAccessFlags.Write,
                }
            );

            customClampSampler = CreateCustomSampler(TextureAddressMode.Clamp);
            customWrapSampler = CreateCustomSampler(TextureAddressMode.Wrap);
            customBorderSampler = CreateCustomSampler(TextureAddressMode.Border);
            builtInBloomProgram = CreateCustomShader(
                CustomShaderCompiler.CompileFile(
                    Path.Combine(AppContext.BaseDirectory, "Shaders", "bloom.hlsl")
                )
            );
            CreateWhiteTexture();

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
            foreach (var target in effectLayerTargets)
                target.Dispose();
            fileTextures.Clear();
            effectLayerTargets.Clear();
            effectLayerDraws.Clear();
            customShaderDraws.Clear();
            ReleaseAndNullify(ref fontSampler);
            ReleaseAndNullify(ref customClampSampler);
            ReleaseAndNullify(ref customWrapSampler);
            ReleaseAndNullify(ref customBorderSampler);
            ReleaseAndNullify(ref fontTextureView);
            ReleaseAndNullify(ref whiteTextureView);
            ReleaseAndNullify(ref indexBuffer);
            ReleaseAndNullify(ref vertexBuffer);
            ReleaseAndNullify(ref blendState);
            ReleaseAndNullify(ref depthStencilState);
            ReleaseAndNullify(ref rasterizerState);
            ReleaseAndNullify(ref pixelShader);
            ReleaseAndNullify(ref pixelShaderBlob);
            ReleaseAndNullify(ref customShaderConstantBuffer);
            builtInBloomProgram?.Dispose();
            builtInBloomProgram = null;
            ReleaseAndNullify(ref constantBuffer);
            ReleaseAndNullify(ref inputLayout);
            ReleaseAndNullify(ref vertexShader);
            ReleaseAndNullify(ref vertexShaderBlob);
        }

        ID3D11SamplerState CreateCustomSampler(TextureAddressMode addressMode) =>
            device.CreateSamplerState(
                new SamplerDescription
                {
                    Filter = Filter.MinMagMipLinear,
                    AddressU = addressMode,
                    AddressV = addressMode,
                    AddressW = addressMode,
                    BorderColor = new Color4(0, 0, 0, 0),
                    ComparisonFunc = ComparisonFunction.Always,
                    MinLOD = 0,
                    MaxLOD = 0,
                }
            );

        void CreateWhiteTexture()
        {
            uint pixel = uint.MaxValue;
            var description = new Texture2DDescription
            {
                Width = 1,
                Height = 1,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                SampleDescription = SampleDescription.Default,
                Usage = ResourceUsage.Immutable,
                BindFlags = BindFlags.ShaderResource,
                CPUAccessFlags = CpuAccessFlags.None,
            };
            var initial = new SubresourceData((IntPtr)(&pixel), sizeof(uint), 0);
            using var texture = device.CreateTexture2D(description, [initial]);
            whiteTextureView = device.CreateShaderResourceView(texture);
        }

        internal sealed class CustomShaderProgram(ID3D11PixelShader shader) : IDisposable
        {
            internal ID3D11PixelShader Shader { get; } = shader;

            public void Dispose() => Shader.Dispose();
        }

        sealed class CustomShaderDraw
        {
            internal CustomShaderProgram Program;
            internal ID3D11ShaderResourceView Texture;
            internal Vector4 Bounds;
            internal float Time;
            internal float DeltaTime;
            internal float[] Parameters;
            internal ShaderSampler Sampler;
        }

        sealed class EffectLayerDraw
        {
            internal IntPtr Id;
            internal Vector4 Bounds;
            internal EffectLayerTarget Target;
        }

        sealed class EffectLayerTarget(
            ID3D11Texture2D texture,
            ID3D11RenderTargetView renderTarget,
            ID3D11ShaderResourceView view,
            int width,
            int height
        ) : IDisposable
        {
            internal ID3D11Texture2D Texture { get; } = texture;
            internal ID3D11RenderTargetView RenderTarget { get; } = renderTarget;
            internal ID3D11ShaderResourceView View { get; } = view;
            internal int Width { get; } = width;
            internal int Height { get; } = height;
            internal bool InUse;
            internal long LastUsedFrame;

            public void Dispose()
            {
                View.Dispose();
                RenderTarget.Dispose();
                Texture.Dispose();
            }
        }

        internal readonly record struct EffectLayerRegistration(
            IntPtr LayerId,
            IntPtr TextureId
        );

        ID3D11SamplerState SamplerFor(ShaderSampler sampler) =>
            sampler switch
            {
                ShaderSampler.Wrap => customWrapSampler,
                ShaderSampler.Border => customBorderSampler,
                _ => customClampSampler,
            };

        internal enum ShaderSampler
        {
            Clamp,
            Wrap,
            Border,
        }
    }
}
