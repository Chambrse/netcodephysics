using Unity.Entities;
using Unity.NetCode;
using Unity.Physics.Systems;




[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class CustomInitializaionSystemGroup : ComponentSystemGroup
{

}

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class CustomInputSystemGroup : ComponentSystemGroup
{

}

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(CustomInputSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class CustomPhysicsSystemGroup : ComponentSystemGroup
{

}

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class CustomPresentationSystemGroup : ComponentSystemGroup
{ }