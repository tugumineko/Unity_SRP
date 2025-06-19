
using UnityEngine;

[ExecuteAlways]
public class TimeCtrl : MonoBehaviour
{
    public bool UpdateTime = true;

    [Range(0f,24f)]
    public float TimeofDay = 6f;

    public float AllDayInMinutes = 1f; //游戏里一天等于现实时间AllDayInMinutes分钟

    [Range(0f,24f)]
    public float DayStartTime = 5f;

    [Range(1f, 24f)] 
    public float DayDuration = 14f;

    private float mTimeProgression;
    private float mNightStartTime;
    private float mNightDuration; 

    private float mCurveTime;  //float关键帧
    private float mGradientTime; //color关键帧
    private float mDayProgression;//白天进行的百分比
    private float mNightProgression;//夜晚进行的百分比
    
    public float CurveTime => mCurveTime;
    public float GradientTime => mGradientTime;
    public float DayProgression => mDayProgression;
    public float NightProgression => mNightProgression;
    
    public bool IsDay => (TimeofDay >= DayStartTime && TimeofDay <= (DayStartTime + DayDuration));

    private void Start()
    {
        if (AllDayInMinutes > 0)
        {
            mTimeProgression = 24f / 60f / AllDayInMinutes;
        }
        else
        {
            mTimeProgression = 0;
        }

        mNightStartTime = Mathf.Min(DayStartTime + DayDuration, 24f);
        mNightDuration = 24f - mNightStartTime + DayStartTime;
        CalculateProgression();
    }

    private void Update()
    {
        if (Application.isPlaying && UpdateTime)
        {
            TimeofDay += Time.deltaTime * mTimeProgression;
            if (TimeofDay >= 24f)
            {
                TimeofDay %= 24f;
            }
        }
        CalculateProgression();
    }

    void CalculateProgression()
    {
        mCurveTime = TimeofDay / 24f * 100f;
        mGradientTime = TimeofDay / 24f;
        mDayProgression = Mathf.Clamp01((TimeofDay - DayStartTime) / DayDuration);
        if (mNightDuration > 0)
        {
            if (TimeofDay >= mNightStartTime)
            {
                mNightProgression = Mathf.Clamp01((TimeofDay - mNightStartTime) / mNightDuration);
            }
            else
            {
                mNightProgression = Mathf.Clamp01((TimeofDay + 24f - mNightStartTime) / mNightDuration);
            }
        }
    } 
}