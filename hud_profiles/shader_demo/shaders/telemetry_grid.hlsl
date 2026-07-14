float4 effect(ForzaOSDInput input)
{
    float2 p = saturate(input.local_position);
    float intensity = max(params0.x, 0.0);
    float animation_speed = params0.y;
    float rpm = saturate(params0.z);
    float throttle = saturate(params0.w);

    float2 cells = abs(frac(p * float2(26.0, 7.0)) - 0.5);
    float grid = 1.0 - smoothstep(0.42, 0.49, max(cells.x, cells.y));

    float sweep_position = frac(frame.x * 0.12 * animation_speed);
    float sweep_distance = abs(p.x - sweep_position);
    sweep_distance = min(sweep_distance, 1.0 - sweep_distance);
    float sweep = exp2(-120.0 * sweep_distance * sweep_distance);

    float edge = min(min(p.x, 1.0 - p.x), min(p.y, 1.0 - p.y));
    float border = 1.0 - smoothstep(0.006, 0.018, edge);
    float scanline = 0.76 + 0.24 * sin((input.screen_position.y + frame.x * 35.0 * animation_speed) * 1.7);
    float rpm_bar = 1.0 - smoothstep(0.0, 0.018, abs(p.y - (0.90 - rpm * 0.72)));
    rpm_bar *= 1.0 - smoothstep(rpm, rpm + 0.015, p.x);

    float light = 0.055 + grid * 0.12 + sweep * 0.48 + border * 0.78;
    light += rpm_bar * (0.55 + throttle * 0.45);
    light *= scanline * intensity;

    float alpha = saturate((0.20 + light) * input.color.a);
    return float4(input.color.rgb * light, alpha);
}
