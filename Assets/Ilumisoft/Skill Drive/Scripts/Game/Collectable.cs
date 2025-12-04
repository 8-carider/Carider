using UnityEngine;
using UnityEngine.Events;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
#endif

namespace Ilumisoft.SkillDrive.Game
{
    public class Collectable : MonoBehaviour
    {
        [SerializeField]
        GameObject particlesPrefab = null;

        [SerializeField, Tooltip("콜렉터블 생성 시 나타날 파티클 (프리팹 자식으로 배치)")]
        private ParticleSystem spawnParticle = null;

        [SerializeField, Tooltip("생성 파티클이 재생될 시간(초)")]
        private float spawnParticleShowDuration = 2.0f;

        public UnityAction OnCollect = null;

        private void Start()
        {
            // Ground 레이어 위에 자동으로 위치 조정
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                RaycastHit hit;
                // 위에서 아래로 Raycast (Collectable이 공중에 있어도 동작)
                if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 100f, 1 << groundLayer))
                {
                    Vector3 pos = transform.position;
                    pos.y = hit.point.y;
                    transform.position = pos;
                }
            }

            // 메시와 콜라이더 찾기
            var renderer = GetComponentInChildren<MeshRenderer>();
            var collider = GetComponentInChildren<Collider>();
            
            // 메시 비활성화
            if (renderer != null)
            {
                renderer.gameObject.SetActive(false);
                Debug.Log($"[Collectable] 메시 비활성화: {renderer.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[Collectable] MeshRenderer를 찾을 수 없음: {gameObject.name}");
            }
            
            if (collider != null)
            {
                collider.enabled = false;
            }

            // 파티클 컴포넌트 찾기 (자식이 없으면 경고)
            if (spawnParticle == null)
            {
                spawnParticle = GetComponentInChildren<ParticleSystem>();
            }

            // 생성 전조 파티클 활성화
            if (spawnParticle != null)
            {
                spawnParticle.gameObject.SetActive(true);
                
                // 파티클이 시간 동안 계속 재생되도록 루핑 활성화
                var main = spawnParticle.main;
                main.loop = true;
                
                spawnParticle.Play();
                Debug.Log($"[Collectable] 생성 파티클 재생 (루핑): {gameObject.name}, 파티클 오브젝트: {spawnParticle.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[Collectable] spawnParticle이 null입니다: {gameObject.name}");
            }

            // 일정 시간 후 파티클 비활성화하고 메시/콜라이더 표시
            StartCoroutine(ShowMeshAfterDelay(renderer, collider, spawnParticle, spawnParticleShowDuration));

            // RadarSystem에 자동 등록 (딜레이 후)
            StartCoroutine(RegisterToRadarDelayed());

            // LocatableManager.Locatables 리스트 로그 출력
            Debug.Log($"[LocatableManager] 현재 등록된 Locatable 개수: {Ilumisoft.RadarSystem.LocatableManager.Locatables.Count}");
            foreach (var loc in Ilumisoft.RadarSystem.LocatableManager.Locatables)
            {
                Debug.Log($"[LocatableManager] 등록된 오브젝트: {loc.gameObject.name}");
            }
        }

        private System.Collections.IEnumerator ShowMeshAfterDelay(MeshRenderer renderer, Collider collider, ParticleSystem particle, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            Debug.Log($"[Collectable] ShowMeshAfterDelay 코루틴 시작, delay={delay}");
            
            // 파티클 비활성화
            if (particle != null)
            {
                particle.Stop();
                particle.gameObject.SetActive(false);
                Debug.Log($"[Collectable] 파티클 비활성화 완료: {particle.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[Collectable] particle이 null입니다!");
            }
            
            // 메시 활성화 - 직접 자식들을 순회해서 찾기
            Transform meshChild = null;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Cube")
                {
                    meshChild = child;
                    break;
                }
            }
            
            if (meshChild != null)
            {
                meshChild.gameObject.SetActive(true);
                Debug.Log($"[Collectable] Cube 활성화 완료: {meshChild.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[Collectable] Cube를 찾을 수 없습니다!");
                // 다른 이름의 메시 자식 찾기
                if (renderer != null)
                {
                    renderer.gameObject.SetActive(true);
                    Debug.Log($"[Collectable] renderer의 게임오브젝트 활성화: {renderer.gameObject.name}");
                }
            }
            
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        private System.Collections.IEnumerator RegisterToRadarDelayed()
        {
            yield return new WaitForSeconds(0.1f); // 0.1초 딜레이

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
            if (other.CompareTag("Player"))
            {

#if PHOTON_UNITY_NETWORKING
                if (particlesPrefab != null)
                {
                    if (PhotonNetwork.InRoom)
                    {
                        PhotonNetwork.Instantiate("Collect Particles", transform.position, transform.rotation);
                    }
                    else
                    {
                        Instantiate(particlesPrefab, transform.position, transform.rotation);
                    }
                }
#else
                if (particlesPrefab != null)
                {
                    Instantiate(particlesPrefab, transform.position, transform.rotation);
                }
#endif


                // 차량의 VehicleCollectables 컴포넌트 업데이트
                var vehicleCollectables = other.GetComponent<VehicleCollectables>();
                if (vehicleCollectables != null)
                {
                    vehicleCollectables.AddCollectable(1);
                }

                OnCollect?.Invoke();

#if PHOTON_UNITY_NETWORKING
                if (PhotonNetwork.InRoom)
                {
                    PhotonView photonView = PhotonView.Get(this);
                    if (photonView != null && !photonView.IsMine)
                    {
                        // 소유권이 없으면 먼저 요청만 하고, 파괴는 소유권이 넘어온 후에만 시도
                        photonView.RequestOwnership();
                        return;
                    }
                    // 소유자(또는 마스터)가 파괴를 시도
                    if (!PhotonNetwork.IsMasterClient)
                    {
                        if (photonView != null)
                        {
                            photonView.RPC("RPC_RequestDestroy", RpcTarget.MasterClient);
                        }
                    }
                    else
                    {
                        PhotonNetwork.Destroy(gameObject);
                    }
                }
                else
                {
                    Destroy(gameObject);
                }
#else
                Destroy(gameObject);
#endif
            }
        }

#if PHOTON_UNITY_NETWORKING
        [PunRPC]
        private void RPC_RequestDestroy()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
#endif
    }
}