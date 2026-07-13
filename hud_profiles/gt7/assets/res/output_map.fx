float4 main(PS_IN pin) {
  float4 color = txImage.Sample(samLinearSimple, pin.Tex);
  if (color.a < 0.01) discard; // or set a threshold as needed

  return color;
}

