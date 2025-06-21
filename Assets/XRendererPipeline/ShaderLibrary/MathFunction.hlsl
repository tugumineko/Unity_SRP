#ifndef MATH_FUNCTION_INCLUDED
#define MATH_FUNCTION_INCLUDED

#define PI 3.14159265358979323846
#define HALF_PI 1.5707963267948966

float FastAcosForAbsCos(float in_abs_cos)
{
    float _local_tmp = ((in_abs_cos * -0.0187292993068695068359375 + 0.074261002242565155029296875) * in_abs_cos - 0.212114393711090087890625) * in_abs_cos + 1.570728778839111328125;
    return _local_tmp * sqrt(1.0 - in_abs_cos);
}

float FastAcos(float in_cos)
{
    float local_abs_cos = abs(in_cos);
    float local_abs_acos = FastAcosForAbsCos(local_abs_cos);
    return in_cos < 0.0 ?  PI - local_abs_acos : local_abs_acos;
}

#endif