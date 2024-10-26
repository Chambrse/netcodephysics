using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Burst;
using Unity.Transforms;

public struct VerticalAcceleration : IComponentData
{
    public float verticalAcceleration;
    public float localVerticalAcceleration;
}

public struct LinearAcceleration : IComponentData
{
    public float3 linearAcceleration;
    public float3 localLinearAcceleration;
}

[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
[UpdateAfter(typeof(PIDSystem))]
[BurstCompile]
public partial struct LinearAccelerationSystem : ISystem
{

    // ComponentLookup<PhysicsWorldSingleton> physicsWorldLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Initialization code here
        // physicsWorldLookup = state.GetComponentLookup<PhysicsWorldSingleton>(true);

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Cleanup code here
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        // physicsWorldLookup.Update(ref state);

        // Debug.Log("LinearAccelerationSystem OnUpdate");
        // Initialize ComponentLookup for angular acceleration
        // var angularAccelerationLookup = state.GetComponentLookup<AngularAcceleration>();
        //var linearAccelerationLookup = state.GetComponentLookup<LinearAcceleration>();

        //// First job: getAngularAcceleration using ComponentLookup instead of EntityManager
        //var getLinearAccelerationJob = new getVerticalAcceleration
        //{
        //    // angularAccelerationLookup = angularAccelerationLookup
        //    linearAccelerationLookup = linearAccelerationLookup
        //};

        //state.Dependency = getLinearAccelerationJob.Schedule(state.Dependency);


        var job = new ApplyHoverForce
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = job.Schedule(state.Dependency);
    }
}



[BurstCompile]
public partial struct ApplyHoverForce : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    private void Execute(
        //ref VerticalAcceleration verticalAcceleration,
        ref LinearAcceleration linearAcceleration,
        ref PhysicsVelocity physicsVelocity,
        in PhysicsMass physicsMass,
        in LocalTransform localTransform,
        in CraftInput craftInput)
    {

        // get rotation
        //quaternion rotation = physicsMass.Transform.rot;
        quaternion rotation = localTransform.Rotation;
        //quaternion rotaion = localTransform.Rotation;

        // Define the world +y direction as a vector
        float3 worldUp = new float3(0, 1, 0);

        // Rotate worldUp vector into the entity's local space using the rotation quaternion
        float3 localUp = math.mul(rotation, worldUp);


        float totalVerticalAcceleration = linearAcceleration.linearAcceleration.y
            + 9.81f
            + (craftInput.Thrust * 100);

        float localAccelerationMagnitude = totalVerticalAcceleration / math.dot(localUp, worldUp);

        //verticalAcceleration.localVerticalAcceleration = localAccelerationMagnitude;
        //linearAcceleration.localLinearAcceleration = localAccelerationMagnitude;


        // physicsVelocity.Linear.y += (verticalAcceleration.verticalAcceleration + 9.81f + (craftInput.Thrust * 100)) * DeltaTime;
        physicsVelocity.Linear += localUp * (localAccelerationMagnitude * DeltaTime);
    }
}
