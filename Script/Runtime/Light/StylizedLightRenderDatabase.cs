using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Misaki.StylizedSky
{
    //Light rendering entity. This struct acts as a handle to set / get light render information into the database.
    internal struct StylizedLightRenderEntity
    {
        public int entityIndex;
        public static readonly StylizedLightRenderEntity Invalid = new StylizedLightRenderEntity() { entityIndex = StylizedLightRenderDatabase.InvalidDataIndex };
        public bool valid { get { return entityIndex != StylizedLightRenderDatabase.InvalidDataIndex; } }
    }
    internal partial class StylizedLightRenderDatabase
    {
        // Intermediate struct which holds the data index of an entity and other information.
        private struct LightEntityInfo
        {
            public int dataIndex;
            public int lightInstanceID;
            public static readonly LightEntityInfo Invalid = new LightEntityInfo() { dataIndex = InvalidDataIndex, lightInstanceID = -1 };
            public bool valid { get { return dataIndex != -1 && lightInstanceID != -1; } }
        }
        private const int ArrayCapacity = 100;
        private static StylizedLightRenderDatabase s_Instance;
        private int m_Capacity = 0;
        private int m_LightCount = 0;
        private int m_AttachedGameObjects = 0;

        private NativeList<LightEntityInfo> m_LightEntities;
        private Dictionary<int, LightEntityInfo> m_LightsToEntityItem = new Dictionary<int, LightEntityInfo>();

        private DynamicArray<GameObject> m_AOVGameObjects = new DynamicArray<GameObject>();
        private DynamicArray<StylizedDirectionalLight> m_StylizedDirectionalLight = new DynamicArray<StylizedDirectionalLight>();

        private Queue<int> m_FreeIndices = new Queue<int>();
        private NativeArray<StylizedLightRenderEntity> m_OwnerEntity;
        private NativeArray<bool> m_AutoDestroy;

        public static StylizedDirectionalLight mainLight => FindMainLight(null);

        public static int InvalidDataIndex = -1;

        //total light count of all lights in the world.`
        public int lightCount => m_LightCount;

        // This array tracks directional lights for the PBR sky
        // We need this as VisibleLight result from culling ignores lights with intensity == 0
        public List<StylizedDirectionalLight> directionalLights = new();
        //Access of main instance
        public static StylizedLightRenderDatabase instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new StylizedLightRenderDatabase();
                return s_Instance;
            }
        }

        // Creates a light render entity.
        public StylizedLightRenderEntity CreateEntity(bool autoDestroy)
        {
            if (!m_LightEntities.IsCreated)
            {
                m_LightEntities = new NativeList<LightEntityInfo>(Allocator.Persistent);
            }

            LightEntityInfo newData = AllocateEntityData();

            StylizedLightRenderEntity newLightEntity = StylizedLightRenderEntity.Invalid;
            if (m_FreeIndices.Count == 0)
            {
                newLightEntity.entityIndex = m_LightEntities.Length;
                m_LightEntities.Add(newData);
            }
            else
            {
                newLightEntity.entityIndex = m_FreeIndices.Dequeue();
                m_LightEntities[newLightEntity.entityIndex] = newData;
            }

            m_OwnerEntity[newData.dataIndex] = newLightEntity;
            m_AutoDestroy[newData.dataIndex] = autoDestroy;
            return newLightEntity;
        }

        // Destroys a light render entity.
        public unsafe void DestroyEntity(StylizedLightRenderEntity lightEntity)
        {
            if (!lightEntity.valid)
                return;

            m_FreeIndices.Enqueue(lightEntity.entityIndex);
            LightEntityInfo entityData = m_LightEntities[lightEntity.entityIndex];
            m_LightsToEntityItem.Remove(entityData.lightInstanceID);

            StylizedDirectionalLight lightData = m_StylizedDirectionalLight[entityData.dataIndex];
            if (lightData != null)
            {
                int idx = directionalLights.FindIndex((x) => ReferenceEquals(x, lightData));
                if (idx != -1) directionalLights.RemoveAt(idx);

                --m_AttachedGameObjects;
            }

            RemoveAtSwapBackArrays(entityData.dataIndex);

            if (m_LightCount == 0)
            {
                DeleteArrays();
            }
            else
            {
                StylizedLightRenderEntity entityToUpdate = m_OwnerEntity[entityData.dataIndex];
                LightEntityInfo dataToUpdate = m_LightEntities[entityToUpdate.entityIndex];
                dataToUpdate.dataIndex = entityData.dataIndex;
                m_LightEntities[entityToUpdate.entityIndex] = dataToUpdate;
                if (dataToUpdate.lightInstanceID != entityData.lightInstanceID)
                    m_LightsToEntityItem[dataToUpdate.lightInstanceID] = dataToUpdate;
            }
        }

        private void RemoveAtSwapBackArrays(int removeIndexAt)
        {
            int lastIndex = m_LightCount - 1;
            m_StylizedDirectionalLight[removeIndexAt] = m_StylizedDirectionalLight[lastIndex];
            m_StylizedDirectionalLight[lastIndex] = null;

            m_AOVGameObjects[removeIndexAt] = m_AOVGameObjects[lastIndex];
            m_AOVGameObjects[lastIndex] = null;

            //m_LightData[removeIndexAt] = m_LightData[lastIndex];
            m_OwnerEntity[removeIndexAt] = m_OwnerEntity[lastIndex];
            m_AutoDestroy[removeIndexAt] = m_AutoDestroy[lastIndex];

            --m_LightCount;
        }

        private void DeleteArrays()
        {
            if (m_Capacity == 0)
                return;

            m_StylizedDirectionalLight.Clear();
            m_AOVGameObjects.Clear();
            //m_LightData.Dispose();
            m_OwnerEntity.Dispose();
            m_AutoDestroy.Dispose();

            m_FreeIndices.Clear();
            m_LightEntities.Dispose();
            m_LightEntities = default;

            m_Capacity = 0;
        }

        public unsafe void AttachGameObjectData(StylizedLightRenderEntity entity, int instanceID, StylizedDirectionalLight additionalLightData, GameObject aovGameObject)
        {
            if (!IsValid(entity))
                return;

            LightEntityInfo entityInfo = m_LightEntities[entity.entityIndex];
            int dataIndex = entityInfo.dataIndex;
            if (dataIndex == InvalidDataIndex)
                return;

            entityInfo.lightInstanceID = instanceID;
            m_LightEntities[entity.entityIndex] = entityInfo;

            m_LightsToEntityItem.Add(entityInfo.lightInstanceID, entityInfo);
            m_StylizedDirectionalLight[dataIndex] = additionalLightData;
            m_AOVGameObjects[dataIndex] = aovGameObject;
            ++m_AttachedGameObjects;

            if (additionalLightData.legacyLight.type == LightType.Directional
#if UNITY_EDITOR
                 && !UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(additionalLightData.gameObject.scene)
#endif
                )
                directionalLights.Add(additionalLightData);
        }

        // Returns true where the entity has been destroyed or not.
        public bool IsValid(StylizedLightRenderEntity entity)
        {
            return entity.valid && m_LightEntities.IsCreated && entity.entityIndex < m_LightEntities.Length;
        }

        private void ResizeArrays()
        {
            m_StylizedDirectionalLight.Resize(m_Capacity, true);
            m_AOVGameObjects.Resize(m_Capacity, true);

            m_OwnerEntity.ResizeArray(m_Capacity);
            m_AutoDestroy.ResizeArray(m_Capacity);
        }

        private LightEntityInfo AllocateEntityData()
        {
            if (m_Capacity == 0 || m_LightCount == m_Capacity)
            {
                m_Capacity = Math.Max(Math.Max(m_Capacity * 2, m_LightCount), ArrayCapacity);
                ResizeArrays();
            }

            int newIndex = m_LightCount++;
            LightEntityInfo newDataIndex = new LightEntityInfo { dataIndex = newIndex, lightInstanceID = -1 };
            return newDataIndex;
        }

        public static StylizedDirectionalLight FindMainLight(StylizedDirectionalLight toExclude)
        {
            StylizedDirectionalLight result = null;
            int currentMax = -101;
            foreach (StylizedDirectionalLight light in instance.directionalLights)
            {
                if (light != toExclude && light.priority > currentMax)
                {
                    currentMax = light.priority;
                    result = light;
                }
            }
            return result;
        }
    }
}