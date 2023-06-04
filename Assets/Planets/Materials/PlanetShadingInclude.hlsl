#ifndef CUSTOM_FUNC_PLANET_SHADING
#define CUSTOM_FUNC_PLANET_SHADING

#define M_PI 3.1415926535897932
#define M_INV_PI 0.3183098861837906

float2 RaySphereInter(float3 ro, float3 rd, float3 pos, float rad)
{
	float3 l = pos - ro;
	float dt = dot(l, rd);
	float r2 = rad * rad;

	float ct2 = dot(l, l) - dt * dt;

	if (ct2 > r2)
		return float2(-1.0, -1.0);

	float at = sqrt(r2 - ct2);

	float tMin = dt - at;
	float tMax = dt + at;

	if (tMin < 0.0 && tMax < 0.0)
		return float2(-1.0, -1.0);

	float temptTMax = max(max(tMin, tMax), 0.0);
	tMin = max(min(tMin, tMax), 0.0);
	tMax = temptTMax;

	return float2(tMin, tMax);
}

float GetElevation(float3 planetPos, float rotation, float3 samplePos, float maxElevation, UnityTexture2D elevationTexture, UnitySamplerState ss, float lod)
{
	float3 localgeoNrm = normalize(samplePos - planetPos);
	float2 uv = float2(1.0 - (atan2(localgeoNrm.x, localgeoNrm.z) * (0.5 * M_INV_PI) + 0.25), 1.0 - acos(localgeoNrm.y) * M_INV_PI);
	uv.x -= rotation;

	return SAMPLE_TEXTURE2D_LOD(elevationTexture, ss, uv, lod).w * maxElevation;
}

#define M_SHADOW_STEPS 8
#define M_SHADOW_EPS 0.0001

void SamplePlanetShadows_float(float3 planetPos, float planetRad, float rotation, float3 position, float elevation, float maxElevation, float3 lightDir, float3 randomOffsets, UnityTexture2D elevationTexture, UnitySamplerState ss, float lod, out float outShadow)
{
	position += normalize(position - planetPos) * (elevation * maxElevation + M_SHADOW_EPS);
	position -= lightDir * M_SHADOW_EPS * 5000.0;
	float2 minMax = RaySphereInter(position, -lightDir, planetPos, planetRad + maxElevation);
	if (minMax.y < 0.0)
	{
		outShadow = 1.0;
		return;
	}

	float3 curDistance = 0.0;
	float3 sampleOffset = float3(0.0, 0.1, 0.2) + randomOffsets * 0.65 + 0.25;
	float3 samples = 1.0;

	[unroll]
	for (int i = 0; i < M_SHADOW_STEPS; ++i)
	{
		float3 frac = (float(i) + sampleOffset) / M_SHADOW_STEPS;
		frac = lerp(frac * frac, frac, 0.5);
		curDistance += minMax.y * frac;

		float3 samplePos = position + curDistance.x * -lightDir;
		float3 sampleElevation;
		sampleElevation.x = length(samplePos - planetPos) - planetRad;
		float3 groundElevation;
		groundElevation.x = GetElevation(planetPos, rotation, samplePos, maxElevation, elevationTexture, ss, lod);

		samplePos = position + curDistance.y * -lightDir;
		sampleElevation.y = length(samplePos - planetPos) - planetRad;
		groundElevation.y = GetElevation(planetPos, rotation, samplePos, maxElevation, elevationTexture, ss, lod);

		samplePos = position + curDistance.z * -lightDir;
		sampleElevation.z = length(samplePos - planetPos) - planetRad;
		groundElevation.z = GetElevation(planetPos, rotation, samplePos, maxElevation, elevationTexture, ss, lod);

		samples = min(samples, step(0.0, sampleElevation - groundElevation));
	}

	outShadow = (samples.x + samples.y + samples.z) / 3.0;
}

void PlanetCloudShadows_float(float3 planetPos, float cloudsRad, float cloudsRotation, float3 position, float3 lightDirection, UnityTexture2D cloudsTexture, UnitySamplerState ss, float lod, out float outShadow)
{
	outShadow = 1.0;
	float2 inter = RaySphereInter(position, -lightDirection, planetPos, cloudsRad);

	float3 hitPosition = position - lightDirection * inter.y;
	float3 normal = normalize(hitPosition - planetPos);
	float2 uv = float2(1.0 - (atan2(normal.x, normal.z) * (0.5 * M_INV_PI) + 0.25), 1.0 - acos(normal.y) * M_INV_PI);
	uv.x -= cloudsRotation;

	outShadow = SAMPLE_TEXTURE2D_LOD(cloudsTexture, ss, uv, lod).z;
}

