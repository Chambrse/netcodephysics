using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Burst;
using Unity.Transforms;
// using Unity.Jobs;
using Unity.Collections;
using Unity.Physics.Systems;
using Unity.NetCode;

public struct AngularAcceleration : IComponentData
{
    public float3 angularAcceleration;
}   

[UpdateInGroup(typeof(CustomPhysicsSystemGroup))]
[UpdateAfter(typeof(PIDSystem))]
[BurstCompile]
public partial struct RotationTorqueSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Initialization code here
        state.RequireForUpdate<CraftInput>();

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Cleanup code here
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Initialize ComponentLookup for angular acceleration
        var angularAccelerationLookup = state.GetComponentLookup<AngularAcceleration>();

        // First job: getAngularAcceleration using ComponentLookup instead of EntityManager
        var getAngularAccelerationJob = new getAngularAcceleration
        {
            angularAccelerationLookup = angularAccelerationLookup
        };

        state.Dependency = getAngularAccelerationJob.Schedule(state.Dependency);


        var job = new ApplyTorqueWithGyroscopicEffectsJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = job.Schedule(state.Dependency);
    }
}

[BurstCompile]
public partial struct getAngularAcceleration : IJobEntity
{
    // [ReadOnly] public ComponentLookup<PIDOutputs_Vector> pidOutputsLookup;
    public ComponentLookup<AngularAcceleration> angularAccelerationLookup;

    [BurstCompile]
    private void Execute(
        in PIDOutputs_Vector pidOutputs,
        ref Parent parent,
        in rotPIDTag rotPID) {
        // Retrieve the desired angular acceleration from the PID outputs
        float3 desiredAngularAcceleration = pidOutputs.VectorResponse;
            if (angularAccelerationLookup.HasComponent(parent.Value))
            {
                angularAccelerationLookup[parent.Value] = new AngularAcceleration { angularAcceleration = desiredAngularAcceleration };
            }
        }
}

[BurstCompile]
public partial struct ApplyTorqueWithGyroscopicEffectsJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    private void Execute(
        in AngularAcceleration craftAngularAcceleration,
        ref PhysicsVelocity physicsVelocity,
        in PhysicsMass physicsMass)
    {
        // // Retrieve the desired angular acceleration (assumed to be in local space)
        // float3 desiredAngularAcceleration = craftAngularAcceleration.angularAcceleration;

        // // Transform the desired angular acceleration to torque using the inertia tensor
        // // Since physicsMass.InertiaTensor is the inverse, we need to use the reciprocal for the correct operation.
        // float3 desiredTorque = desiredAngularAcceleration / physicsMass.InverseInertia;

        // // Calculate the gyroscopic torque: w x (I * w)
        // float3 currentAngularVelocity = physicsVelocity.Angular;
        // float3 inertiaTimesAngularVelocity = currentAngularVelocity / physicsMass.InverseInertia;
        // float3 gyroscopicTorque = math.cross(currentAngularVelocity, inertiaTimesAngularVelocity);

        // // Calculate the total torque to be applied
        // float3 totalTorque = desiredTorque + gyroscopicTorque;

        // // Apply the torque to calculate the change in angular velocity
        // float3 angularVelocityChange = totalTorque * DeltaTime;

        // // Update the angular velocity in the physics system
        // physicsVelocity.Angular += angularVelocityChange;


                // Retrieve the desired angular acceleration in local space
        float3 desiredAngularAcceleration = craftAngularAcceleration.angularAcceleration;

        // Convert the angular acceleration to a change in angular velocity
        float3 angularVelocityChange = desiredAngularAcceleration * DeltaTime;

        //clamp the angular velocity change to the maximum angular velocity
        // angularVelocityChange = math.clamp(angularVelocityChange, new float3(-1,-1,-1), new float3(1,1,1));

        // Apply the change directly to the angular velocity
        physicsVelocity.Angular += angularVelocityChange;
    }
}
