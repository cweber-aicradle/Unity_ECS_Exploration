using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Random = UnityEngine.Random;

public class Testing : MonoBehaviour
{
    [SerializeField] private bool useJobs;
    [SerializeField] private Transform pfSun;
    private List<Sun> sunsList;

    public class Sun
    {
        public Transform transform;
        public float moveY;
    }

    private void Start()
    {
        sunsList = new List<Sun>();
        for (int i = 0; i < 100000; i++)
        {
            Transform sunTransform = Instantiate(pfSun, new Vector3(Random.Range(-5, 5f), Random.Range(-4, 4f)), Quaternion.identity);
            sunsList.Add(new Sun { transform = sunTransform, moveY = Random.Range(1f, 2f) }); 
        }
    }

    private void Update()
    {
        float startTime = Time.realtimeSinceStartup;

        if (useJobs)
        {
            //Duplicate pieces of data. If we did this efficiently, only create these 1x.
            NativeArray<float> moveYarray = new NativeArray<float>(sunsList.Count, Allocator.TempJob);
            TransformAccessArray transformAccessArray = new TransformAccessArray(sunsList.Count);

            for (int i = 0; i < sunsList.Count; i++)
            {
                transformAccessArray.Add(sunsList[i].transform);
                moveYarray[i] = sunsList[i].moveY;
            }

            ReallyToughParallelJobTransforms reallyToughParallelJobTransforms = new ReallyToughParallelJobTransforms
            {
                deltaTime = Time.deltaTime,
                moveYarray = moveYarray,
            };

            JobHandle jobHandle = reallyToughParallelJobTransforms.Schedule(transformAccessArray);
            jobHandle.Complete();

            for (int i = 0; i < sunsList.Count; i++)
            {
                sunsList[i].moveY = moveYarray[i];
            }

            transformAccessArray.Dispose();
            moveYarray.Dispose();
        }
        else
        {
            foreach (Sun sun in sunsList)
            {
                sun.transform.position += new Vector3(0, sun.moveY * Time.deltaTime);
                if (sun.transform.position.y > 5f)
                {
                    sun.moveY = -math.abs(sun.moveY);
                }
                else if (sun.transform.position.y < -5f)
                {
                    sun.moveY = math.abs(sun.moveY);
                }
                //Testing.ReallyToughTask(1000);
            }
        }

        //Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
    }


    private void Update_2()
    {
        float startTime = Time.realtimeSinceStartup;

        if (useJobs)
        {
            //Duplicate pieces of data. If we did this efficiently, only create these 1x.
            NativeArray<float3> positionArray = new NativeArray<float3>(sunsList.Count, Allocator.TempJob);
            NativeArray<float> moveYarray = new NativeArray<float>(sunsList.Count, Allocator.TempJob);

            for(int i = 0; i < sunsList.Count; i++)
            {
                positionArray[i] = sunsList[i].transform.position;
                moveYarray[i] = sunsList[i].moveY;
            }

            ReallyToughParallelJob reallyToughParallelJob = new ReallyToughParallelJob
            {
                deltaTime = Time.deltaTime,
                positionArray = positionArray,
                moveYarray = moveYarray,
            };

            JobHandle jobHandle = reallyToughParallelJob.Schedule(sunsList.Count, 100);
            jobHandle.Complete();

            for (int i = 0; i < sunsList.Count; i++)
            {
                sunsList[i].transform.position = positionArray[i];
                sunsList[i].moveY = moveYarray[i];
            }

            positionArray.Dispose();
            moveYarray.Dispose();
        }
        else
        {
            foreach (Sun sun in sunsList)
            {
                sun.transform.position += new Vector3(0, sun.moveY * Time.deltaTime);
                if (sun.transform.position.y > 5f)
                {
                    sun.moveY = -math.abs(sun.moveY);
                }
                else if (sun.transform.position.y < -5f)
                {
                    sun.moveY = math.abs(sun.moveY);
                }
                Testing.ReallyToughTask(1000);
            }
        }

        //Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
    }

    private void Update_Original()
    {
        float startTime = Time.realtimeSinceStartup;

        if (useJobs)
        {
            NativeList<JobHandle> jobHandles = new NativeList<JobHandle>(Allocator.Temp);
            for (int i = 0; i < 10; i++)
            {
                JobHandle jobHandle = ReallyToughTaskJob();
                jobHandles.Add(jobHandle);
            }
            JobHandle.CompleteAll(jobHandles);
            jobHandles.Dispose();
        }
        else
        {
            for (int i = 0; i < 10; i++)
                ReallyToughTask(50000);
        }


        Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
    }

    //Represents a task that takes awhile like pathfinding
    public static void ReallyToughTask(int depth)
    {
        float value = 0f;
        for(int i = 0; i < depth; i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }


    private JobHandle ReallyToughTaskJob()
    {
        ReallyToughJob job  = new ReallyToughJob();
        return job.Schedule();
    }
}

//value type unlike class which is reference type
[BurstCompile]
public struct ReallyToughJob : IJob
{
    //Represents a task that takes awhile like pathfinding
    public void Execute()
    {
        float value = 0f;
        for (int i = 0; i < 50000; i++)
        {
            value = math.exp10(math.sqrt(value));
        }
    }
}

//use ReadOnly when applied to Native fields
//it allows multiple jobs to workconcurrently on the same data.
//they are always copies not refs.
//so here adding ReadOnly to deltaTime was not necessary
[BurstCompile]
public struct ReallyToughParallelJob : IJobParallelFor
{
    public NativeArray<float3> positionArray;
    public NativeArray<float> moveYarray;
    [ReadOnly] public float deltaTime; 

    public void Execute(int index)
    {
        positionArray[index] += new float3(0, moveYarray[index] * deltaTime, 0f);
        if (positionArray[index].y > 5f)
        {
            moveYarray[index] = -math.abs(moveYarray[index]);
        }
        else if (positionArray[index].y < -5f)
        {
            moveYarray[index] = math.abs(moveYarray[index]);
        }
        Testing.ReallyToughTask(1000);
    }
}

[BurstCompile]
public struct ReallyToughParallelJobTransforms : IJobParallelForTransform
{
    public NativeArray<float> moveYarray;
    [ReadOnly] public float deltaTime;

    public void Execute(int index, TransformAccess transform)
    {
        transform.position += new Vector3(0, moveYarray[index] * deltaTime, 0f);
        if (transform.position.y > 5f)
        {
            moveYarray[index] = -math.abs(moveYarray[index]);
        }
        else if (transform.position.y < -5f)
        {
            moveYarray[index] = math.abs(moveYarray[index]);
        }
        //Testing.ReallyToughTask(1000);
    }
}