void PlanetCloudShadows2_float(float3 planetPos, float cloudsRad, float cloudsRotation, float3 position, float3 lightDirection, UnityTexture2D cloudsTexture, UnitySamplerState ss, float lod, out float outShadow)
{
	outShadow = 1.0;
	float2 inter = RaySphereInter(position, -lightDirection, planetPos, cloudsRad);

	float3 hitPosition = position - lightDirection * inter.y;
	float3 normal = normalize(hitPosition - planetPos);
	float2 uv = float2(1.0 - (atan2(normal.x, normal.z) * (0.5 * M_INV_PI) + 0.25), 1.0 - acos(normal.y) * M_INV_PI);
	uv.x -= cloudsRotation;

	outShadow = SAMPLE_TEXTURE2D_LOD(cloudsTexture, ss, uv, lod).w;
}

float DistributionGGX(float nDotH, float a)
{
	a = max(a, 0.001);
	float a2 = a * a;

	float denom = (nDotH * nDotH * (a2 - 1.0) + 1.0);
	denom = M_PI * denom * denom;

	return a2 / denom;
}

float GeometrySchlickGGX(float nDotV, float k)
{
	return nDotV / (nDotV * (1.0 - k) + k);
}

float GeometrySmith(float nDotV, float nDotL, float a)
{
	float k = (a + 1.0) * (a + 1.0) * 0.125;
	return GeometrySchlickGGX(nDotV, k) * GeometrySchlickGGX(nDotL, k);
}

float3 FresnelSchlick(float cosTheta, float3 f0)
{
	return f0 + (1.0 - f0) * pow(1.0 - cosTheta, 5.0);
}

void PlanetShading_float(float3 cameraDirection, float3 baseColor, float roughness, float metallic, float specularF0 , float3 worldNormal, float3 lightDirection, float3 lightColor, float3 ambientColor, float shadows, out float3 output)
{
	float3 f0 = lerp(specularF0.xxx, baseColor, metallic);

	float3 halfVector = normalize(-lightDirection + cameraDirection);
	float nDotV = max(dot(worldNormal, cameraDirection), 0.0);
	float nDotL = max(dot(worldNormal, -lightDirection), 0.0);
	float hDotV = max(dot(halfVector, cameraDirection), 0.0);
	float nDotH = max(dot(worldNormal, halfVector), 0.0);

	float d = DistributionGGX(nDotH, roughness);
	float g = GeometrySmith(nDotV, nDotL, roughness);
	float3 f = FresnelSchlick(hDotV, f0);
	f *= (1.0 - smoothstep(0.8, 1.0, roughness) * (1.0 - metallic));

	float3 specular = d * g * f / max(4.0 * nDotV * nDotL, 0.0000001);
	float3 diffKomp = (1.0 - f) * (1.0 - metallic);

	float3 radiance = lightColor * shadows;
	float3 directLighting = (diffKomp * baseColor * M_INV_PI + specular) * radiance * nDotL;

	float3 indirectLighting = baseColor * M_INV_PI * ambientColor;

	output = directLighting + indirectLighting;
}

float RemapClamped(float value, float originalMin, float originalMax, float newMin, float newMax)
{
	return clamp(newMin + (((value - originalMin) / (originalMax - originalMin)) * (newMax - newMin)), newMin, newMax);
}

float Remap(float value, float originalMin, float originalMax, float newMin, float newMax)
{
	return newMin + (((value - originalMin) / (originalMax - originalMin)) * (newMax - newMin));
}

