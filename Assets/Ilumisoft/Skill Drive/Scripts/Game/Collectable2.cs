using UnityEngine;
using UnityEngine.Events;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class Collectable2 : MonoBehaviour
    {
        [SerializeField]
        private GameObject particlesPrefab = null;

        [SerializeField, Tooltip("이 Collectable2의 고유 ID (자동 생성됨, 수동 설정 가능)")]
        private int spawnPointID = 0;

        // 중복 수집 방지 플래그
        private bool isCollected = false;

        public int SpawnPointID => spawnPointID;

        public UnityAction OnCollect = null;

        public void SetSpawnPointID(int id)
        {
            spawnPointID = id;
        }

        private void Awake()
        {
            // SpawnPointID가 0이면 InstanceID를 기반으로 자동 생성
            if (spawnPointID == 0)
            {
                spawnPointID = Mathf.Abs(GetInstanceID());
            }
        }

        private void Start()
        {
            // SpawnPointID 확인
            Debug.Log($"[Collectable2] {gameObject.name} 시작: SpawnPointID={spawnPointID}, 위치={transform.position}");
            
            // CollectableManager에 등록 (위치 저장)
            if (CollectableManager.Instance != null)
            {
                CollectableManager.Instance.RegisterCollectable2(this);
            }
            else
            {
                Debug.LogWarning($"[Collectable2] CollectableManager.Instance가 null입니다!");
            }

            // RadarSystem 자동 등록
            StartCoroutine(RegisterToRadarDelayed());
        }

        private System.Collections.IEnumerator RegisterToRadarDelayed()
        {
            yield return new WaitForSeconds(0.1f);

            var locatableManagerType = System.Type.GetType("Ilumisoft.RadarSystem.LocatableManager, Ilumisoft.RadarSystem");
            if (locatableManagerType != null)
            {
                var registerMethod = locatableManagerType.GetMethod("Register", new[] { typeof(GameObject) });
                if (registerMethod != null)
                {
                    var managerInstance = GameObject.FindObjectOfType(locatableManagerType);
                    if (managerInstance != null)
                    {
                        registerMethod.Invoke(managerInstance, new object[] { this.gameObject });
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            // 중복 수집 방지
            if (isCollected)
                return;

            isCollected = true;

            // 파티클 생성 (컬러 분리: Collectable2는 지정된 프리팹 사용)
            if (particlesPrefab != null)
            {
                // 파티클은 네트워크 동기화가 필요 없으므로 로컬 생성만 수행
                Instantiate(particlesPrefab, transform.position, transform.rotation);
            }

            // 점수 +2
            var vehicleCollectables = other.GetComponent<VehicleCollectables>();
            if (vehicleCollectables != null)
            {
                vehicleCollectables.AddCollectable(2);
            }

            OnCollect?.Invoke();

            Debug.Log($"[Collectable2] {gameObject.name} 먹힘: SpawnPointID={spawnPointID}, 마스터={PhotonNetwork.IsMasterClient}");

            // 🔥 리스폰: CollectableManager 중앙 관리로 통합
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.InRoom)
            {
                // 마스터만 리스폰 처리 (중복 방지)
                if (PhotonNetwork.IsMasterClient)
                {
                    CollectableManager.Instance?.OnCollectable2PickedUp(this);
                }
            }
            else
            {
                CollectableManager.Instance?.OnCollectable2PickedUp(this);
            }
#else
            CollectableManager.Instance?.OnCollectable2PickedUp(this);
#endif

            // === 파괴 처리 (약간 지연하여 모든 클라이언트가 OnTriggerEnter 처리할 수 있도록) ===
#if PHOTON_UNITY_NETWORKING
            if (PhotonNetwork.InRoom)
            {
                // 콜라이더 비활성화로 중복 충돌 방지
                var col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                
                // 렌더러도 비활성화 (시각적으로 즉시 사라짐)
                var renderers = GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.enabled = false;
                }
                
                PhotonView pv = PhotonView.Get(this);

                if (!PhotonNetwork.IsMasterClient)
                {
                    if (pv != null)
                        pv.RPC("RPC_RequestDestroy", RpcTarget.MasterClient);
                }
                else
                {
                    // 마스터는 약간 지연 후 파괴
                    StartCoroutine(DestroyAfterDelay(0.1f));
                }
            }
            else
            {
                var col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
                Destroy(gameObject, 0.1f);
            }
#else
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Destroy(gameObject, 0.1f);
#endif
        }

#if PHOTON_UNITY_NETWORKING
        [PunRPC]
        private void RPC_RequestDestroy()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(DestroyAfterDelay(0.1f));
            }
        }

        private System.Collections.IEnumerator DestroyAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (PhotonNetwork.IsMasterClient && gameObject != null)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
#endif
    }
}
