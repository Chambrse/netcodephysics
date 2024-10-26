using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;

[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
[UpdateAfter(typeof(DetermineVelocityErrors))]
[BurstCompile]
public partial struct PIDSystemLinear : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        var pidJob = new PIDJob_Linear
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        }.ScheduleParallel(state.Dependency);

        pidJob.Complete();

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

        //pidOutputs.VectorResponse =
        //    (pidGainSet.Kp + inputGain.GainInput) * error -
        //    (pidGainSet.Kd + inputGain.GainInput) * deltaError;


        pidOutputs.VectorResponse =
            (pidGainSet.Kp) * error -
            (pidGainSet.Kd) * deltaError;
    }
}