void CloudsShading_float(float3 cameraDirection, float3 baseColor, float roughness, float metallic, float specularF0, float3 worldNormal, float3 geoNormal, float3 lightDirection, float3 lightColor, float3 ambientColor, float shadows, out float3 output)
{
	float3 f0 = lerp(specularF0, baseColor, metallic);

	float3 halfVector = normalize(-lightDirection + cameraDirection);
	float nDotV = max(dot(worldNormal, cameraDirection), 0.0);
	float nDotL = max(dot(worldNormal, -lightDirection), 0.0);
	float hDotV = max(dot(halfVector, cameraDirection), 0.0);
	float nDotH = max(dot(worldNormal, halfVector), 0.0);

	float d = DistributionGGX(nDotH, roughness);
	float g = GeometrySmith(nDotV, nDotL, roughness);
	float3 f = FresnelSchlick(hDotV, f0);

	float3 specular = d * g * f / max(4.0 * nDotV * nDotL, 0.0000001);
	float3 diffKomp = (1.0 - f) * (1.0 - metallic);

	float3 radiance = lightColor * shadows;
	float lightWrap = RemapClamped(dot(geoNormal, -lightDirection), -0.2, 1.0, 0.0, 1.0);
	float3 directLighting = diffKomp * baseColor * M_INV_PI * radiance * lerp(nDotL, lightWrap, 0.6);
	directLighting += specular * radiance * nDotL;

	float3 indirectLighting = baseColor * M_INV_PI * ambientColor;

	output = directLighting + indirectLighting;
}

#define TRANSMITTANCE_TEXTURE_WIDTH 256
#define TRANSMITTANCE_TEXTURE_HEIGHT 64

#define SCATTERING_TEXTURE_R_SIZE 32
#define SCATTERING_TEXTURE_MU_SIZE 128
#define SCATTERING_TEXTURE_MU_S_SIZE 32
#define SCATTERING_TEXTURE_NU_SIZE 8

struct AtmosphereParameters
{
	float BottomRadius;
	float TopRadius;
	float MuSMin;

	float RayleighExpScale;
	float3 RayleighScattering;

	float MieExpScale;
	float3 MieScattering;
	float3 MieExtinction;
	float MiePhaseFunctionG;

	float AbsorptionTipAltitude;
	float AbsorptionTipWidth;
	float3 AbsorptionExtinction;
};

float ClampCosine(float mu) { return clamp(mu, -1.0, 1.0); }

float ClampRadius(in AtmosphereParameters atmosphere, float r) { return clamp(r, atmosphere.BottomRadius, atmosphere.TopRadius); }

float ClampDistance(float d) { return max(d, 0.0); }

bool RayIntersectsGround(in AtmosphereParameters atmosphere, float r, float mu)
{
	return mu < 0.0 && r * r * (mu * mu - 1.0) + atmosphere.BottomRadius * atmosphere.BottomRadius >= 0.0;
}

float GetTextureCoordFromUnitRange(float x, float textureSize)
{
	return 0.5 / textureSize + x * (1.0 - 1.0 / textureSize);
}

float DistanceToBottomAtmosphereBoundary(in AtmosphereParameters atmosphere, float r, float mu)
{
	float discriminant = r * r * (mu * mu - 1.0) + atmosphere.BottomRadius * atmosphere.BottomRadius;
	return ClampDistance(-r * mu - SafeSqrt(discriminant));
}

float DistanceToTopAtmosphereBoundary(in AtmosphereParameters atmosphere, float r, float mu)
{
	float discriminant = r * r * (mu * mu - 1.0) + atmosphere.TopRadius * atmosphere.TopRadius;
	return ClampDistance(-r * mu + SafeSqrt(discriminant));
}

float DistanceToNearestAtmosphereBoundary(in AtmosphereParameters atmosphere, float r, float mu, bool rayRMuIntersectsGround)
{
	if (rayRMuIntersectsGround)
		return DistanceToBottomAtmosphereBoundary(atmosphere, r, mu);
	else
		return DistanceToTopAtmosphereBoundary(atmosphere, r, mu);
}

float2 GetTransmittanceTextureUvFromRMu(in AtmosphereParameters atmosphere, float r, float mu)
{
	// Distance to top atmosphere boundary for a horizontal ray at ground level.
	float H = sqrt(atmosphere.TopRadius * atmosphere.TopRadius - atmosphere.BottomRadius * atmosphere.BottomRadius);
	// Distance to the horizon.
	float rho = SafeSqrt(r * r - atmosphere.BottomRadius * atmosphere.BottomRadius);
	// Distance to the top atmosphere boundary for the ray (r,mu), and its minimum
	// and maximum values over all mu - obtained for (r,1) and (r,mu_horizon).
	float d = DistanceToTopAtmosphereBoundary(atmosphere, r, mu);
	float dMin = atmosphere.TopRadius - r;
	float dMax = rho + H;
	float xMu = (d - dMin) / (dMax - dMin);
	float xR = rho / H;
	return float2(GetTextureCoordFromUnitRange(xMu, TRANSMITTANCE_TEXTURE_WIDTH), GetTextureCoordFromUnitRange(xR, TRANSMITTANCE_TEXTURE_HEIGHT));
}

