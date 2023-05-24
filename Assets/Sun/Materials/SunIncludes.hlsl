void Blackbody_float(float t, out float3 output)
{
	float u = (0.860117757 + 1.54118254e-4 * t + 1.28641212e-7 * t * t)
		/ (1.0 + 8.42420235e-4 * t + 7.08145163e-7 * t * t);

	float v = (0.317398726 + 4.22806245e-5 * t + 4.20481691e-8 * t * t)
		/ (1.0 - 2.89741816e-5 * t + 1.61456053e-7 * t * t);

	float x = 3.0 * u / (2.0 * u - 8.0 * v + 4.0);
	float y = 2.0 * v / (2.0 * u - 8.0 * v + 4.0);
	float z = 1.0 - x - y;

	float Y = 1.0;
	float X = Y / y * x;
	float Z = Y / y * z;

	const float3x3 XYZtoRGB = float3x3(3.2404542, -1.5371385, -0.4985314,
		-0.9692660, 1.8760108, 0.0415560,
		0.0556434, -0.2040259, 1.0572252);

	output = max(float3(0.0, 0.0, 0.0), mul(XYZtoRGB, float3(X, Y, Z)) * pow(t * 0.0004, 4.0));
}

float3 Random3(float3 c)
{
	float j = 4096.0 * sin(dot(c, float3(17.0, 59.4, 15.0)));
	float3 r;
	r.z = frac(512.0 * j);
	j *= .125;
	r.x = frac(512.0 * j);
	j *= .125;
	r.y = frac(512.0 * j);
	return r - 0.5;
}

void Simplex3D_float(float3 p, out float result)
{
	/* 1. find current tetrahedron T and it's four vertices */
	/* s, s+i1, s+i2, s+1.0 - absolute skewed (integer) coordinates of T vertices */
	/* x, x1, x2, x3 - unskewed coordinates of p relative to each of T vertices*/

	/* calculate s and x */
	const float F3 = 0.3333333;
	const float G3 = 0.1666667;

	float3 s = floor(p + dot(p, F3.xxx));
	float3 x = p - s + dot(s, G3.xxx);

	/* calculate i1 and i2 */
	float3 e = step(0.0.xxx, x - x.yzx);
	float3 i1 = e * (1.0 - e.zxy);
	float3 i2 = 1.0 - e.zxy * (1.0 - e);

	/* x1, x2, x3 */
	float3 x1 = x - i1 + G3;
	float3 x2 = x - i2 + 2.0 * G3;
	float3 x3 = x - 1.0 + 3.0 * G3;

	/* 2. find four surflets and store them in d */
	float4 w, d;

	/* calculate surflet weights */
	w.x = dot(x, x);
	w.y = dot(x1, x1);
	w.z = dot(x2, x2);
	w.w = dot(x3, x3);

	/* w fades from 0.6 at the center of the surflet to 0.0 at the margin */
	w = max(0.6 - w, 0.0);

	/* calculate surflet components */
	d.x = dot(Random3(s), x);
	d.y = dot(Random3(s + i1), x1);
	d.z = dot(Random3(s + i2), x2);
	d.w = dot(Random3(s + 1.0), x3);

	/* multiply d by w^4 */
	w *= w;
	w *= w;
	d *= w;

	/* 3. return the sum of the four surflets */
	result = dot(d, 52.0.xxxx);
}
