void add_bloom_sample(float2 uv, float weight, inout float3 light, inout float coverage)
{
    float4 sample = source_texture.Sample(source_sampler, uv);
    float brightness = max(sample.r, max(sample.g, sample.b));
    float contribution = sample.a * brightness * weight;
    light += sample.rgb * contribution;
    coverage += contribution;
}

float4 effect(ForzaOSDInput input)
{
    float2 radius = params0.xy;
    float intensity = params0.z;
    float4 glow_color = params1;
    float3 light = 0;
    float coverage = 0;

    add_bloom_sample(input.uv, 0.12, light, coverage);
    add_bloom_sample(input.uv + float2( radius.x * 0.5, 0), 0.12, light, coverage);
    add_bloom_sample(input.uv + float2(-radius.x * 0.5, 0), 0.12, light, coverage);
    add_bloom_sample(input.uv + float2(0,  radius.y * 0.5), 0.12, light, coverage);
    add_bloom_sample(input.uv + float2(0, -radius.y * 0.5), 0.12, light, coverage);
    add_bloom_sample(input.uv + radius * float2( 0.5,  0.5), 0.07, light, coverage);
    add_bloom_sample(input.uv + radius * float2(-0.5,  0.5), 0.07, light, coverage);
    add_bloom_sample(input.uv + radius * float2( 0.5, -0.5), 0.07, light, coverage);
    add_bloom_sample(input.uv + radius * float2(-0.5, -0.5), 0.07, light, coverage);
    add_bloom_sample(input.uv + float2( radius.x, 0), 0.03, light, coverage);
    add_bloom_sample(input.uv + float2(-radius.x, 0), 0.03, light, coverage);
    add_bloom_sample(input.uv + float2(0,  radius.y), 0.03, light, coverage);
    add_bloom_sample(input.uv + float2(0, -radius.y), 0.03, light, coverage);

    float3 color = light / max(coverage, 0.0001);
    float alpha = saturate(coverage * intensity) * input.color.a * glow_color.a;
    return float4(color * glow_color.rgb, alpha);
}