float4 GetScatteringTextureUvwzFromRMuMuSNu(in AtmosphereParameters atmosphere,
	float r, float mu, float muS, float nu, bool rayRMuIntersectsGround)
{
	// Distance to top atmosphere boundary for a horizontal ray at ground level.
	float H = sqrt(atmosphere.TopRadius * atmosphere.TopRadius - atmosphere.BottomRadius * atmosphere.BottomRadius);
	// Distance to the horizon.
	float rho = SafeSqrt(r * r - atmosphere.BottomRadius * atmosphere.BottomRadius);
	float uR = GetTextureCoordFromUnitRange(rho / H, SCATTERING_TEXTURE_R_SIZE);

	// Discriminant of the quadratic equation for the intersections of the ray
	// (r,mu) with the ground (see RayIntersectsGround).
	float rMu = r * mu;
	float discriminant = rMu * rMu - r * r + atmosphere.BottomRadius * atmosphere.BottomRadius;
	float uMu;
	if (rayRMuIntersectsGround)
	{
		// Distance to the ground for the ray (r,mu), and its minimum and maximum
		// values over all mu - obtained for (r,-1) and (r,mu_horizon).
		float d = -rMu - SafeSqrt(discriminant);
		float dMin = r - atmosphere.BottomRadius;
		float dMax = rho;
		uMu = 0.5 - 0.5 * GetTextureCoordFromUnitRange(dMax == dMin ? 0.0 : (d - dMin) / (dMax - dMin), SCATTERING_TEXTURE_MU_SIZE / 2);
	}
	else
	{
		// Distance to the top atmosphere boundary for the ray (r,mu), and its
		// minimum and maximum values over all mu - obtained for (r,1) and
		// (r,mu_horizon).
		float d = -rMu + SafeSqrt(discriminant + H * H);
		float dMin = atmosphere.TopRadius - r;
		float dMax = rho + H;
		uMu = 0.5 + 0.5 * GetTextureCoordFromUnitRange((d - dMin) / (dMax - dMin), SCATTERING_TEXTURE_MU_SIZE / 2);
	}

	float d = DistanceToTopAtmosphereBoundary(atmosphere, atmosphere.BottomRadius, muS);
	float dMin = atmosphere.TopRadius - atmosphere.BottomRadius;
	float dMax = H;
	float a = (d - dMin) / (dMax - dMin);
	float D = DistanceToTopAtmosphereBoundary(atmosphere, atmosphere.BottomRadius, atmosphere.MuSMin);
	float A = (D - dMin) / (dMax - dMin);
	// An ad-hoc function equal to 0 for muS = mu_s_min (because then d = D and
	// thus a = A), equal to 1 for muS = 1 (because then d = dMin and thus
	// a = 0), and with a large slope around muS = 0, to get more texture 
	// samples near the horizon.
	float uMuS = GetTextureCoordFromUnitRange(max(1.0 - a / A, 0.0) / (1.0 + a), SCATTERING_TEXTURE_MU_S_SIZE);

	float uNu = (nu + 1.0) * 0.5;
	return float4(uNu, uMuS, uMu, uR);
}


float3 GetTransmittanceToTopAtmosphereBoundary(
	in AtmosphereParameters atmosphere,
	in UnityTexture2D transmittanceTexture, in UnitySamplerState ss,
	float r, float mu)
{
	float2 uv = GetTransmittanceTextureUvFromRMu(atmosphere, r, mu);
	return SAMPLE_TEXTURE2D_LOD(transmittanceTexture, ss, uv, 0.0).xyz;
}

