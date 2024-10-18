using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


//enum for viz types

public enum vizType
{
    RotationViz,
    vectorViz
}

public struct RotationViz : IComponentData { }

public struct LinearVectorViz : IComponentData { }

//public struct TorqueVectorViz : IComponentData { }

[DisallowMultipleComponent]
public class VizAuthoring : MonoBehaviour
{

    public vizType type;

    class Baker : Baker<VizAuthoring>
    {
        public override void Bake(VizAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            switch (authoring.type)
            {
                //to do: add disabled component so they are off by default.
                case vizType.RotationViz:
                    AddComponent(entity, new RotationViz());
                    //AddComponent(entity, new)
                    break;
                case vizType.vectorViz:
                    AddComponent(entity, new LinearVectorViz());
                    break;
                default:
                    break;
            }
            //AddComponent(entity, new Disabled());


        }
    }
}
