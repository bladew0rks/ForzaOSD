float4 straight_sample(float2 uv)
{
    float4 sample = source_texture.Sample(source_sampler, uv);
    sample.rgb /= max(sample.a, 0.0001);
    return sample;
}

float4 effect(ForzaOSDInput input)
{
    float rpm = saturate(params0.x);
    float throttle = saturate(params0.y);
    float separation = 0.0015 + rpm * 0.0035;
    float wobble = sin(input.screen_position.y * 0.075 + frame.x * 4.0) * separation * throttle;

    float4 center = straight_sample(input.uv);
    float4 red = straight_sample(input.uv + float2(separation + wobble, 0));
    float4 blue = straight_sample(input.uv - float2(separation - wobble, 0));
    float alpha = max(center.a, max(red.a, blue.a));
    float3 color = float3(red.r, center.g, blue.b);

    float scanline = 0.88 + 0.12 * sin(input.screen_position.y * 1.9);
    float sweep = exp2(-180.0 * pow(input.uv.x - frac(frame.x * 0.12), 2.0));
    color *= scanline;
    color += float3(0.12, 0.55, 0.65) * sweep * center.a * 0.22;

    return float4(color * input.color.rgb, alpha * input.color.a);
}