float3 GetTransmittance(
	in AtmosphereParameters atmosphere,
	in UnityTexture2D transmittanceTexture, in UnitySamplerState ss,
	float r, float mu, float d, bool rayRMuIntersectsGround)
{
	float rD = ClampRadius(atmosphere, sqrt(d * d + 2.0 * r * mu * d + r * r));
	float muD = ClampCosine((r * mu + d) / rD);

	if (rayRMuIntersectsGround)
	{
		return min(
			GetTransmittanceToTopAtmosphereBoundary(atmosphere, transmittanceTexture, ss, rD, -muD)
			/ GetTransmittanceToTopAtmosphereBoundary(atmosphere, transmittanceTexture, ss, r, -mu),
			1.0);
	}
	else
	{
		return min(
			GetTransmittanceToTopAtmosphereBoundary(atmosphere, transmittanceTexture, ss, r, mu)
			/ GetTransmittanceToTopAtmosphereBoundary(atmosphere, transmittanceTexture, ss, rD, muD),
			1.0);
	}
}

float3 GetExtrapolatedSingleMieScattering(in AtmosphereParameters atmosphere, float4 scattering) 
{
	// Algebraically this can never be negative, but rounding errors can produce
	// that effect for sufficiently short view rays.
	return scattering.x <= 0.0 ? 0.0 : (scattering.xyz * scattering.w / scattering.x *
		(atmosphere.RayleighScattering.x / atmosphere.MieScattering.x) *
		(atmosphere.MieScattering / atmosphere.RayleighScattering));
}

float3 GetCombinedScattering(
	in AtmosphereParameters atmosphere,
	in UnityTexture3D scatteringTexture, in UnitySamplerState ss,
	float r, float mu, float muS, float nu,
	bool rayRMuIntersectsGround,
	out float3 singleMieScattering)
{
	float4 uvwz = GetScatteringTextureUvwzFromRMuMuSNu(atmosphere, r, mu, muS, nu, rayRMuIntersectsGround);
	float texCoordX = uvwz.x * float(SCATTERING_TEXTURE_NU_SIZE - 1);
	float texX = floor(texCoordX);
	float lerp = texCoordX - texX;
	float3 uvw0 = float3((texX + uvwz.y) / SCATTERING_TEXTURE_NU_SIZE, uvwz.z, uvwz.w);
	float3 uvw1 = float3((texX + 1.0 + uvwz.y) / SCATTERING_TEXTURE_NU_SIZE, uvwz.z, uvwz.w);

	float4 combinedScattering = SAMPLE_TEXTURE2D_LOD(scatteringTexture, ss, uvw0, 0.0) * (1.0 - lerp) + SAMPLE_TEXTURE2D_LOD(scatteringTexture, ss, uvw1, 0.0) * lerp;
	singleMieScattering = GetExtrapolatedSingleMieScattering(atmosphere, combinedScattering);
	return combinedScattering.xyz;
}

float RayleighPhaseFunction(float nu)
{
	float k = 3.0 / (16.0 * M_PI);
	return k * (1.0 + nu * nu);
}

float MiePhaseFunction(float g, float nu)
{
	float k = 3.0 / (8.0 * M_PI) * (1.0 - g * g) / (2.0 + g * g);
	return k * (1.0 + nu * nu) / pow(max(1.0 + g * g - 2.0 * g * nu, 0.0), 1.5);
}

