using System.Collections;
using System.Collections.Generic;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

public enum WaitType { Produce,Take,Break,ShortBreak }

[System.Serializable]
public class WaitTimeEntry
{
    public WaitType waitType;
    public float waitTime;
}
[System.Serializable]
public class JobWaitTimeData
{
    public Job jobType;
    public List<WaitTimeEntry> waitTimes;

    private Dictionary<WaitType, float> _dict;

    public float GetWaitTime(WaitType type)
    {
        if (_dict == null || _dict.Count != waitTimes.Count)
        {
            _dict = new Dictionary<WaitType, float>();
            foreach (var entry in waitTimes)
            {
                _dict[entry.waitType] = entry.waitTime;
            }
        }
        return _dict[type];
    }
}
[CreateAssetMenu(fileName = "JobWaitTimeDatabase", menuName = "ScriptableObjects/JobWaitTimeDatabase")]
public class JobWaitTimeDataSO : ScriptableObject
{
    public List<JobWaitTimeData> jobWaitTimes;

    private Dictionary<Job, JobWaitTimeData> _lookup;
    public float GetWaitTime(Job job, WaitType type)
    {
        if (_lookup == null || _lookup.Count != jobWaitTimes.Count)
        {
            _lookup = new Dictionary<Job, JobWaitTimeData>();
            foreach (var data in jobWaitTimes)
            {
                _lookup[data.jobType] = data;
            }
        }

        return _lookup[job].GetWaitTime(type);
    }
}
