using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Unity.Transforms;
using Unity.Collections;

[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
[UpdateAfter(typeof(DetermineVelocityErrors))]
[BurstCompile]
public partial struct PIDSystemLinear : ISystem
{

    //ComponentLookup<CraftInput> craftInputLookup;

    //[BurstCompile]
    //public void OnCreate(ref SystemState state)
    //{
    //    craftInputLookup = state.GetComponentLookup<CraftInput>(isReadOnly: true);
    //}

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        var craftInputLookup = state.GetComponentLookup<CraftInput>(isReadOnly: true);

        //craftInputLookup.Update(ref state);

        var getInputGainJob = new getLinearInputGain
        {
            craftInputLookup = craftInputLookup
        }.ScheduleParallel(state.Dependency);

        getInputGainJob.Complete();



        var pidJob = new PIDJob_Linear
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        }.ScheduleParallel(state.Dependency);

        pidJob.Complete();

    }
}

[BurstCompile]
public partial struct getLinearInputGain : IJobEntity
{

    [ReadOnly] public ComponentLookup<CraftInput> craftInputLookup;

    [BurstCompile]
    private void Execute(
            in Parent parent,
            ref PIDGainFromInput inputGain,
            in linPIDTag linPID
        )
    {
        if (craftInputLookup.HasComponent(parent.Value))
        {
            inputGain.GainInput = craftInputLookup[parent.Value].Brakes;
        }
    }
}

[BurstCompile]
public partial struct PIDJob_Linear : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    private void Execute(
        in PIDGainSet pidGainSet,
        in PIDInputs_Vector pidInputs,
        ref PIDOutputs_Vector pidOutputs,
        in PIDGainFromInput inputGain,
        in linPIDTag linPID)
    {
        float3 error = pidInputs.VectorError;
        float3 deltaError = pidInputs.DeltaVectorError;

        //log
        Debug.Log("hello");
        Debug.Log(inputGain.GainInput);

        pidOutputs.VectorResponse =
            (pidGainSet.Kp * inputGain.GainInput) * error -
            (pidGainSet.Kd) * deltaError;


        //pidOutputs.VectorResponse =
        //    (pidGainSet.Kp) * error -
        //    (pidGainSet.Kd) * deltaError;
    }
}