float3 GetSkyRadianceToPoint(
	in AtmosphereParameters atmosphere,
	in UnityTexture2D transmittanceTexture,
	in UnityTexture3D scatteringTexture, UnitySamplerState ss,
	float3 camera, float3 position, float shadowLength,
	float3 sunDirection, out float3 transmittance)
{
	// Compute the distance to the top atmosphere boundary along the view ray,
	// assuming the viewer is in space (or NaN if the view ray does not intersect
	// the atmosphere).
	float3 viewRay = normalize(position - camera);
	float r = length(camera);
	float rmu = dot(camera, viewRay);
	float distanceToTopAtmosphereBoundary = -rmu - sqrt(rmu * rmu - r * r + atmosphere.TopRadius * atmosphere.TopRadius);
	// If the viewer is in space and the view ray intersects the atmosphere, move
	// the viewer to the top atmosphere boundary (along the view ray):
	if (distanceToTopAtmosphereBoundary > 0.0)
	{
		camera = camera + viewRay * distanceToTopAtmosphereBoundary;
		r = atmosphere.TopRadius;
		rmu += distanceToTopAtmosphereBoundary;
	}

	// Compute the r, mu, muS and nu parameters for the first texture lookup.
	float mu = rmu / r;
	float muS = dot(camera, sunDirection) / r;
	float nu = dot(viewRay, sunDirection);
	float d = length(position - camera);
	bool rayRMuIntersectsGround = RayIntersectsGround(atmosphere, r, mu);

	transmittance = GetTransmittance(atmosphere, transmittanceTexture, ss, r, mu, d, rayRMuIntersectsGround);

	float3 singleMieScattering;
	float3 scattering = GetCombinedScattering(atmosphere, scatteringTexture, ss,
		r, mu, muS, nu, rayRMuIntersectsGround, singleMieScattering);

	// Compute the r, mu, muS and nu parameters for the second texture lookup.
	// If shadowLength is not 0 (case of light shafts), we want to ignore the
	// scattering along the last shadowLength meters of the view ray, which we
	// do by subtracting shadowLength from d (this way scatteringP is equal to
	// the S|x_s=x_0-lv term in Eq. (17) of our paper).
	d = max(d - shadowLength, 0.0);
	float rP = ClampRadius(atmosphere, sqrt(d * d + 2.0 * r * mu * d + r * r));
	float muP = (r * mu + d) / rP;
	float muSP = (r * muS + d * nu) / rP;

	float3 singleMieScatteringP;
	float3 scatteringP = GetCombinedScattering(atmosphere, scatteringTexture, ss,
		rP, muP, muSP, nu, rayRMuIntersectsGround, singleMieScatteringP);

	// Combine the lookup results to get the scattering between camera and point.
	float3 shadowTransmittance = transmittance;
	if (shadowLength > 0.0)
	{
		// This is the T(x,x_s) term in Eq. (17) of our paper, for light shafts.
		shadowTransmittance = GetTransmittance(atmosphere, transmittanceTexture, ss,
			r, mu, d, rayRMuIntersectsGround);
		
		shadowTransmittance = saturate(shadowTransmittance);

	}
	scattering = scattering - shadowTransmittance * scatteringP;
	singleMieScattering = singleMieScattering - shadowTransmittance * singleMieScatteringP;
	//singleMieScattering = GetExtrapolatedSingleMieScattering(atmosphere, float4(scattering, max(singleMieScattering.x, 0.0001)));

	// Hack to avoid rendering artifacts when the sun is below the horizon.
	//singleMieScattering *= smoothstep(0.0, 0.3, muS);

	return scattering * RayleighPhaseFunction(nu) + singleMieScattering * MiePhaseFunction(atmosphere.MiePhaseFunctionG, nu);
}

void GetSphereShadowInOut(float3 spherePos, float radius, float3 rayOrigin, float3 viewDirection, float3 sunDirection, out float dIn, out float dOut)
{
	float3 pos = rayOrigin - spherePos;
	float posDotSun = dot(pos, sunDirection);
	float viewDotSun = dot(viewDirection, sunDirection);
	float k = 0.001;
	float l = 1.0 + k * k;
	float a = 1.0 - l * viewDotSun * viewDotSun;
	float b = dot(pos, viewDirection) - l * posDotSun * viewDotSun - k * radius * viewDotSun;
	float c = dot(pos, pos) - l * posDotSun * posDotSun - 2.0 * k * radius * posDotSun - radius * radius;
	float discriminant = b * b - a * c;
	if (discriminant > 0.0)
	{
		dIn = max(0.0, (-b - sqrt(discriminant)) / a);
		dOut = (-b + sqrt(discriminant)) / a;
		// The values of d for which delta is equal to 0 and radius / k.
		float dBase = -posDotSun / viewDotSun;
		float dApex = -(posDotSun + radius / k) / viewDotSun;
		if (viewDotSun > 0.0)
		{
			dIn = max(dIn, dApex);
			dOut = a > 0.0 ? min(dOut, dBase) : dBase;
		}
		else
		{
			dIn = a > 0.0 ? max(dIn, dBase) : dBase;
			dOut = min(dOut, dApex);
		}
	}
	else
	{
		dIn = 0.0;
		dOut = 0.0;
	}
}

