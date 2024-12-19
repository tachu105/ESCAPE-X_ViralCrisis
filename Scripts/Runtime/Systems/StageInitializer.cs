using SFEscape.Runtime.Utilities;
using Unity.AI.Navigation;
using UnityEngine;

namespace SFEscape.Runtime.Systems
{
    /// <summary>
    /// ステージの構築を行うクラス
    /// </summary>
    [RequireComponent(typeof(NavMeshSurface))]
    public class StageInitializer : MonoBehaviour
    {
        [SerializeField]
        private GameObject subComputerPrefab;
        
        [SerializeField, Tooltip("サブコンピュータの設置個数")]
        private int subComputerCount;
        
        [SerializeField, Tooltip("サブコンピュータの設置候補地")]
        private Transform[] subComputerCandidate;
        
        [SerializeField, Tooltip("ナビメッシュビルド用の非表示オブジェクト")]
        private GameObject[] invisibleBakeObjects;
        
        private void Awake()
        {
            PutSubComputers();
            BakeNavMesh();
            QuestProgress.Instance.InitializeProgress(subComputerCount);
        }

        /// <summary>
        /// サブコンピューターのランダム設置メソッド
        /// </summary>
        private void PutSubComputers()
        {
            foreach (var index in RandomEx.GetUniqueRandomNumbers(0, subComputerCandidate.Length, subComputerCount))
            {
                Instantiate(subComputerPrefab, subComputerCandidate[index].position, subComputerCandidate[index].rotation);
            }
        }
        
        /// <summary>
        /// ナビメッシュのビルド
        /// </summary>
        private void BakeNavMesh()
        {
            foreach (var obj in invisibleBakeObjects)
            {
                obj.SetActive(true);
            }
            this.GetComponent<NavMeshSurface>().BuildNavMesh();
            foreach (var obj in invisibleBakeObjects)
            {
                obj.SetActive(false);
            }
        }
    }
}
