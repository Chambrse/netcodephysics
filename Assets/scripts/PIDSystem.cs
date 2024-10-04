using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Burst;
// using System.Numerics;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;

public struct PIDGainSet : IComponentData
{
    public float Kp;
    public float Ki;
    public float Kd;
}

public struct PIDInputs_Vector : IComponentData
{
    public float3 AngleError;

    public float3 AngularVelocity;
}

public struct PIDOutputs_Vector : IComponentData
{
    public float3 angularAcceleration;
}

public struct PIDInputs_Scalar : IComponentData
{
    public float Error;

    public float DeltaError;
}

public struct PIDOutputs_Scalar : IComponentData
{
    public float linearAcceleration;
}

public struct PID_Rotation_Tag : IComponentData { };



[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(DetermineRotationErrors))]
[BurstCompile]
public partial struct PIDSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        // Initialize ComponentLookup for TargetRotation
        var targetRotationLookup = state.GetComponentLookup<TargetRotation>(isReadOnly: true);

        // First job: getAngleError using ComponentLookup instead of EntityManager
        JobHandle getAngleErrorJobHandle = new getAngleError
        {
            targetRotationLookup = targetRotationLookup
        }.Schedule(state.Dependency);

        getAngleErrorJobHandle.Complete();

        var pidJob = new PIDJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        }.Schedule(getAngleErrorJobHandle);

        pidJob.Complete();

        // Initialize ComponentLookup for PIDOutputs_Vector
        var pidOutputsLookup = state.GetComponentLookup<TargetRelativeVelocity>(isReadOnly: true);

        // JobHandle getVelocityErrorJobHandle = new getVelocityError
        // {
        //     targetRelativeVelocityLookup = pidOutputsLookup
        // }.Schedule(state.Dependency);

        // getVelocityErrorJobHandle.Complete();

        var pidJobScalar = new PIDJob_Scalar
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        }.Schedule(pidJob);

        pidJobScalar.Complete();
    }
}

[BurstCompile]
public partial struct getAngleError : IJobEntity
{

    [ReadOnly] public ComponentLookup<TargetRotation> targetRotationLookup;

    [BurstCompile]
    private void Execute(
        in Parent parent,
        ref PIDInputs_Vector pidInputs)
    {
        // Safely access the TargetRotation component from the parent entity
        if (targetRotationLookup.HasComponent(parent.Value))
        {
            pidInputs.AngleError = targetRotationLookup[parent.Value].targetRotationError;
        }
    }
}

[BurstCompile]
public partial struct getVelocityError : IJobEntity
{

    [ReadOnly] public ComponentLookup<TargetRelativeVelocity> targetRelativeVelocityLookup;

    [BurstCompile]
    private void Execute(
        in Parent parent,
        ref PIDInputs_Scalar pidInputs)
    {
        // Safely access the TargetRelativeVelocity component from the parent entity
        if (targetRelativeVelocityLookup.HasComponent(parent.Value))
        {
            pidInputs.Error = targetRelativeVelocityLookup[parent.Value].targetRelativeVelocityError;
        }
    }

}

[BurstCompile]
public partial struct PIDJob_Scalar : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    private void Execute(
        in PIDGainSet pidGainSet,
        in PIDInputs_Scalar pidInputs,
        ref PIDOutputs_Scalar pidOutputs)
    {
        float error = pidInputs.Error;
        float deltaError = pidInputs.DeltaError;

        pidOutputs.linearAcceleration =
            pidGainSet.Kp * error -
            pidGainSet.Kd * deltaError;
    }
}

[BurstCompile]
public partial struct PIDJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    private void Execute(
        in PIDGainSet pidGainSet,
        in PIDInputs_Vector pidInputs,
        ref PIDOutputs_Vector pidOutputs)
    {
        float3 error = pidInputs.AngleError;
        float3 angularVelocity = pidInputs.AngularVelocity;

//      // todo: convert to Burst-compatible code   
        // Vector3 errorVector = new Vector3(error.x, error.y, error.z);
        // Vector3 angularVelocityVector = new Vector3(angularVelocity.x, angularVelocity.y, angularVelocity.z);

        pidOutputs.angularAcceleration =
            pidGainSet.Kp * error -
            pidGainSet.Kd * angularVelocity;

    }
}