void AtmosphereScattering2_float(float3 rayOrigin, float3 rayDirection, float3 planetPos, float bottomRadius, float topRadius,
	float3 rayleighScattering, float3 mieScattering, float miePhaseFunctionG, float cosineMaxSunAngle, float3 lightDirection, float3 lightColor,
	UnityTexture2D transmittanceTexture, UnityTexture3D scatteringTexture, UnitySamplerState ss, out float3 output)
{
	AtmosphereParameters atmosphere;
	atmosphere.BottomRadius = bottomRadius;
	atmosphere.TopRadius = topRadius;
	atmosphere.MuSMin = cosineMaxSunAngle;

	//atmosphere.RayleighExpScale = -1.0 / hR;
	atmosphere.RayleighScattering = rayleighScattering;

	//atmosphere.MieExpScale = -1.0 / hM;;
	atmosphere.MieScattering = mieScattering;
	//atmosphere.MieExtinction = mieExtinction;
	atmosphere.MiePhaseFunctionG = miePhaseFunctionG;

	//atmosphere.AbsorptionTipAltitude = hO;
	//atmosphere.AbsorptionTipWidth = wO;
	//atmosphere.AbsorptionExtinction = betaO;

	float2 minMax1 = RaySphereInter(rayOrigin, rayDirection, planetPos, atmosphere.TopRadius);
	if (max(minMax1.x, minMax1.y) <= 0.0)
	{
		output = 0.0;
		return;
	}

	float2 minMax2 = RaySphereInter(rayOrigin, rayDirection, planetPos, atmosphere.BottomRadius);
	float planetT = minMax2.x < 0.0 ? minMax2.y : minMax2.x;

	float tMin = minMax1.x < 0.0 ? 0.0 : minMax1.x;
	tMin = planetT < 0.0 ? tMin : min(tMin, planetT);
	float tMax = minMax1.y;
	tMax = planetT < 0.0 ? tMax : min(tMax, planetT);

	float3 position = rayOrigin + rayDirection * tMax;

	float shadowIn, shadowOut;
	GetSphereShadowInOut(planetPos, atmosphere.BottomRadius, rayOrigin, rayDirection, -lightDirection, shadowIn, shadowOut);

	float lightshaftFade = 1.0 - smoothstep(0.0, 0.3, dot(normalize(rayOrigin + rayDirection * (tMin + tMax) * 0.5 - planetPos), lightDirection));
	lightshaftFade *= lightshaftFade;
	float shadowLength = shadowOut - shadowIn;
	shadowLength = max(0.0, shadowLength - max(tMin - shadowIn, 0.0) - max(shadowOut - tMax, 0.0)) * (1.0 - lightshaftFade);

	float3 transmittance;
	output = GetSkyRadianceToPoint(atmosphere, transmittanceTexture, scatteringTexture, ss,
		rayOrigin - planetPos, position - planetPos, shadowLength, -lightDirection, transmittance);

	output *= lightColor;
}

float3 GetSkyTransmittanceToPoint(
	in AtmosphereParameters atmosphere,
	in UnityTexture2D transmittanceTexture, UnitySamplerState ss,
	float3 camera, float3 position)
{
	// Compute the distance to the top atmosphere boundary along the view ray,
	// assuming the viewer is in space (or NaN if the view ray does not intersect
	// the atmosphere).
	float3 viewRay = normalize(position - camera);
	float r = length(camera);
	float rmu = dot(camera, viewRay);
	float distanceToTopAtmosphereBoundary = -rmu - sqrt(rmu * rmu - r * r + atmosphere.TopRadius * atmosphere.TopRadius);
	// If the viewer is in space and the view ray intersects the atmosphere, move
	// the viewer to the top atmosphere boundary (along the view ray):
	if (distanceToTopAtmosphereBoundary > 0.0)
	{
		camera = camera + viewRay * distanceToTopAtmosphereBoundary;
		r = atmosphere.TopRadius;
		rmu += distanceToTopAtmosphereBoundary;
	}

	// Compute the r, mu, muS and nu parameters for the first texture lookup.
	float mu = rmu / r;
	float d = length(position - camera);
	bool rayRMuIntersectsGround = RayIntersectsGround(atmosphere, r, mu);

	return GetTransmittance(atmosphere, transmittanceTexture, ss, r, mu, d, rayRMuIntersectsGround);
}

