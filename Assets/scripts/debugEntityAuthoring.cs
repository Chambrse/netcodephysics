using Unity.Entities;
using UnityEngine;


[DisallowMultipleComponent]
public class debugEntityAuthoring : MonoBehaviour
{

    public GameObject rotationVizPrefab;

    class Baker : Baker<debugEntityAuthoring>
    {
        public override void Bake(debugEntityAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            DebugVizPrefabs DVPComponent = new DebugVizPrefabs { 
                rotationVizPrefab = GetEntity(authoring.rotationVizPrefab, TransformUsageFlags.Dynamic) 
                };

            AddComponent(entity, DVPComponent);

            DebugSettings DSComponent = new DebugSettings { 
                DebugMode = IntToBool(PlayerPrefs.GetInt("DebugMode")), 
                ShowTargetRotation = IntToBool(PlayerPrefs.GetInt("ShowTargetRotation")) 
                };

            AddComponent(entity, DSComponent);

        }

        //function to convert 0 and 1 to true and false
        public static bool IntToBool(int i)
        {
            return i == 1;
        }
    }
}

