using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Physics;
using Unity.Mathematics;


public struct AeroForces : IComponentData
{
    public float3 linearAeroForces;
    public float3 angularAeroForces;
}

[UpdateInGroup(typeof(CustomPhysicsSystemGroup))]
[BurstCompile]
public partial struct CalculateAeroForces : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CraftTuning>();
        state.RequireForUpdate<PlayerTag>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (physVelocity, localTx, craftTuning, craftInput, aeroForces)
                 in SystemAPI.Query<RefRW<PhysicsVelocity>,
                                    RefRO<LocalTransform>,
                                    RefRO<CraftTuning>,
                                    RefRO<CraftInput>,
                                    RefRW<AeroForces>>()
                             .WithAll<PlayerTag>())
        {
            float3 worldVel = physVelocity.ValueRO.Linear;
            quaternion rot = localTx.ValueRO.Rotation;

            // 1) Compute drag force ------------------------------------

            // Convert velocity to local space
            float3 localVel = math.mul(math.inverse(rot), worldVel);

            float dragX = math.abs(localVel.x) * localVel.x * craftTuning.ValueRO.dragVector.x; // sideways
            float dragY = math.abs(localVel.y) * localVel.y * craftTuning.ValueRO.dragVector.y; // up
            float dragZ = math.abs(localVel.z) * localVel.z * craftTuning.ValueRO.dragVector.z; // forward

            float3 localDragUnscaled = new float3(dragX, dragY, dragZ);

            // Opposite direction of velocity
            float dragMag = math.length(localDragUnscaled);
            float3 dragDir = math.select(float3.zero,
                                         math.normalize(localDragUnscaled) * -1f,
                                         dragMag > 1e-5f);
            float3 finalDragLocal = dragDir * dragMag;

            // Convert to world space
            float3 finalDragWorld = math.mul(rot, finalDragLocal);

            // 2) Compute torque ---------------------------------------
            // T = r x F, where r is offset from CG to CP, F = drag force
            // We'll do it in local first, then transform to world
            float3 cpOffsetLocal = craftTuning.ValueRO.centerOfPressureOffset;
            float3 torqueLocal = math.cross(cpOffsetLocal, finalDragLocal);

            // 3) Store in AeroForces component -------------------------
            aeroForces.ValueRW.linearAeroForces = finalDragWorld;
            aeroForces.ValueRW.angularAeroForces = torqueLocal;
        }
    }
}