void AtmosphereAbsorption2_float(float3 rayOrigin, float3 rayDirection, float3 planetPos, float bottomRadius, float topRadius, float cosineMaxSunAngle,
	UnityTexture2D transmittanceTexture, UnitySamplerState ss, out float3 output)
{
	AtmosphereParameters atmosphere;
	atmosphere.BottomRadius = bottomRadius;
	atmosphere.TopRadius = topRadius;
	atmosphere.MuSMin = cosineMaxSunAngle;

	//atmosphere.RayleighExpScale = -1.0 / hR;
	//atmosphere.RayleighScattering = betaR;

	//atmosphere.MieExpScale = -1.0 / hM;;
	//atmosphere.MieScattering = betaM;
	//atmosphere.MieExtinction = mieExtinction;

	//atmosphere.AbsorptionTipAltitude = hO;
	//atmosphere.AbsorptionTipWidth = wO;
	//atmosphere.AbsorptionExtinction = betaO;

	float2 minMax1 = RaySphereInter(rayOrigin, rayDirection, planetPos, atmosphere.TopRadius);
	if (max(minMax1.x, minMax1.y) <= 0.0)
	{
		output = 1.0;
		return;
	}

	float2 minMax2 = RaySphereInter(rayOrigin, rayDirection, planetPos, atmosphere.BottomRadius);
	float planetT = minMax2.x < 0.0 ? minMax2.y : minMax2.x;

	float tMin = minMax1.x < 0.0 ? 0.0 : minMax1.x;
	tMin = planetT < 0.0 ? tMin : min(tMin, planetT);
	float tMax = minMax1.y;
	tMax = planetT < 0.0 ? tMax : min(tMax, planetT);

	float3 position = rayOrigin + rayDirection * tMax;
	output = GetSkyTransmittanceToPoint(atmosphere, transmittanceTexture, ss, rayOrigin - planetPos, position - planetPos);
}

float HenyeyGreenstein(float cosAngle, float g)
{
	return (1.0 - g * g) / pow(1.0 + g * g - 2.0 * g * cosAngle, 1.5) * 0.25 * M_PI;
}

void SphereSoftShadow_float(float3 position, float3 lightDirection, float3 center, float radius, float hardness, out float shadow)
{
	float3 oc = position - center;
	float b = dot(oc, lightDirection);
	float c = dot(oc, oc) - radius * radius;
	float h = b * b - c;

	shadow = (b > 0.0) ? step(-0.0001, c) : smoothstep(0.0, 1.0, h * hardness / b);
}

void RingShading_float(float3 viewDirection, float3 albedo, float3 backsideAlbedo, float phaseFuncG, float alpha, float3 worldNormal, float3 lightDirection, float3 lightColor, float3 ambientColor, float shadows, out float3 output)
{
	float viewDotLight = clamp(dot(viewDirection, lightDirection), -1.0, 1.0);
	float normalDotLight = dot(worldNormal, lightDirection);
	float normalDotView = dot(worldNormal, viewDirection);
	float3 scatteringColor = lerp(albedo, backsideAlbedo * (1.0 - alpha * 0.75), smoothstep(-0.3, 0.3, normalDotLight));
	output = scatteringColor * HenyeyGreenstein(viewDotLight, phaseFuncG) * M_INV_PI * 0.125 * 0.5;
	output *= exp(-0.05 / max(abs(normalDotLight), 0.001)) * 0.5 + 0.5;
	output *= lightColor * shadows;
	output += ambientColor * M_INV_PI * 0.5;
	output *= RemapClamped(exp(-0.25 / max(abs(normalDotView), 0.001)), 0.0, 1.0, 0.15, 1.0);
}

void RingShadows_float(float3 ringPositionRel, float3 ringNormalRel, float ringInnerRadiusRel, float ringOutRadiusRel, float3 position, float3 lightDirection, UnityTexture2D ringsTexture, UnitySamplerState ss, out float outShadow)
{
	float t = dot(ringNormalRel, ringPositionRel - position) / dot(ringNormalRel, -lightDirection);
	float distToCenter = length(position - lightDirection * t - ringPositionRel);
	float shadowMask = step(0.0, t) * step(ringInnerRadiusRel, distToCenter) * step(distToCenter, ringOutRadiusRel);
	float shadowSample = SAMPLE_TEXTURE2D(ringsTexture, ss, float2(Remap(distToCenter, ringInnerRadiusRel, ringOutRadiusRel, 0.0, 1.0), 0.0)).y;
	outShadow = 1.0 - shadowSample * shadowMask;
}

#endif // CUSTOM_FUNC_PLANET_SHADING