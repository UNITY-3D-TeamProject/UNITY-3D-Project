using Attribute.Core;
using Attribute.Effect;
using UnityEngine;

namespace Core
{
    public abstract class SpawnerBase : MonoBehaviour
    {
        protected bool TrySpawnWithAttributes(
            GameObject prefab,
            Transform spawnPoint,
            out GameObject instance,
            out AttributeSet attributes)
        {
            instance = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            attributes = instance.GetComponentInChildren<AttributeSet>(true);

            if (attributes != null)
            {
                return true;
            }

            Debug.LogError($"[{prefab.name}] AttributeSet이 없습니다.", instance);
            Destroy(instance);
            return false;
        }

        protected void ApplySpawnEffects(AttributeSet attributes, SOAttributeEffect[] effects)
        {
            if (effects == null)
            {
                return;
            }

            foreach (SOAttributeEffect effect in effects)
            {
                if (effect == null)
                {
                    continue;
                }

                effect.Apply(attributes);
            }
        }
    }
}
