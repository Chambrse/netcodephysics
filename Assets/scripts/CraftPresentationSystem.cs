//using Unity.Entities;
//using Unity.NetCode;
//using Unity.Transforms;

//[UpdateInGroup(typeof(CustomPresentationSystemGroup))]
//[UpdateAfter(typeof(TransformSystemGroup))] // Runs after all transforms are finalized
//public partial struct CraftPresentationSystem : ISystem
//{
//    public void OnUpdate(ref SystemState state)
//    {
//        foreach (var (followData, transform) in SystemAPI.Query<RefRO<cameraFollowPosition>, RefRW<LocalTransform>>().WithAll<GhostOwnerIsLocal>())
//        {
//            transform.ValueRW.Position = followData.ValueRO.SmoothedPosition;
//            transform.ValueRW.Rotation = followData.ValueRO.SmoothedRotation;
//        }
//    }
//}