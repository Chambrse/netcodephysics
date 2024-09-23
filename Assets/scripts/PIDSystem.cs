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
    public Vector3 angularAcceleration;
}

public struct PID_Rotation_Tag : IComponentData { };



[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(DetermineTargetRotationSystem))]
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

        Vector3 errorVector = new Vector3(error.x, error.y, error.z);
        Vector3 angularVelocityVector = new Vector3(angularVelocity.x, angularVelocity.y, angularVelocity.z);

        pidOutputs.angularAcceleration =
            pidGainSet.Kp * errorVector -
            pidGainSet.Kd * angularVelocityVector;

    }
}