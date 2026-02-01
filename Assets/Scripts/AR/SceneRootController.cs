using UnityEngine;

namespace TheTear.AR
{
    public class SceneRootController : MonoBehaviour
    {
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
